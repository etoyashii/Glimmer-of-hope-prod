using UnityEngine;

namespace GlimmerOfHope.Audio
{
    /// <summary>
    /// Utilitaire statique partagé pour détecter le Terrain Layer dominant sous une
    /// position donnée, en gérant plusieurs Terrain dans la scène. Utilisé à la fois
    /// par la musique d'ambiance (TerrainMusicZone) et les bruits de pas
    /// (FootstepAudioSystem) — évite de dupliquer la logique de lecture d'alphamap.
    /// </summary>
    public static class TerrainLayerUtility
    {
        public static Terrain FindTerrainUnderPosition(Vector3 worldPos)
        {
            foreach (Terrain t in Terrain.activeTerrains)
            {
                Vector3 local = worldPos - t.transform.position;
                Vector3 size = t.terrainData.size;
                if (local.x >= 0f && local.x <= size.x && local.z >= 0f && local.z <= size.z)
                {
                    return t;
                }
            }
            return null;
        }

        /// <summary>Retourne le TerrainLayer dominant à la position donnée, ou null si
        /// aucun Terrain ne couvre cette position (ex : sol non-Terrain, mesh classique).</summary>
        public static TerrainLayer GetDominantTerrainLayer(Vector3 worldPos)
        {
            Terrain terrain = FindTerrainUnderPosition(worldPos);
            if (terrain == null) { return null; }

            TerrainData data = terrain.terrainData;
            Vector3 localPos = worldPos - terrain.transform.position;

            int mapX = Mathf.Clamp(Mathf.FloorToInt((localPos.x / data.size.x) * data.alphamapWidth), 0, data.alphamapWidth - 1);
            int mapZ = Mathf.Clamp(Mathf.FloorToInt((localPos.z / data.size.z) * data.alphamapHeight), 0, data.alphamapHeight - 1);

            float[,,] alphamaps = data.GetAlphamaps(mapX, mapZ, 1, 1);
            int layerCount = alphamaps.GetLength(2);

            int dominantIndex = 0;
            float maxWeight = 0f;
            for (int i = 0; i < layerCount; i++)
            {
                if (alphamaps[0, 0, i] > maxWeight)
                {
                    maxWeight = alphamaps[0, 0, i];
                    dominantIndex = i;
                }
            }

            TerrainLayer[] layers = data.terrainLayers;
            if (dominantIndex >= 0 && dominantIndex < layers.Length)
            {
                return layers[dominantIndex];
            }
            return null;
        }
    }
}
