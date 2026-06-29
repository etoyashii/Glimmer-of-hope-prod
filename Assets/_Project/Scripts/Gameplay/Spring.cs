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
        [SerializeField] private Transform _anchor;

        [Header("Variable")]
        [Min(0f)]
        [Tooltip("Under this value the spring will do nothing.")]
        [SerializeField] private float _minDistance;

        [Tooltip("Max distance possible complete stop at this distance.")]
        [SerializeField] private float _maxDistance;

        [SerializeField] private float _springStrength = 1f;

        #endregion

        #region Public Properties

        public bool isActive = false;
        public float currentPullForce = 0f;

        #endregion

        #region Unity Lifecycle

        private void FixedUpdate()
        {
            if (!isActive) return;

            Vector3 toAnchor = _anchor.position - transform.position;
            float distance = toAnchor.magnitude;
            Vector3 dir = toAnchor.normalized;

            if (distance >= _minDistance && distance <= _maxDistance) //if between the min and the max => spring active
            {
                currentPullForce = _springStrength * (distance - _minDistance);
                transform.position += dir * currentPullForce * Time.fixedDeltaTime;
            }

            if (distance > _maxDistance)
            {
                //bloquage de la position au max
                transform.position = _anchor.position - dir * _maxDistance;
            }
        }

        #endregion

        #region Public Methods
        public void SetAnchor(Transform anchor)
        {
            _anchor = anchor;
        }

        public void SetMinDistance(float minDistance)
        {
            _minDistance = minDistance;
        }

        public void SetMaxDistance(float maxDistance)
        {
            _maxDistance = maxDistance;
        }

        #endregion        
    }
}
