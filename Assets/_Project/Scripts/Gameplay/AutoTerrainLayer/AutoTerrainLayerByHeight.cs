using UnityEngine;
using System.Linq;

namespace GlimmerOfHope.Gameplay.AutoTerrainLayer
{    
    #region Dependencies
    [RequireComponent(typeof(Terrain))]
    #endregion
    /// <summary>
    /// This Class is a Component that allow you to directly apply multiple layer to a unity terrain.
    /// You can also decide of the minimum and maximum height of each layers 
    /// </summary>
    public class AutoTerrainLayerByHeight : MonoBehaviour
    {        
        #region Public Properties
        public LayerHeightRule[] rules; // Table of rules (layer + height range), sorted from lowest to highest
        #endregion

        #region Private Fields

        private Terrain terrain;
        private float[,,] originalAlphamaps; //We Save the original maps for the undo
        private bool hasApplied = false;
        #endregion

        #region Private Methods
        private void SaveOriginalAlphamaps()
        {
            TerrainData terrainData = terrain.terrainData; 
            originalAlphamaps = terrainData.GetAlphamaps(
                0, 0,
                terrainData.alphamapWidth,
                terrainData.alphamapHeight
            );// Backup of the latest OriginalMaps to update the latest states to undo
        }
        #endregion

        #region Public Methods
      
        /// <summary>
        /// Apply the layers according to the heights
        /// </summary>
        public void ApplyLayersByHeight()
        {
            if (terrain == null)
                terrain = GetComponent<Terrain>();
            TerrainData terrainData = terrain.terrainData;
            // Adds the missing layers to the terrain
            foreach (var rule in rules)
            {
                if (rule.layer == null)
                {
                    Debug.LogError("Un TerrainLayer est manquant dans les règles !");
                    return;
                }
                if (!terrainData.terrainLayers.Contains(rule.layer))
                {
                    terrainData.terrainLayers = terrainData.terrainLayers.Append(rule.layer).ToArray();
                }
            }
            // Save for Undo
            SaveOriginalAlphamaps();

            hasApplied = true;

            int alphamapWidth = terrainData.alphamapWidth;
            int alphamapHeight = terrainData.alphamapHeight;
            int heightmapWidth = terrainData.heightmapResolution;
            int heightmapHeight = terrainData.heightmapResolution;

            float[,,] alphamaps = terrainData.GetAlphamaps(0, 0, alphamapWidth, alphamapHeight);
            float[,] heights = terrainData.GetHeights(0, 0, heightmapWidth, heightmapHeight);

            // Retrieves the indexes of the rule layers
            int[] layerIndices = new int[rules.Length];
            for (int i = 0; i < rules.Length; i++)
            {
                layerIndices[i] = System.Array.IndexOf(terrainData.terrainLayers, rules[i].layer);
                if (layerIndices[i] == -1)
                {
                    Debug.LogError($"Le TerrainLayer {rules[i].layer.name} est introuvable !");
                    return;
                }
            }
            for (int y = 0; y < alphamapHeight; y++)
            {
                for (int x = 0; x < alphamapWidth; x++)
                {
                    int heightX = (int)(x * (float)heightmapWidth / alphamapWidth);
                    int heightY = (int)(y * (float)heightmapHeight / alphamapHeight);
                    float height = heights[heightY, heightX];
                    // Checks if the height is within one of the ranges
                    bool isInRuleRange = false;
                    for (int i = 0; i < rules.Length; i++)
                    {
                        float min = rules[i].minHeight / 600f;
                        float max = rules[i].maxHeight / 600f;
                        if (height >= min && height <= max)
                        {
                            // Resets ALL layers for this point
                            for (int layer = 0; layer < terrainData.alphamapLayers; layer++)
                            {
                                alphamaps[y, x, layer] = 0;
                            }

                            float center = (min + max) * 0.5f;
                            float halfRange = (max - min) * 0.5f;
                            float opacity = 1f;

                            if (halfRange > 0f)
                            {
                                if (height > center && i < rules.Length - 1)
                                {
                                    // fade to the next top layer, only if one exists
                                    opacity = 1f - (height - center) / halfRange;
                                    opacity = Mathf.Clamp01(opacity);
                                    alphamaps[y, x, layerIndices[i + 1]] = 1f - opacity;
                                }
                            }

                            // Activate the corresponding layer with its opacity
                            alphamaps[y, x, layerIndices[i]] = opacity;
                            isInRuleRange = true;
                            break;
                        }
                    }
                    // If the height is not within ANY range, the alphamaps are NOT modified.
                }
            }
            terrainData.SetAlphamaps(0, 0, alphamaps);
        }
        /// <summary>
        /// Cancel changes
        /// </summary>
        public void UndoLayers()
        {
            if (originalAlphamaps != null && terrain != null && hasApplied)
            {
                terrain.terrainData.SetAlphamaps(0, 0, originalAlphamaps);
                hasApplied = false;
            }
            else
            {
                Debug.LogWarning("Rien à annuler !");
            }
        }
        #endregion
    }
}