using System.Collections;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Skill that pushes "windpushable" tagged objects (need rigidbody) away from the caster 
    /// </summary>
    public class PushSkill : Skills
    {
        #region Serialized Fields
        [Header("Refs")]
        [Tooltip("Transform of the caster")]
        [SerializeField] private Transform _playerTransform;

        [Tooltip("VFX of the skill")]
        [SerializeField] private ParticleSystem _pushVFX;

        [Header("Shape of the push")]
        [Tooltip("Length of the push Cone in front of the caster")]
        [SerializeField] private float _pushLength = 8f;

        [Tooltip("Radius of the push Cone at its farthest from the caster")]
        [SerializeField] private float _pushRadius = 3f;

        [Tooltip("Layers taken in count in the raycast collision detection")]
        [SerializeField] private LayerMask _detectionMask = ~0;

        [Header("Strenght of the push")]
        [Tooltip("Total strenght applied to the pushable objects")]
        [SerializeField] private float _pushForce = 18f;

        [Tooltip("Upward Strenght to simulate an elevation")]
        [SerializeField] private float _upwardForce = 4f;

        [Tooltip("Falloff : Far objects receive less force (0= force nullified, 1= full force)")]
        [Range(0f, 1f)]
        [SerializeField] private float _distanceFalloff = 0.6f;

        [Header("Progressive Force Application")]
        [Tooltip("Activate the progressiv Force Application")]
        [SerializeField] private bool _useForceWave = true;

        [Tooltip("Total duration of the Force Application")]
        [SerializeField] private float _forceWaveDuration = 0.3f;

        [Tooltip("Number of impulse during the Force Application")]
        [SerializeField] private int _forcePulseCount = 3;
        #endregion

        #region Constants
        private const string PUSHABLE_TAG = "WindPushable";
        #endregion

        #region Public Methods

        public override void PerformSkill()
        {
            Push();
        }

        #endregion

        #region Private Methods
        private void Push()
        {

            Debug.Log("Executing Wind Push!");
            // Get the forward of the caster
            Vector3 forward = new Vector3(
                _playerTransform.forward.x,
                0f,
                _playerTransform.forward.z
            ).normalized;

            // Center of the Cone : halfway in front of the caster
            Vector3 capsuleCenter = _playerTransform.position + forward * (_pushLength * 0.5f);

            // Raycast in front of the caster
            Collider[] hits = Physics.OverlapCapsule(
                _playerTransform.position,
                capsuleCenter + forward * (_pushLength * 0.5f),
                _pushRadius,
                _detectionMask
            );

            // Play the VFX
            if (_pushVFX != null)
            {
/*                _pushVFX.transform.position = _playerTransform.position;
                _pushVFX.transform.rotation = Quaternion.LookRotation(forward);*/
                _pushVFX.Play();
            }

            // Filter the objects raycasted to get the pushables
            foreach (Collider col in hits)
            {
                if (!col.CompareTag(PUSHABLE_TAG)) continue;

                Rigidbody rb = col.attachedRigidbody;
                if (rb == null || rb.isKinematic) continue;

                // To verify that the object is in fact in front of the caster
                Vector3 toObject = (col.transform.position - _playerTransform.position).normalized;
                if (Vector3.Dot(forward, toObject) < 0f) continue;

                float distance = Vector3.Distance(_playerTransform.position, col.transform.position);
                float falloff = Mathf.Lerp(1f, 1f - _distanceFalloff, distance / _pushLength);
                float finalForce = _pushForce * falloff;

                // Push = forward + elevation
                Vector3 pushDir = (forward + Vector3.up * (_upwardForce / _pushForce)).normalized;

                if (_useForceWave)
                    StartCoroutine(ApplyGustWave(rb, pushDir, finalForce));
                else
                    rb.AddForce(pushDir * finalForce, ForceMode.Impulse);
            }
        }

        // to apply the force progressivcely.
        private IEnumerator ApplyGustWave(Rigidbody rb, Vector3 direction, float totalForce)
        {
            float forcePerPulse = totalForce / _forcePulseCount;
            float interval = _forceWaveDuration / _forcePulseCount;

            for (int i = 0; i < _forcePulseCount; i++)
            {
                if (rb == null) yield break;

                rb.AddForce(direction * forcePerPulse, ForceMode.Impulse);
                yield return new WaitForSeconds(interval);
            }
        }
        #endregion

        private void OnDrawGizmos()
        {
            // Par exemple, pour visualiser un OverlapCapsule centré sur un objet
            // avec une direction selon l'axe Y local
            OverlapCapsuleGizmo.DrawCapsuleGizmo(
                center: _playerTransform.position + (_playerTransform.forward * _pushLength/2) ,
                direction: _playerTransform.forward,
                radius: _pushRadius,
                height: _pushLength,
                color: new Color(0f, 1f, 0.5f, 0.25f)
            );
        }

        public class OverlapCapsuleGizmo : MonoBehaviour
        {
            [Header("Capsule Settings")]
            public float radius = 0.5f;
            public float height = 2.0f;
            public Vector3 direction = Vector3.up;
            public Color gizmoColor = new Color(0f, 1f, 0.5f, 0.3f);

            private void OnDrawGizmos()
            {
                DrawCapsuleGizmo(transform.position, direction, radius, height, gizmoColor);
            }

            public static void DrawCapsuleGizmo(
                Vector3 center,
                Vector3 direction,
                float radius,
                float height,
                Color color)
            {
                // Calcul des centres des deux demi-sphères
                // L'offset est la distance depuis le centre jusqu'à chaque sphere-center
                float offset = Mathf.Max(0f, (height / 2f) - radius);
                Vector3 dir = direction.normalized;

                Vector3 point0 = center - dir * offset; // bas
                Vector3 point1 = center + dir * offset; // haut

                // Rotation pour orienter les cercles perpendiculairement à la capsule
                Quaternion rot = Quaternion.FromToRotation(Vector3.up, dir);

                Color prevColor = Gizmos.color;

                // --- Remplissage semi-transparent ---
                Gizmos.color = color;
                // On dessine les sphères aux deux extrémités
                Gizmos.DrawSphere(point0, radius);
                Gizmos.DrawSphere(point1, radius);

                // --- Contour wire ---
                Gizmos.color = new Color(color.r, color.g, color.b, 1f);

                // Sphères fil de fer
                Gizmos.DrawWireSphere(point0, radius);
                Gizmos.DrawWireSphere(point1, radius);

                // 4 lignes reliant les sphères (les "côtés" de la capsule)
                DrawCapsuleSideLines(point0, point1, radius, rot);

                // Points centraux pour debug
                Gizmos.DrawSphere(point0, 0.02f);
                Gizmos.DrawSphere(point1, 0.02f);

                Gizmos.color = prevColor;
            }

            private static void DrawCapsuleSideLines(
                Vector3 p0, Vector3 p1,
                float radius, Quaternion rot)
            {
                // 4 directions perpendiculaires à l'axe
                Vector3[] perps = new Vector3[]
                {
            rot * Vector3.right,
            rot * Vector3.left,
            rot * Vector3.forward,
            rot * Vector3.back
                };

                foreach (var perp in perps)
                {
                    Gizmos.DrawLine(
                        p0 + perp * radius,
                        p1 + perp * radius
                    );
                }
            }
        }
    }
}
