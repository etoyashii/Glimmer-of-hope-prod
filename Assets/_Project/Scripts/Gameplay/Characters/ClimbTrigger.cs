using UnityEngine;
using GlimmerOfHope.Gameplay.Character.SpecialActions;

namespace GlimmerOfHope.Gameplay.Triggers
{
    public class ClimbTrigger : MonoBehaviour
    {
        #region SerializeFields

        [Header("Wall")]

        [Tooltip("Wall normal: direction perpendicular to the wall, pointing TOWARD the player. " +
         "Example: facade perpendicular to the z-axis -> normal = (0, 0, -1).")]
        [SerializeField] private Vector3 _wallNormal = Vector3.back;

        [Header("Destination (optional)")]

        [Tooltip("Empty Transform placed at the top of the wall, where the player should land. " +
                 "If not assigned, the destination is calculated using " +
                 "the _climbAngle and _climbDistance parameters.")]
        [SerializeField] private Transform _landingPoint;

        [Header("Required Direction (optional)")]

        [Tooltip("If enabled, climbing is only triggered if the player is moving toward the wall.")]
        [SerializeField] private bool _checkDirection = true;

        [Tooltip("Required direction to trigger climbing (must point toward the wall).")]
        [SerializeField] private Vector3 _requiredDirection = Vector3.forward;

        [Tooltip("Tolerance for the direction check.")]
        [Range(10f, 90f)]
        [SerializeField] private float _directionTolerance = 50f;

        #endregion

        #region Private Methods

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<Movement>(out var movement)) return;
            if (!other.TryGetComponent<SkillManager>(out var skills)) return;
            if (!other.TryGetComponent<Climb>(out var climb)) return;

            if (!skills.HasClimb) return;

            if (_checkDirection)
            {
                Vector3 playerDir = movement.MoveDirection;

                if (playerDir == Vector3.zero) return;
                if (Vector3.Angle(playerDir, _requiredDirection) > _directionTolerance) return;
            }

            climb.TriggerClimb(_wallNormal, _landingPoint);
        }

        #endregion

        #region Editor

        private void OnValidate()
        {
            if (_wallNormal != Vector3.zero)
                _wallNormal = _wallNormal.normalized;

            if (_requiredDirection != Vector3.zero)
                _requiredDirection = _requiredDirection.normalized;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Wall Normal
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, _wallNormal * 1.2f);

            // Required Direction
            if (_checkDirection)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, _requiredDirection * 1.2f);
            }

            // Landing point
            if (_landingPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(_landingPoint.position, 0.2f);
                Gizmos.DrawLine(transform.position, _landingPoint.position);

                // Curve previsualisation
                var player = FindFirstObjectByType<Climb>();
                if (player != null)
                    player.DrawClimbPreview(transform.position, _landingPoint.position, _wallNormal);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.5f, 0f, 1f, 0.2f);
            if (TryGetComponent<BoxCollider>(out var box))
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
            }
        }
#endif
    }

        #endregion
}
