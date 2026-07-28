using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class MassiveFoliageMeshPlacer : MonoBehaviour
    {
        /// <summary>
        /// A Massive Foliage placer that scatters foliage prefabs across a terrain based on its
        /// texture layers per-prefab density rules.
        /// </summary>
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

            string layerName = SanitizeName(layer.name);
            Transform layerRoot = GetOrCreateChild(generatedRoot, layerName);

            // One container per prefab to keep the hierarchy organized
            Transform[] prefabRoots = new Transform[entries.Count];
            for (int e = 0; e < entries.Count; e++)
            {
                if (entries[e].prefab == null) continue;
                prefabRoots[e] = GetOrCreateChild(layerRoot, SanitizeName(entries[e].prefab.name) + "_" + e);
            }

            var context = new FoliagePlacementContext(terrain, collisionCheckLayers);
            var algorithm = new FoliagePlacementAlgorithm(context);
            algorithm.PlaceFoliageForLayer(layer, textureLayer, prefabRoots);
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