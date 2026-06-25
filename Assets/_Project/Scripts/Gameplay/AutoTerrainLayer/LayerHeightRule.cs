using UnityEngine;

namespace GlimmerOfHope.Gameplay.AutoTerrainLayer
{
    /// <summary>
    /// This class Contains the rules that should be applied to a specific layer
    /// </summary>
    [System.Serializable]
    public class LayerHeightRule
    {
        #region Public Properties
        public TerrainLayer layer; // The TerrainLayer to Apply
        [Range(0f, 600f), Tooltip("Hauteur minimale World (0 = bas, 600 = haut)")]
        public float minHeight;     // The Min Height of the layer to apply
        [Range(0f, 600f), Tooltip("Hauteur maximale World (0 = bas, 600 = haut)")]
        public float maxHeight;     // The Max Height of the layer to apply
        #endregion
    }
}