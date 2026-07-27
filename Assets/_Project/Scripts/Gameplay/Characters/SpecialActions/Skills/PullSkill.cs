using System.Collections;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Skill that pulls "windpushable" tagged objects (need rigidbody) towards the caster 
    /// </summary>
    public class PullSkill : Skills
    {
        #region Serialized Fields
        [Header("Refs")]
        [Tooltip("Transform of the caster")]
        [SerializeField] private Transform _playerTransform;

        [Tooltip("VFX of the skill")]
        [SerializeField] private ParticleSystem _pullVFX;

        [Tooltip("Layers pris en compte par l'OverlapCapsule (tout par défaut)")]
        [SerializeField] private LayerMask _detectionMask = ~0;

        [Header("Shape of the pull")]
        [Tooltip("Length of the pull Cone in front of the caster")]
        [SerializeField] private float _pullLength = 8f;
        [Tooltip("Radius of the pull Cone at its farthest from the caster")]
        [SerializeField] private float _pullRadius = 3f;

        [Header("Strenght of the pull")]
        [Tooltip("Total strenght applied to the pullable objects")]
        [SerializeField] private float _pullForce = 18f;

        [Tooltip("downward Strenght")]
        [SerializeField] private float _downwardForce = 2f;

        [Tooltip("Does the object stop once the caster reached")]
        [SerializeField] private bool _dampOnArrival = true;

        [Tooltip("Distance from wich the objects coming towards the caster are slowed down")]
        [SerializeField] private float _arrivalRadius = 1.5f;

        [Tooltip("Falloff : close objects receive less force (0= full force, 1= force nullified)")]
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

        public override void PerformSkill()
        {
            Pull();
        }

        #region Constants
        private const string PUSHABLE_TAG = "WindPushable";
        #endregion

        #region Private Methods
        private void Pull()
        {
            Debug.Log("Executing Wind Pull!");
            Vector3 forward = new Vector3(
            _playerTransform.forward.x,
            0f,
            _playerTransform.forward.z
            ).normalized;

            Vector3 capsuleEnd = _playerTransform.position + forward * _pullLength;

            Collider[] hits = Physics.OverlapCapsule(
                _playerTransform.position,
                capsuleEnd,
                _pullRadius,
                _detectionMask
            );

            if (_pullVFX != null)
            {
                // orient the vfx towards the caster 
/*                _pullVFX.transform.position = _playerTransform.position + forward * _pullLength;
                _pullVFX.transform.rotation = Quaternion.LookRotation(-forward);*/
                _pullVFX.Play();
            }

            foreach (Collider col in hits)
            {
                if (!col.CompareTag(PUSHABLE_TAG)) continue;

                // Let a scripted reaction take over instead of a raw physics push
                WindReactive reactive = col.GetComponent<WindReactive>();
                if (reactive != null && reactive.ReactsToPull)
                {
                    reactive.NotifyPush();
                    continue;
                }

                Rigidbody rb = col.attachedRigidbody;
                if (rb == null || rb.isKinematic) continue;

                // Check if the object is in fact in front of the caster
                Vector3 toObject = (col.transform.position - _playerTransform.position).normalized;
                if (Vector3.Dot(forward, toObject) < 0f) continue;

                float distance = Vector3.Distance(_playerTransform.position, col.transform.position);

                // inversed falloff
                float falloff = Mathf.Lerp(1f - _distanceFalloff, 1f, distance / _pullLength);
                float finalForce = _pullForce * falloff;

                // pull = towards the caster + down
                Vector3 toPlayer = (_playerTransform.position - col.transform.position).normalized;
                Vector3 pullDir = (toPlayer - Vector3.up * (_downwardForce / _pullForce)).normalized;

                if (_useForceWave)
                    StartCoroutine(ApplyPullWave(rb, pullDir, finalForce, distance));
                else
                    rb.AddForce(pullDir * finalForce, ForceMode.Impulse);
            }
        }
        private IEnumerator ApplyPullWave(Rigidbody rb, Vector3 direction, float totalForce, float startDistance)
        {
            float forcePerPulse = totalForce / _forcePulseCount;
            float interval = _forceWaveDuration / _forcePulseCount;

            for (int i = 0; i < _forcePulseCount; i++)
            {
                if (rb == null) yield break;

                // Recalculate the direction towards the player each impulse
                Vector3 toPlayer = (_playerTransform.position - rb.position).normalized;
                Vector3 currentDir = (toPlayer - Vector3.up * (_downwardForce / _pullForce)).normalized;

                rb.AddForce(currentDir * forcePerPulse, ForceMode.Impulse);
                yield return new WaitForSeconds(interval);
            }

            // slow on arrival
            if (_dampOnArrival && rb != null)
                yield return StartCoroutine(DampOnArrival(rb));
        }

        private IEnumerator DampOnArrival(Rigidbody rb)
        {
            float timeout = 3f;
            float elapsed = 0f;

            while (rb != null && elapsed < timeout)
            {
                elapsed += Time.deltaTime;

                float dist = Vector3.Distance(rb.position, _playerTransform.position);
                if (dist <= _arrivalRadius)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    yield break;
                }

                yield return null;
            }
        }
        #endregion
    }
}
