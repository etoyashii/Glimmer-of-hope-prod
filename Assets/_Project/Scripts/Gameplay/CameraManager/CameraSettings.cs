using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Données de configuration passées à CameraManager.ConfigureCamera().
    /// Tous les champs sont optionnels : seuls les champs non-null sont appliqués.
    /// </summary>
    public class CameraSettings
    {
        #region Public Properties
        public Transform Follow;          // cible de déplacement
        public Vector3? FollowOffset;    // offset par rapport à la cible
        public Transform LookAt;          // cible de regard (fallback : Follow)
        public float? FOV;             // champ de vision en degrés
        public Vector3? PositionDamping; // inertie de position (x, y, z)
        public float? RotationDamping; // inertie de rotation
        #endregion

        #region Public Methods
        /// Follow + offset, LookAt automatiquement sur la même cible.
        public static CameraSettings WithFollow(Transform target, Vector3 offset) => new()
        {
            Follow = target,
            FollowOffset = offset,
            LookAt = target
        };

        /// Follow et LookAt sur deux cibles distinctes.
        public static CameraSettings WithFollowAndLookAt(Transform follow, Transform lookAt, Vector3 offset) => new()
        {
            Follow = follow,
            FollowOffset = offset,
            LookAt = lookAt
        };

        /// Modifie uniquement le FOV.
        public static CameraSettings WithFOV(float fov) => new() { FOV = fov };

        /// Modifie uniquement le damping de position et de rotation.
        public static CameraSettings WithDamping(Vector3 posDamping, float rotDamping) => new()
        {
            PositionDamping = posDamping,
            RotationDamping = rotDamping
        };
        #endregion
    }
}