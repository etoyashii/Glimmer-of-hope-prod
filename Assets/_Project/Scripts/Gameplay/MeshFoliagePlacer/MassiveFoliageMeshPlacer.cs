using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace GlimmerOfHope.Gameplay
{
    public class MassiveFoliageMeshPlacer : MonoBehaviour
    {
        #region Public Properties
        [Header("Attach The Terrain")]
        public Terrain terrain; // The terrain this placer will read from

        [Header("Select Target Texture and Source")]
        public List<LayerTerrain> TerrainLayers = new List<LayerTerrain>(); // One entry per terrain texture layer, auto-synced in OnValidate

        [Header("Global Parameters")]
        public bool EraseModification = true; // If true, previously generated foliage is deleted before regenerating

        [Tooltip("Physics layers tested during the anti-overlap check for foliage prefabs.")]
        public LayerMask collisionCheckLayers = ~0;

        public enum FillType { full, sides } //Different fill types

        public const int MAX_AMOUNT = 1000; // Reference max used to normalize "density" into a 0-1 spawn probability

        private Transform generatedRoot; // root transform holding all generated foliage ("GeneratedFoliage")

        #endregion

        #region Public Methods
        public void GenerateFoliageMeshes() // Generates foliage for every terrain layer
        {
            EnsureGeneratedRoot();
            if (EraseModification) CleanFoliageMeshes(-1);

            for (int layer = 0; layer < TerrainLayers.Count; layer++)
            {
                PlaceFoliageForLayer(layer);
            }
        }
        public void GenerateFoliageMeshesForLayer(int layerIndex)  // Generates foliage for a single specific layer only
        {
            EnsureGeneratedRoot();
            if (EraseModification) CleanFoliageMeshes(layerIndex);

            PlaceFoliageForLayer(layerIndex);
        }
        public void CleanFoliageMeshes(int layerIndex) // Removes generated foliage. Pass -1 to clear everything, or a valid layer index to clear only that layer
        {
            EnsureGeneratedRoot();

            if (layerIndex == -1)
            {
                // Destroy every child under the generated root (all layers)
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
            float[,,] alphaMapData = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight); // Read the raw splatmap (alpha weight per texture layer) for the whole terrain
            // Convert the weights of this specific texture layer into a grayscale/alpha texture
            Texture2D alphaTex = BuildAlphaTexture(alphaMapData, textureLayer);  

            // Resize the alpha texture to the desired placement grid resolution (bilinear = smoother sampling)
            int gridResolution = Mathf.Clamp(layer.placementResolution, 16, 1024);
            TextureScale.Bilinear(alphaTex, gridResolution, gridResolution);

            string layerName = SanitizeName(layer.name);
            Transform layerRoot = GetOrCreateChild(generatedRoot, layerName);

            // One container per prefab to keep the hierarchy organized
            Transform[] prefabRoots = new Transform[entries.Count];
            for (int e = 0; e < entries.Count; e++)
            {
                if (entries[e].prefab == null) continue;
                prefabRoots[e] = GetOrCreateChild(layerRoot, SanitizeName(entries[e].prefab.name) + "_" + e);
            }

            Vector3 terrainSize = terrainData.size;
            Vector3 terrainPos = terrain.transform.position;
            // World-space size of a single grid cell for X and Z
            float cellSizeX = terrainSize.x / gridResolution;
            float cellSizeZ = terrainSize.z / gridResolution;
            float densityMultiplier = layer.DensityMultiplier;

            // Reused list of prefab indices that qualify for the current cell
            List<int> qualifying = new List<int>(entries.Count);

            // Iterate over every cell of the placement grid
            for (int gx = 0; gx < gridResolution; gx++)
            {
                for (int gz = 0; gz < gridResolution; gz++)
                {
                    // Texture cover (0-1) of this layer at this cell
                    float a = alphaTex.GetPixel(gx, gz).a;

                    // Normalized coordinates 
                    float u = (gx + 0.5f) / gridResolution;
                    float v = (gz + 0.5f) / gridResolution;
                    float slopeDegrees = terrainData.GetSteepness(u, v);

                    qualifying.Clear();
                    // Check every prefab entry to see which ones are allowed to spawn on this cell
                    for (int e = 0; e < entries.Count; e++)
                    {
                        FoliagePrefabEntry entry = entries[e];
                        if (entry.prefab == null) continue;
                        if (slopeDegrees < entry.minSlope || slopeDegrees > entry.maxSlope) continue;

                        // full: cell must be covered enough by the texture (above the falloff threshold)
                        // sides: cell must be fully covered (a >= 0.999) AND touch a neighbor that isn't, i.e. be on the texture's edge
                        bool pass = entry.fillType == FillType.full
                            ? a > entry.fallOff
                            : (a >= (1-layer.SideSize) && HasNeighborBelowThreshold(alphaTex, gx, gz, gridResolution));

                        if (pass) qualifying.Add(e);
                    }

                    if (qualifying.Count == 0) continue;

                    // Pick a single random prefab among the ones that qualify for this cell
                    int chosen = qualifying[Random.Range(0, qualifying.Count)];
                    FoliagePrefabEntry chosenEntry = entries[chosen];

                    // Convert density (0-MAX_AMOUNT) into a 0-1 probability 
                    float alphaFactor = layer.AlphaDensity  ? Mathf.InverseLerp(chosenEntry.fallOff, 1f, a)  : 1f;
                    float spawnChance = Mathf.Clamp01((chosenEntry.density * densityMultiplier * alphaFactor) / MAX_AMOUNT);

                    if (Random.value > spawnChance) continue;

                    // Random inside the cell so instances don't align perfectly to the grid
                    float worldX = terrainPos.x + (gx + Random.value) * cellSizeX;
                    float worldZ = terrainPos.z + (gz + Random.value) * cellSizeZ;
                    float worldY = terrain.SampleHeight(new Vector3(worldX, 0, worldZ)) + terrainPos.y;
                    Vector3 worldPos = new Vector3(worldX, worldY, worldZ);

                    TrySpawnFoliage(chosenEntry, worldPos, prefabRoots[chosen]);
                }
            }
            DestroyTexture(alphaTex);
        }

        private bool HasNeighborBelowThreshold(Texture2D tex, int x, int y, int size) // Returns true if at least one of the 4 direct neighbors has a coverage below 1 
        {
            int x0 = Mathf.Max(x - 1, 0), x1 = Mathf.Min(x + 1, size - 1);
            int y0 = Mathf.Max(y - 1, 0), y1 = Mathf.Min(y + 1, size - 1);
            return tex.GetPixel(x0, y).a < 1 || tex.GetPixel(x1, y).a < 1 || tex.GetPixel(x, y0).a < 1 || tex.GetPixel(x, y1).a < 1;
        }

        // Instantiates the prefab at the given position with random rotation/scale, then removes it if it collides with something
        private void TrySpawnFoliage(FoliagePrefabEntry entry, Vector3 worldPos, Transform parent)
        {
            Quaternion rot = Quaternion.identity;

            if (entry.AlignToNormal)
            {
                // Sample the terrain normal at this world position and align the prefab's up axis to it
                float u = (worldPos.x - terrain.transform.position.x) / terrain.terrainData.size.x;
                float v = (worldPos.z - terrain.transform.position.z) / terrain.terrainData.size.z;
                Vector3 normal = terrain.terrainData.GetInterpolatedNormal(u, v);
                rot = Quaternion.FromToRotation(Vector3.up, normal);
            }

            if (entry.randomRotationY)
            {
                // Add a random spin around the Y axis on top of any normal alignment
                rot *= Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            }

            // Random uniform scale within the configured range
            float scale = Random.Range(entry.uniformScaleRange.x, entry.uniformScaleRange.y);

            // Instantiate while keeping a live prefab connection (blue link in the Hierarchy)
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, parent);
            instance.transform.SetPositionAndRotation(worldPos, rot);
            instance.transform.localScale = Vector3.one * scale;

            // Discard the instance if it overlaps another object (prevents foliage spawning inside rocks, buildings, etc.)
            if (IsColliding(instance, entry.collisionCheckRadius, worldPos))
            {
                DestroyGameObjectImmediate(instance);
            }
        }

        // Checks that the instance's colliders do not overlap anything else (excluding the terrain and itself)
        private bool IsColliding(GameObject instance, float fallbackRadius, Vector3 worldPos)
        {
            // Make sure physics transforms are up to date before running overlap queries
            Physics.SyncTransforms();
            Collider[] ownColliders = instance.GetComponentsInChildren<Collider>();

            if (ownColliders.Length == 0)
            {
                // No collider on the prefab: fall back to a simple sphere check using the configured radius
                Collider[] hits = Physics.OverlapSphere(worldPos, fallbackRadius, collisionCheckLayers, QueryTriggerInteraction.Ignore);
                foreach (var hit in hits)
                {
                    if (hit.GetComponent<TerrainCollider>() != null) continue; // Ignore the terrain itself
                    return true;
                }
                return false;
            }

            // Prefab has its own colliders: test each one individually with a box overlap matching its bounds
            foreach (var col in ownColliders)
            {
                Collider[] hits = Physics.OverlapBox(col.bounds.center, col.bounds.extents, col.transform.rotation, collisionCheckLayers, QueryTriggerInteraction.Ignore);
                foreach (var hit in hits)
                {
                    if (hit.transform.IsChildOf(instance.transform)) continue; // Ignore self-collision
                    if (hit.GetComponent<TerrainCollider>() != null) continue; // Ignore the terrain itself
                    return true;
                }
            }

            return false;
        }

        // Makes sure the "GeneratedFoliage" root transform exists (finds it or creates it), and caches it
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

        // Returns an existing child transform by name, or creates a new empty one if it doesn't exist yet
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

        // Returns a safe fallback name if the given name is null or empty
        private string SanitizeName(string name) => string.IsNullOrEmpty(name) ? "Unnamed" : name;

        // Builds a texture whose alpha channel represents the coverage (0-1) of a single terrain texture layer
        private Texture2D BuildAlphaTexture(float[,,] alphaMapData, int textureLayer)  // WARNING: Unity returns the alphamap array indexed as [y, x, layer], not [x, y, layer].
        {
            int width = alphaMapData.GetLength(1);
            int height = alphaMapData.GetLength(0);

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] colors = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // RGB is unused, only alpha (the layer's weight) matters here
                    colors[y * width + x] = new Color(0f, 0f, 0f, alphaMapData[y, x, textureLayer]);
                }
            }

            tex.SetPixels(colors);
            tex.Apply();
            return tex;
        }

        // Destroys a temporary Unity object, using the correct method depending on edit mode vs play mode
        private void DestroyTexture(Object obj)
        {
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }

        // Destroys a GameObject, using the correct method depending on edit mode vs play mode
        private void DestroyGameObjectImmediate(GameObject go)
        {
            if (Application.isPlaying) Object.Destroy(go);
            else Object.DestroyImmediate(go);
        }

        // Called automatically by Unity whenever a field is changed in the inspector, or on load
        private void OnValidate()
        {
            // Try to auto-find a terrain reference if none is assigned yet
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

            // Keep TerrainLayers in sync with the actual number of texture layers on the terrain
            while (TerrainLayers.Count < terrain.terrainData.terrainLayers.Length)
            { TerrainLayers.Add(new LayerTerrain()); }

            while (TerrainLayers.Count > terrain.terrainData.terrainLayers.Length)
            { TerrainLayers.RemoveAt(TerrainLayers.Count - 1); }

            // Refresh each layer entry's display name to match the terrain's actual texture layer name
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