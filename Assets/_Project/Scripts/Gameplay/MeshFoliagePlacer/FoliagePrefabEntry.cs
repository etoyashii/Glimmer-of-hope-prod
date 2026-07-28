using GlimmerOfHope.Core;
using System.Collections.Generic;
using UnityEngine;
using static GlimmerOfHope.Gameplay.MassiveFoliageMeshPlacer;
using static GlimmerOfHope.Gameplay.FoliagePlacementAlgorithm;

namespace GlimmerOfHope.Gameplay
{
     
    [System.Serializable]
    public class FoliagePrefabEntry
    {
        #region public Properties
       
        [PreviewPrefab(width = 60, height = 60)]
        public GameObject prefab; // The prefab reference of the foliage
        
        [Range(1, FoliagePlacementAlgorithm.MAX_AMOUNT)]
        [Tooltip("Controls the spawn probability of this prefab when it is chosen on a valid cell.")]
        public int density = 50; // The level of density you want for this prefab in the layer
        
        [Range(0, 1)]
        public float fallOff = 0.8f; // Minimum texture alpha required on a cell for this prefab to be spawn there (0 = spawns even innn transition areas, 1 = only in fully pure/dominant areas)        
        [Tooltip("full = the entire area covered by the texture. sides = only the edges/transitions of the texture.")]
        public FillType fillType = FillType.full; // Defines which part of the texture zone is used for spawning
        
        [Tooltip("Uniform min/max scale randomly applied to each instance.")]
        public Vector2 uniformScaleRange = new Vector2(0.85f, 1.15f); // Random uniform scale range applied per instance
       
        public bool randomRotationY = true; // Whether to apply a random Y rotation to each instance
       
        [Tooltip("Radius used for the collision check if the prefab has no Collider.")]
        public float collisionCheckRadius = 0.5f; // Fallback collision check radius when no Collider is present
        
        [Header("Pente")]
        [Range(0, 90)]
        [Tooltip("Minimum slope angle (degrees) required to allow spawning here.")]
        public float minSlope = 0f; // Minimum angle (degrees) allowed for spawning
        
        [Range(0, 90)]
        [Tooltip("Maximum slope angle (degrees) allowed for spawning here.")]
        public float maxSlope = 45f; // Maximum angle (degrees) allowed for spawning

        [Tooltip("Align tihs foliage to normal")]
        public bool AlignToNormal = true;
        #endregion
    }
}