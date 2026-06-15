using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class CameraSettings
    {
        #region Public Properties
        public Transform Follow;
        public Vector3? FollowOffset;
        public Transform LookAt;
        public float? FOV;
        public Vector3? PositionDamping;
        public float? RotationDamping;
        #endregion

        #region Public Methods
        public static CameraSettings WithFollow(Transform target, Vector3 offset) => new()
        {
            Follow = target,
            FollowOffset = offset,
            LookAt = target
        };

        public static CameraSettings WithFollowAndLookAt(Transform follow, Transform lookAt, Vector3 offset) => new()
        {
            Follow = follow,
            FollowOffset = offset,
            LookAt = lookAt
        };

        public static CameraSettings WithFOV(float fov) => new() { FOV = fov };
        public static CameraSettings WithDamping(Vector3 posDamping, float rotDamping) => new() { PositionDamping = posDamping, RotationDamping = rotDamping };
        #endregion
    }
}
