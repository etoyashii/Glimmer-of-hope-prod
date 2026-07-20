using GlimmerOfHope.Gameplay.Character.SpecialActions;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    #region Dependencies
    [RequireComponent(typeof(Rigidbody))]
    #endregion

    /// <summary>
    /// Script to manage the slide with a rigidbody
    /// </summary>
    public class Slide : MonoBehaviour
    {
        #region SerializeField

        [SerializeField] private float _maxDegree = 30f;
        [SerializeField] private float _slideSpeed = 1f;
        [SerializeField] private float _stopSlidingTimer = 0.5f;
        [SerializeField] private float _slideAcceleration = 10f;
        [SerializeField] private Movement _movement;

        #endregion

        #region Public Properties

        public bool isSliding = false;
        public Vector3 lastDirection = Vector3.zero;

        #endregion

        #region Private Properties

        private Rigidbody _rb;
        private float _stopTimer;
        private float _currentSlideSpeed = 0f;

        #endregion

        #region Unity lifecycle

        private void Start()
        {
            _rb = GetComponent<Rigidbody>();
        }

        // Update is called once per frame
        void Update()
        {
            if (_movement == null) return;

            if (Vector3.Angle(Vector3.up, _movement.lastHit.normal) > _maxDegree)
            {
                if (isSliding) return;

                isSliding = true;
                _currentSlideSpeed = 0f;

                _stopTimer = _stopSlidingTimer;
            }
            else
            {
                _stopTimer -= Time.deltaTime;

                if (_stopTimer <= 0f && isSliding)
                {
                    isSliding = false;
                }
            }
        }

        private void FixedUpdate()
        {
            if (!isSliding) return;

            lastDirection = Vector3.ProjectOnPlane(Vector3.down, _movement.lastHit.normal).normalized;

            _currentSlideSpeed = Mathf.MoveTowards(_currentSlideSpeed, _slideSpeed, _slideAcceleration * Time.fixedDeltaTime);

            _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, lastDirection * _currentSlideSpeed, _slideAcceleration * Time.fixedDeltaTime);
        }

        #endregion
    }
}
