using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Configuration data passed to CameraManager.ConfigureCamera().
    /// All fields are optional: only non-null fields are applied.
    /// </summary>
    public class CameraSettings
    {
        #region Public Properties
        public Transform Follow;          // movement target
        public Vector3? FollowOffset;    // offset relative to the target
        public Transform LookAt;          // look-at target (fallback: Follow)
        public float? FOV;             // field of view in degrees
        public Vector3? PositionDamping; // position inertia (x, y, z)
        public float? RotationDamping; // rotation inertia
        #endregion

        #region Public Methods
        /// Follow + offset, LookAt automatically set to the same target.
        public static CameraSettings WithFollow(Transform target, Vector3 offset) => new()
        {
            Follow = target,
            FollowOffset = offset,
            LookAt = target
        };

        /// Follow and LookAt on two separate targets.
        public static CameraSettings WithFollowAndLookAt(Transform follow, Transform lookAt, Vector3 offset) => new()
        {
            Follow = follow,
            FollowOffset = offset,
            LookAt = lookAt
        };

        /// Sets only the FOV.
        public static CameraSettings WithFOV(float fov) => new() { FOV = fov };

        /// Sets only the position and rotation damping.
        public static CameraSettings WithDamping(Vector3 posDamping, float rotDamping) => new()
        {
            PositionDamping = posDamping,
            RotationDamping = rotDamping
        };
        #endregion
    }
}