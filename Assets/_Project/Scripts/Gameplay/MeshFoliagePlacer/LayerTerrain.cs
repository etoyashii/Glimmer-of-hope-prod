using System.Collections.Generic;
using UnityEngine;
namespace GlimmerOfHope.Gameplay
{
    [System.Serializable]
    public class LayerTerrain
    {
        #region Public Properties
        [HideInInspector]
        public string name; // Name of the layer
        [Tooltip("Multiplies the density of all foliage prefabs in this texture layer.")]
        [Range(0f, 3f)]
        public float DensityMultiplier = 1f; // The density multiplier which will be applied to all the prefabs in the layer
        [Range(16, 1024)]
        [Tooltip("Placement grid resolution for this layer. Higher = more precise placements but slower to generate.")]
        public int placementResolution = 128; // The placement grid resolution, a higher resolution means more precision and more mesh but also more time to generate.
        public List<FoliagePrefabEntry> FoliagePrefabs = new List<FoliagePrefabEntry>(); // List all the Foliage prefag of this layer
        [Range(0f, 1f)]
        public float SideSize = 0.05f; // The size of the side

        [Tooltip("If enabled,The density of the Foliage on the layer will be proportional to the alpha of the layer.")]
        public bool AlphaDensity = false;
        #endregion
    }
}