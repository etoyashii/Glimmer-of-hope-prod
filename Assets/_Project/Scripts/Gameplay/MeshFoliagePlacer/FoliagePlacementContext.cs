using UnityEngine;
namespace GlimmerOfHope.Gameplay
{
    public readonly struct FoliagePlacementContext
    {
        #region Public Properties
        public readonly Terrain terrain; // Terrain being read (splatmap, height, normals,...)
        public readonly LayerMask collisionCheckLayers; // Physics layers tested during the anti-overlap check
        #endregion
        #region Public Methods
        public FoliagePlacementContext(Terrain terrain, LayerMask collisionCheckLayers)
        {
            this.terrain = terrain;
            this.collisionCheckLayers = collisionCheckLayers;
        }
        #endregion
    }
}