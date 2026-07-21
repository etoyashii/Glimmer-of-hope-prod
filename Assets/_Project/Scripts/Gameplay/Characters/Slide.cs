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
        [SerializeField] private Rigidbody _rb;

        #endregion

        #region Public Properties

        public bool isSliding = false;
        public Vector3 lastDirection = Vector3.zero;

        #endregion

        #region Private Properties

        [SerializeField]  private float _stopTimer;

        #endregion

        #region Unity lifecycle

        // Update is called once per frame
        void Update()
        {
            if (_movement == null) return;

            if (Vector3.Angle(Vector3.up, _movement.lastHit.normal) > _maxDegree)
            {
                if (isSliding) return;

                isSliding = true;
                _movement.SetLockCameraY(true);

                _stopTimer = _stopSlidingTimer;
            }
            else
            {
                _stopTimer -= Time.deltaTime;

                if (_stopTimer <= 0f && isSliding)
                {
                    isSliding = false;
                    _movement.SetLockCameraY(false);
                }
            }
        }

        private void FixedUpdate()
        {
            if (!isSliding) return;

            lastDirection = Vector3.ProjectOnPlane(Vector3.down, _movement.lastHit.normal).normalized;
            Vector3 targetVelocity = lastDirection * _slideSpeed;

            Vector3 velocityDiff = targetVelocity - _rb.linearVelocity;
            Vector3 accel = Vector3.ClampMagnitude(velocityDiff / Time.fixedDeltaTime, _slideAcceleration);

            _rb.AddForce(accel, ForceMode.Acceleration);
        }

        #endregion
    }
}
