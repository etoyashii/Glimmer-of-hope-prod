using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace GlimmerOfHope.Gameplay
{
    public class MassiveFoliageMeshPlacer : MonoBehaviour
    {
        #region Public Properties
        [Header("Attach The Terrain")]
        public Terrain terrain;

        [Header("Select Target Texture and Source")]
        public List<LayerTerrain> TerrainLayers = new List<LayerTerrain>();

        [Header("Global Parameters")]
        public bool EraseModification = true;

        [Tooltip("Layers physiques testés lors de la vérification anti-chevauchement des prefabs de foliage.")]
        public LayerMask collisionCheckLayers = ~0;

        [Tooltip("Si activé, oriente le prefab selon la normale du terrain plutôt que sur l'axe vertical.")]
        public bool alignToTerrainNormal = false;

        public enum FillType { full, sides }

        public const int MAX_AMOUNT = 500;

        private Transform generatedRoot;

        #endregion

        #region Public Methods
        // ============  FOLIAGE MESH (PREFABS) PLACEMENT  ========
        public void GenerateFoliageMeshes()
        {
            EnsureGeneratedRoot();
            if (EraseModification) CleanFoliageMeshes(-1);

            for (int layer = 0; layer < TerrainLayers.Count; layer++)
            {
                PlaceFoliageForLayer(layer);
            }
        }

        public void GenerateFoliageMeshesForLayer(int layerIndex)
        {
            EnsureGeneratedRoot();
            if (EraseModification) CleanFoliageMeshes(layerIndex);

            PlaceFoliageForLayer(layerIndex);
        }

        public void CleanFoliageMeshes(int layerIndex)
        {
            EnsureGeneratedRoot();

            if (layerIndex == -1)
            {
                for (int i = generatedRoot.childCount - 1; i >= 0; i--)
                    DestroyGameObjectImmediate(generatedRoot.GetChild(i).gameObject);
                return;
            }

            if (layerIndex < 0 || layerIndex >= TerrainLayers.Count) return;

            string layerName = SanitizeName(TerrainLayers[layerIndex].name);
            Transform layerRoot = generatedRoot.Find(layerName);
            if (layerRoot != null) DestroyGameObjectImmediate(layerRoot.gameObject);
        }
        #endregion

        #region Private Methods
        private void PlaceFoliageForLayer(int textureLayer)
        {
            LayerTerrain layer = TerrainLayers[textureLayer];
            List<FoliagePrefabEntry> entries = layer.FoliagePrefabs;
            if (entries == null || entries.Count == 0) return;

            TerrainData terrainData = terrain.terrainData;
            float[,,] alphaMapData = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);
            Texture2D alphaTex = BuildAlphaTexture(alphaMapData, textureLayer);

            int gridResolution = Mathf.Clamp(layer.placementResolution, 16, 1024);
            TextureScale.Bilinear(alphaTex, gridResolution, gridResolution);

            string layerName = SanitizeName(layer.name);
            Transform layerRoot = GetOrCreateChild(generatedRoot, layerName);

            // Un container par prefab pour garder la hiérarchie organisée
            Transform[] prefabRoots = new Transform[entries.Count];
            for (int e = 0; e < entries.Count; e++)
            {
                if (entries[e].prefab == null) continue;
                prefabRoots[e] = GetOrCreateChild(layerRoot, SanitizeName(entries[e].prefab.name) + "_" + e);
            }

            Vector3 terrainSize = terrainData.size;
            Vector3 terrainPos = terrain.transform.position;
            float cellSizeX = terrainSize.x / gridResolution;
            float cellSizeZ = terrainSize.z / gridResolution;
            float densityMultiplier = layer.DensityMultiplier;

            List<int> qualifying = new List<int>(entries.Count);

            for (int gx = 0; gx < gridResolution; gx++)
            {
                for (int gz = 0; gz < gridResolution; gz++)
                {
                    float a = alphaTex.GetPixel(gx, gz).a;

                    // Coordonnées normalisées pour interroger la pente réelle du terrain
                    float u = (gx + 0.5f) / gridResolution;
                    float v = (gz + 0.5f) / gridResolution;
                    float slopeDegrees = terrainData.GetSteepness(u, v);

                    qualifying.Clear();
                    for (int e = 0; e < entries.Count; e++)
                    {
                        FoliagePrefabEntry entry = entries[e];
                        if (entry.prefab == null) continue;
                        if (slopeDegrees < entry.minSlope || slopeDegrees > entry.maxSlope) continue;

                        bool pass = entry.fillType == FillType.full
                            ? a > entry.fallOff
                            : (a >= 0.999f && HasNeighborBelowThreshold(alphaTex, gx, gz, gridResolution));

                        if (pass) qualifying.Add(e);
                    }

                    if (qualifying.Count == 0) continue;

                    // On choisit un seul modèle au hasard parmi ceux qui qualifient pour cette cellule
                    int chosen = qualifying[Random.Range(0, qualifying.Count)];
                    FoliagePrefabEntry chosenEntry = entries[chosen];

                    float spawnChance = Mathf.Clamp01((chosenEntry.density * densityMultiplier) / MAX_AMOUNT);
                    if (Random.value > spawnChance) continue;

                    float worldX = terrainPos.x + (gx + Random.value) * cellSizeX;
                    float worldZ = terrainPos.z + (gz + Random.value) * cellSizeZ;
                    float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrainPos.y;
                    Vector3 worldPos = new Vector3(worldX, worldY, worldZ);

                    TrySpawnFoliage(chosenEntry, worldPos, prefabRoots[chosen]);
                }
            }

            DestroyTexture(alphaTex);
        }
        private bool HasNeighborBelowThreshold(Texture2D tex, int x, int y, int size)
        {
            int x0 = Mathf.Max(x - 1, 0), x1 = Mathf.Min(x + 1, size - 1);
            int y0 = Mathf.Max(y - 1, 0), y1 = Mathf.Min(y + 1, size - 1);
            return tex.GetPixel(x0, y).a < 1 || tex.GetPixel(x1, y).a < 1 ||
                   tex.GetPixel(x, y0).a < 1 || tex.GetPixel(x, y1).a < 1;
        }
        private void TrySpawnFoliage(FoliagePrefabEntry entry, Vector3 worldPos, Transform parent)
        {
            Quaternion rot = Quaternion.identity;

            if (alignToTerrainNormal)
            {
                float u = (worldPos.x - terrain.transform.position.x) / terrain.terrainData.size.x;
                float v = (worldPos.z - terrain.transform.position.z) / terrain.terrainData.size.z;
                Vector3 normal = terrain.terrainData.GetInterpolatedNormal(u, v);
                rot = Quaternion.FromToRotation(Vector3.up, normal);
            }

            if (entry.randomRotationY)
            {
                rot *= Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }

            float scale = Random.Range(entry.uniformScaleRange.x, entry.uniformScaleRange.y);

            GameObject instance = Object.Instantiate(entry.prefab, worldPos, rot, parent);
            instance.transform.localScale = Vector3.one * scale;

            if (IsColliding(instance, entry.collisionCheckRadius, worldPos))
            {
                DestroyGameObjectImmediate(instance);
            }
        }

        // Vérifie que les colliders de l'instance ne chevauchent rien d'autre (hors terrain et hors elle-même)
        private bool IsColliding(GameObject instance, float fallbackRadius, Vector3 worldPos)
        {
            Physics.SyncTransforms();
            Collider[] ownColliders = instance.GetComponentsInChildren<Collider>();

            if (ownColliders.Length == 0)
            {
                Collider[] hits = Physics.OverlapSphere(worldPos, fallbackRadius, collisionCheckLayers, QueryTriggerInteraction.Ignore);
                foreach (var hit in hits)
                {
                    if (hit.GetComponent<TerrainCollider>() != null) continue;
                    return true;
                }
                return false;
            }

            foreach (var col in ownColliders)
            {
                Collider[] hits = Physics.OverlapBox(col.bounds.center, col.bounds.extents, col.transform.rotation, collisionCheckLayers, QueryTriggerInteraction.Ignore);
                foreach (var hit in hits)
                {
                    if (hit.transform.IsChildOf(instance.transform)) continue;
                    if (hit.GetComponent<TerrainCollider>() != null) continue;
                    return true;
                }
            }

            return false;
        }

        // =======================  UTILS  ========================

        private void EnsureGeneratedRoot()
        {
            if (generatedRoot != null) return;

            Transform existing = transform.Find("GeneratedFoliage");
            if (existing != null)
            {
                generatedRoot = existing;
            }
            else
            {
                GameObject go = new GameObject("GeneratedFoliage");
                go.transform.SetParent(transform, false);
                generatedRoot = go.transform;
            }
        }

        private Transform GetOrCreateChild(Transform parent, string childName)
        {
            Transform child = parent.Find(childName);
            if (child == null)
            {
                GameObject go = new GameObject(childName);
                go.transform.SetParent(parent, false);
                child = go.transform;
            }
            return child;
        }

        private string SanitizeName(string name) => string.IsNullOrEmpty(name) ? "Unnamed" : name;

        // ATTENTION: Unity retourne le tableau d'alphamaps indexé [y, x, layer] et non [x, y, layer].
        private Texture2D BuildAlphaTexture(float[,,] alphaMapData, int textureLayer)
        {
            int width = alphaMapData.GetLength(1);
            int height = alphaMapData.GetLength(0);

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] colors = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    colors[y * width + x] = new Color(0f, 0f, 0f, alphaMapData[y, x, textureLayer]);
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            return tex;
        }

        private void DestroyTexture(Object obj)
        {
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }

        private void DestroyGameObjectImmediate(GameObject go)
        {
            if (Application.isPlaying) Object.Destroy(go);
            else Object.DestroyImmediate(go);
        }

        private void OnValidate()
        {
            if (!terrain)
            {
                terrain = GetComponentInChildren<Terrain>();
                if (!terrain)
                {
                    terrain = GetComponentInParent<Terrain>();
                    if (!terrain) terrain = FindAnyObjectByType<Terrain>();
                }
            }

            if (!terrain || terrain.terrainData == null) return;

            while (TerrainLayers.Count < terrain.terrainData.terrainLayers.Length)
                TerrainLayers.Add(new LayerTerrain());
            while (TerrainLayers.Count > terrain.terrainData.terrainLayers.Length)
                TerrainLayers.RemoveAt(TerrainLayers.Count - 1);

            int i = 0;
            foreach (var terrainL in TerrainLayers)
            {
                terrainL.name = terrain.terrainData.terrainLayers[i].name;
                i++;
            }
        }
        #endregion
    }
}