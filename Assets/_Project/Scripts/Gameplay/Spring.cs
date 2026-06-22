using System.ComponentModel;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]

    /// <summary>
    /// Use to apply a spring effect on a gameObject
    /// </summary>
    public class Spring : MonoBehaviour
    {
        #region SerializeField

        [Header("Reference")]
        [Tooltip("The static point of the spring.")]
        [SerializeField] private Transform anchor;

        [Header("Variable")]
        [Min(0f)]
        [Tooltip("Under this value the spring will do nothing.")]
        [SerializeField] private float minDistance;

        [Tooltip("Max distance possible complete stop at this distance.")]
        [SerializeField] private float maxDistance;

        [SerializeField] private float springStrength = 1f;

        #endregion

        #region Public Properties

        public bool isActive = false;
        public float currentPullForce = 0f;

        #endregion

        #region Unity Lifecycle

        private void FixedUpdate()
        {
            if (!isActive) return;

            Vector3 toAnchor = anchor.position - transform.position;
            float distance = toAnchor.magnitude;
            Vector3 dir = toAnchor.normalized;

            if (distance >= minDistance && distance <= maxDistance) //if between the min and the max => spring active
            {
                currentPullForce = springStrength * (distance - minDistance);
                transform.position += dir * currentPullForce * Time.fixedDeltaTime;
            }

            if (distance > maxDistance)
            {
                //bloquage de la position au max
                transform.position = anchor.position - dir * maxDistance;
            }
        }

        #endregion

        #region Public Methods
        public void SetAnchor(Transform _anchor)
        {
            anchor = _anchor;
        }

        public void SetMinDistance(float _minDistance)
        {
            minDistance = _minDistance;
        }

        public void SetMaxDistance(float _maxDistance)
        {
            maxDistance = _maxDistance;
        }

        #endregion        
    }
}
