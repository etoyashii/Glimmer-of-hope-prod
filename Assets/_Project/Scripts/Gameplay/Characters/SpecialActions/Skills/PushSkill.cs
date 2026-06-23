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
                _pushVFX.transform.position = _playerTransform.position;
                _pushVFX.transform.rotation = Quaternion.LookRotation(forward);
                _pushVFX.Play();
            }

            // Filter the objects raycasted to get the pushables
            foreach (Collider col in hits)
            {
                if (!col.CompareTag(PUSHABLE_TAG)) continue;

                if (col.transform.gameObject.TryGetComponent<RotatedByPushPull>(out RotatedByPushPull rotatedByPushPull))
                {
                    rotatedByPushPull.Rotate();
                    continue;
                }

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
    }
}
