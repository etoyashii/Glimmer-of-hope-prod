using Unity.Loading;
using UnityEngine;
using UnityEngine.AI;

namespace GlimmerOfHope.Gameplay
{
    public class Jemytos : MonoBehaviour
    {
        [SerializeField] private float _lifeTime = 20f;
        [SerializeField] private float _moveSpeed = 1f;
        [SerializeField] private float _edgeCheckDistance = 1.5f;
        [SerializeField] private float _minAngle = 20f;
        [SerializeField] private float _maxGapSpeed = 0.01f;
        [SerializeField] private float _lateralOffset = 1f;

        private float _cooldownCheckGap = 1f;

        private NavMeshAgent _navMeshAgent;

        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            _cooldownCheckGap -= Time.fixedDeltaTime;

            Vector3 offset = transform.forward * Time.deltaTime;

            //check if arrive to the limit of the navmesh
            Vector3 desiredPos = transform.position + offset;

            _navMeshAgent.Move(offset);

            // nextPosition = position réelle après clamp sur le NavMesh
            float gap = Vector3.Distance(_navMeshAgent.nextPosition, desiredPos);

            if (gap > _maxGapSpeed && _cooldownCheckGap <= 0f) // tolérance à ajuster
            {
                Debug.Log("Bloqué par le bord du NavMesh : " + gap + "   max : " + _maxGapSpeed);
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

                // Direction latérale : perpendiculaire à la normale dans le plan horizontal,
                // orientée par le même "sign" que la rotation
                Vector3 lateralDir = Vector3.Cross(Vector3.up, normal).normalized * sign;

                float normalOffset = 0.1f;   // éloignement du mur

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
    }
}
