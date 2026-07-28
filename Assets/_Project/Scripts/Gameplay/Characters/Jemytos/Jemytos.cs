using Unity.Loading;
using UnityEngine;
using UnityEngine.AI;

namespace GlimmerOfHope.Gameplay
{
    public class Jemytos : MonoBehaviour
    {
        #region Serialize Field

        [SerializeField] private float _lifeTime = 20f;
        [SerializeField] private float _moveSpeed = 1f;
        [SerializeField] private float _edgeCheckDistance = 1.5f;
        [SerializeField] private float _minAngle = 20f;
        [SerializeField] private float _maxGapSpeed = 0.01f;
        [SerializeField] private float _lateralOffset = 1f;

        #endregion

        #region Private Prperties

        private float _cooldownCheckGap = 1f;

        private NavMeshAgent _navMeshAgent;

        #endregion

        #region Public Properties

        public Portal portal;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            _cooldownCheckGap -= Time.fixedDeltaTime;

            Vector3 offset = transform.forward * Time.deltaTime;

            Vector3 desiredPos = transform.position + offset;

            _navMeshAgent.Move(offset);

            float gap = Vector3.Distance(_navMeshAgent.nextPosition, desiredPos);

            if (gap > _maxGapSpeed && _cooldownCheckGap <= 0f)
            {
                Destroy(gameObject);
            }

            _lifeTime -= Time.deltaTime;

            if (_lifeTime <= 0f)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Finish"))
            {
                portal.FinishJemytos();
                Destroy(gameObject);
                return;
            }

            Vector3 incomingDir = transform.forward;
            Vector3 normal = collision.contacts[0].normal;
            Vector3 contactPoint = collision.contacts[0].point;

            Vector3 bounceDir = Vector3.Reflect(incomingDir, normal).normalized;
            bounceDir.y = 0f;

            float angle = Vector3.Angle(incomingDir, bounceDir);

            Debug.Log(angle);

            if (180 - angle < _minAngle)
            {
                float sign = Mathf.Sign(Vector3.Cross(incomingDir, normal).y);

                Quaternion forcedRotation = Quaternion.AngleAxis(_minAngle * sign, Vector3.up);
                Vector3 forcedDir = forcedRotation * bounceDir;
                forcedDir.y = 0f;
                forcedDir.Normalize();

                Vector3 lateralDir = Vector3.Cross(Vector3.up, normal).normalized * sign;

                float normalOffset = 0.1f;

                Vector3 newPosition = transform.position
                    + normal * normalOffset
                    - lateralDir * _lateralOffset;

                transform.position = newPosition;
                transform.rotation = Quaternion.LookRotation(forcedDir);
            }
            else
            {
                if (bounceDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(bounceDir);
                }
            }

        }

        #endregion
    }
}
