using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    // Prevents adding multiple instances on the same GameObject to avoid mesh baking conflicts
    [DisallowMultipleComponent]
    public class BlenderMeshFixer : MonoBehaviour
    {
        #region Public Properties
        [Header("Source Mesh Import (Do not modify manually)")]
        [Tooltip("Original mesh from Blender import - automatically assigned during the first bake. Never assign a baked mesh here.")]
        public Mesh importMesh; // Reference used as the baseline for iterative baking operations
        #endregion
    }
}