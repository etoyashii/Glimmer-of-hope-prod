using GlimmerOfHope.Gameplay.Character.SpecialActions;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{

    /// <summary>
    /// Script to manage the slide with a rigidbody
    /// </summary>
    public class Slide : Skills
    {
        #region SerializeField

        [Header("References")]
        [SerializeField] private Movement _movement;
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private PhysicsMaterial _pm;
        [SerializeField] private CapsuleCollider _capsuleColliderPlayer;

        [Header("Commun parametre")]
        [SerializeField] private float _slideSpeed = 1f;
        [SerializeField] private float _stopSlidingTimer = 0.5f;
        [SerializeField] private float _slideAcceleration = 10f;
        [SerializeField] private float _speedDesactivation = 0.01f;
        [SerializeField] private float _slideControlStrength = 0.2f;
        [SerializeField] private AnimationCurve _slideSpeedCurve;
        [SerializeField] private float _maxDecelForce = 15f;
        [SerializeField] private float _slideBackwardDecel = 0.85f;
        [SerializeField] private float _slideForwardBoost = 1f;

        [Header("World activation")]
        [SerializeField] private float _maxDegree = 30f;

        [Header("Skills")]
        [SerializeField] private float _impulseStartSlide = 5f;       

        #endregion

        #region Private Properties

        private float _stopTimer;
        private bool _activateByWorld = false;
        private bool _isSliding = false;
        private Vector3 _lastDirection = Vector3.zero;

        #endregion

        #region Unity lifecycle

        // Update is called once per frame
        void Update()
        {
            if (_movement == null) return;

            if (Vector3.Angle(Vector3.up, _movement.lastHit.normal) > _maxDegree) //if the current angle with the floor is to hight
            {
                if (_isSliding) return;

                _activateByWorld = true;
                PerformSkill();
            }
            else if (_isSliding)
            {
                _stopTimer -= Time.deltaTime;

                if (_stopTimer <= 0f && _activateByWorld)
                {
                    StopSliding();
                }
                else if (!_activateByWorld)
                {
                    if (_rb.linearVelocity.magnitude < _speedDesactivation)
                    {
                        StopSliding();
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            if (!_isSliding) return;

            if (_activateByWorld)
            {
                _lastDirection = Vector3.ProjectOnPlane(Vector3.down, _movement.lastHit.normal).normalized; //get the move direction from the normal
                Vector3 targetVelocity = _lastDirection * _slideSpeed;

                Vector3 velocityDiff = targetVelocity - _rb.linearVelocity;
                Vector3 accel = Vector3.ClampMagnitude(velocityDiff / Time.fixedDeltaTime, _slideAcceleration);//use to have a an acceleration

                _rb.AddForce(accel, ForceMode.Acceleration);
            }
            else
            {
                Vector3 slideDir = _rb.transform.forward;
                slideDir.y = 0f;
                slideDir.Normalize();

                Vector3 currentHorizontalVel = new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z);
                float currentSpeed = currentHorizontalVel.magnitude;

                Vector3 targetDir = _movement._targetMoveDirection;
                targetDir.y = 0f;

                Vector3 blendedDirection = (slideDir + targetDir * _slideControlStrength).normalized; //blend the slide move with the player input

                float inputY = _movement.GetInput().y;
                float newSpeed = 0f;

                if (inputY > 0)
                {
                    newSpeed = currentSpeed + _slideForwardBoost * Time.fixedDeltaTime;
                }
                else if (inputY < 0)
                {
                    newSpeed = Mathf.Max(0, currentSpeed - _slideBackwardDecel * Time.fixedDeltaTime);
                }
                else //== 0
                {
                    float speedRatio = Mathf.Clamp01(currentSpeed / _slideSpeed);
                    float decelAmount = _slideSpeedCurve.Evaluate(speedRatio) * _maxDecelForce;
                    newSpeed = Mathf.Max(0, currentSpeed - decelAmount * Time.fixedDeltaTime);
                }

                Vector3 targetVelocity = blendedDirection * newSpeed;
                Vector3 velocityChange = targetVelocity - currentHorizontalVel;
                _rb.AddForce(velocityChange, ForceMode.VelocityChange);
            }
            
        }

        #endregion

        #region Public Methods

        public override void PerformSkill()
        {
            _isSliding = true;

            _stopTimer = _stopSlidingTimer;

            _capsuleColliderPlayer.material = _pm;

            if (!_activateByWorld)
            {
                //start impulse
                Vector3 impulse = _rb.transform.forward * _impulseStartSlide;
                _rb.AddForce(impulse , ForceMode.Impulse);
            }
        }

        public void StopSliding()
        {
            _isSliding = false;
            _activateByWorld = false;

            _capsuleColliderPlayer.material = null;
        }

        public bool IsSliding()
        {
            return _isSliding;
        }

        public Vector3 GetLastDirection()
        {
            return _lastDirection;
        }

        #endregion
    }
}
