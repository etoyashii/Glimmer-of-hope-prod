using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    [System.Serializable]
    public class LayerTerrain
    {
        #region Public Properties
        [HideInInspector]
        public string name;

        [Tooltip("Multiplie la densité de tous les prefabs de foliage de ce layer de texture.")]
        [Range(0f, 3f)]
        public float DensityMultiplier = 1f;

        [Range(16, 1024)]
        [Tooltip("Finesse de la grille de placement pour ce layer. Plus haut = placements plus précis mais plus lents à générer.")]
        public int placementResolution = 128;

        public List<FoliagePrefabEntry> FoliagePrefabs = new List<FoliagePrefabEntry>();
        #endregion
    }
}
