using UnityEngine;
using GlimmerOfHope.Gameplay.Character.SpecialActions;

namespace GlimmerOfHope.Gameplay.Triggers
{
    public class JumpTrigger : MonoBehaviour
    {
        #region SerializeFields

        [Header("Destination")]
        [Tooltip("Empty Transform placed exactly where the player should land.")]
        [SerializeField] Transform _landingPoint;

        [Header("Required Direction (Optional)")]
        [Tooltip("If enabled, the jump only triggers if the player is moving in the correct direction.")]
        [SerializeField] bool _checkDirection = true;

        [Tooltip("Required movement direction to clear this gap (e.g., (1,0,0) = right).")]
        [SerializeField] Vector3 _requiredDirection = Vector3.forward;

        [Tooltip("Angular tolerance in degrees.")]
        [Range(10f, 90f)]
        [SerializeField] float _directionTolerance = 45f;

        #endregion

        #region Private Methods

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent<Movement>(out var movement)) return;
            if (!other.TryGetComponent<SkillManager>(out var skills)) return;
            if (!other.TryGetComponent<Jump>(out var jump)) return;

            if (!skills.HasJump) return;

            if (_landingPoint == null)
            {
                Debug.LogWarning($"[JumpTrigger] {gameObject.name} : no landing point assigned !", this);
                return;
            }

            if (_checkDirection)
            {
                Vector3 playerDir = movement.MoveDirection;

                if (playerDir == Vector3.zero) return;
                if (Vector3.Angle(playerDir, _requiredDirection) > _directionTolerance) return;
            }

            jump.TriggerJump(_landingPoint.position);
        }

        #endregion

        #region Editor

        private void OnValidate()
        {
            if (_requiredDirection != Vector3.zero)
                _requiredDirection = _requiredDirection.normalized;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_landingPoint == null) return;

            Gizmos.color = Color.cyan;
            Vector3 start = transform.position;
            Vector3 end = _landingPoint.position;
            int segments = 20;

            for (int i = 0; i < segments; i++)
            {
                float t0 = (float)i / segments;
                float t1 = (float)(i + 1) / segments;
                Vector3 p0 = Vector3.Lerp(start, end, t0);
                Vector3 p1 = Vector3.Lerp(start, end, t1);
                p0.y += 1.8f * Mathf.Sin(t0 * Mathf.PI);
                p1.y += 1.8f * Mathf.Sin(t1 * Mathf.PI);
                Gizmos.DrawLine(p0, p1);
            }

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_landingPoint.position, 0.2f);

            if (_checkDirection)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, _requiredDirection * 1.5f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
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