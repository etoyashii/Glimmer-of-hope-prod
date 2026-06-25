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
        public float minHeight;     // The Min Height of the layer to apply
        public float maxHeight;     // The Max Height of the layer to apply
        #endregion
    }
}