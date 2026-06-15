using UnityEngine;

namespace GlimmerOfHope.Gameplay.Character.SpecialActions
{
    /// <summary>
    /// Directional air current zone. Attach to a GameObject with a Trigger Collider.
    /// Pushes the player continuously in any direction while inside the zone.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class AirCurrent : MonoBehaviour
    {
        #region SerializeField

        [Header("Air Current Settings")]
        [Tooltip("Direction of the air current in world space. Will be normalized automatically.")]
        [SerializeField] private Vector3 _direction = Vector3.up;

        [Range(0f, 50f)]
        [Tooltip("Intensity of the air current force.")]
        [SerializeField] private float _force = 15f;

        [Header("References")]
        [Tooltip("Tag used to identify the player GameObject.")]
        [SerializeField] private string _playerTag = "Player";

        #endregion

        #region Private Properties

        private Vector3 ForceVector => _direction.normalized * _force;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (!col.isTrigger)
            {
                col.isTrigger = true;
                Debug.LogWarning($"[AirCurrent] Collider on {gameObject.name} was not a trigger — fixed automatically.");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(_playerTag)) return;

            if (other.TryGetComponent(out Movement movement))
                movement.SetAirCurrent(true, ForceVector);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(_playerTag)) return;

            if (other.TryGetComponent(out Movement movement))
                movement.SetAirCurrent(false);
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            Vector3 origin = transform.position;
            Vector3 dir = _direction.normalized;

            // Zone fill
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.2f);
            Collider col = GetComponent<Collider>();
            if (col is BoxCollider box)
                Gizmos.DrawCube(transform.position + box.center, Vector3.Scale(box.size, transform.lossyScale));
            else if (col is SphereCollider sphere)
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
            else if (col is CapsuleCollider capsule)
                Gizmos.DrawWireSphere(transform.position + capsule.center, capsule.radius * transform.lossyScale.x);

            // Direction arrow
            float arrowLength = Mathf.Clamp(_force * 0.15f, 0.5f, 4f);
            float headSize = arrowLength * 0.25f;

            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.95f);
            Vector3 tip = origin + dir * arrowLength;
            Gizmos.DrawLine(origin, tip);

            Vector3 perp1 = Vector3.Cross(dir, dir == Vector3.up ? Vector3.right : Vector3.up).normalized;
            Vector3 perp2 = Vector3.Cross(dir, perp1).normalized;
            Gizmos.DrawLine(tip, tip - dir * headSize + perp1 * headSize * 0.5f);
            Gizmos.DrawLine(tip, tip - dir * headSize - perp1 * headSize * 0.5f);
            Gizmos.DrawLine(tip, tip - dir * headSize + perp2 * headSize * 0.5f);
            Gizmos.DrawLine(tip, tip - dir * headSize - perp2 * headSize * 0.5f);

#if UNITY_EDITOR
            UnityEditor.Handles.color = new Color(0.4f, 0.8f, 1f, 1f);
            UnityEditor.Handles.Label(origin + dir * (arrowLength + 0.2f), $"{_force:F1} N");
#endif
        }

        #endregion
    }
}