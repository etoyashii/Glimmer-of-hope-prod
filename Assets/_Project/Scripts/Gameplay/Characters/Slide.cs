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
    public class Slide : Skills
    {
        #region SerializeField

        [SerializeField] private float _maxDegree = 30f;
        [SerializeField] private float _slideSpeed = 1f;
        [SerializeField] private float _stopSlidingTimer = 0.5f;
        [SerializeField] private float _slideAcceleration = 10f;
        [SerializeField] private float _speedDesactivation = 0.01f;
        [SerializeField] private float _impulseStartSlide = 5f;
        [SerializeField] private float _slideFriction = 0f;
        [SerializeField] private AnimationCurve _slideSpeedCurve;
        [SerializeField] private float _maxDecelForce = 15f;
        [SerializeField] private Movement _movement;
        [SerializeField] private Rigidbody _rb;

        #endregion

        #region Public Properties

        public bool isSliding = false;
        public Vector3 lastDirection = Vector3.zero;

        #endregion

        #region Private Properties

        [SerializeField]  private float _stopTimer;
        private bool _activateByWorld = false;

        #endregion

        #region Unity lifecycle

        // Update is called once per frame
        void Update()
        {
            if (_movement == null) return;

            if (Vector3.Angle(Vector3.up, _movement.lastHit.normal) > _maxDegree)
            {
                if (isSliding) return;

                _activateByWorld = true;
                PerformSkill();
            }
            else if (isSliding)
            {
                _stopTimer -= Time.deltaTime;

                if (_stopTimer <= 0f && _activateByWorld)
                {
                    StopSliding();
                }
                else if (!_activateByWorld)
                {
                    //stop if jump press or if speed is low?
                    if (_rb.linearVelocity.magnitude < _speedDesactivation)
                    {
                        Debug.Log("Magintude velocity : " + _rb.linearVelocity.magnitude);
                        StopSliding();
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            if (!isSliding) return;

            if (_activateByWorld)
            {
                lastDirection = Vector3.ProjectOnPlane(Vector3.down, _movement.lastHit.normal).normalized;
                Vector3 targetVelocity = lastDirection * _slideSpeed;

                Vector3 velocityDiff = targetVelocity - _rb.linearVelocity;
                Vector3 accel = Vector3.ClampMagnitude(velocityDiff / Time.fixedDeltaTime, _slideAcceleration);

                _rb.AddForce(accel, ForceMode.Acceleration);
            }
            else
            {
                Vector3 currentHorizontalVel = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
                float currentSpeed = currentHorizontalVel.magnitude;

                float speedRatio = Mathf.Clamp01(currentSpeed / _slideSpeed);

                float decelAmount = _slideSpeedCurve.Evaluate(speedRatio) * _maxDecelForce;

                Vector3 decelForce = -currentHorizontalVel.normalized * decelAmount;
                _rb.AddForce(decelForce, ForceMode.Acceleration);
            }
            
        }

        private void StopSliding()
        {
            Debug.Log("Stop sliding");
            isSliding = false;
            _activateByWorld = false;
            _movement.SetLockCameraY(false);
        }

        #endregion

        public override void PerformSkill()
        {
            Debug.Log("Perfom slide skill");

            isSliding = true;
            _movement.SetLockCameraY(true);

            _stopTimer = _stopSlidingTimer;

            if (!_activateByWorld)
            {
                //start impulse
                Vector3 impulse = _rb.transform.forward * _impulseStartSlide;
                Debug.Log("Impulse : " + impulse);
                _rb.AddForce(impulse , ForceMode.Impulse);
            }
        }
    }
}
