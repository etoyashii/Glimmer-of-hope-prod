using GlimmerOfHope.Gameplay.Character.SpecialActions;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Handles surface swimming. The player stays locked at water level
    /// and moves using the same inputs as Movement.cs.
    /// Must be unlocked explicitly via SetSwimmingUnlocked(true).
    /// </summary>
    public class Swimming : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Refs")]
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private Movement _playerMovement;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _playerTransform;

        [Header("Swimming Settings")]
        [Tooltip("Speed of the player while swimming.")]
        [SerializeField] private float _swimSpeed = 5f;

        [Tooltip("How strongly the player is pulled to the water surface.")]
        [SerializeField] private float _surfaceLockStrength = 10f;

        [Tooltip("Tolerance around the water surface Y before correction kicks in (in meters).")]
        [SerializeField] private float _surfaceTolerance = 0.05f;

        #endregion

        #region Private Fields

        [SerializeField] public bool _isSwimmingUnlocked = true;
        private bool _isSwimming = false;
        public float _waterSurfaceY = 0f;

        private static readonly int _animIsSwimming = Animator.StringToHash("IsSwimming");
        private static readonly int _animSwimSpeed = Animator.StringToHash("SwimSpeed");

        #endregion

        #region Public Properties

        public bool IsSwimming => _isSwimming;

        #endregion

        #region Unity Lifecycle

        private void FixedUpdate()
        {
            if (!_isSwimming) return;

            ApplySwimMovement();
            LockToSurface();
        }

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("Entered");
            if (!other.CompareTag("Water")) return;
            if (!_isSwimmingUnlocked) return;

            // Snap water surface Y from the top of the water collider
            _waterSurfaceY = other.bounds.max.y;

            EnterWater();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Water")) return;

            ExitWater();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Unlock or lock the ability to swim.
        /// When locked, the player won't enter swim mode even if touching water.
        /// </summary>
        public void SetSwimmingUnlocked(bool unlocked)
        {
            _isSwimmingUnlocked = unlocked;

            // If swimming is re-locked mid-water, force exit
            if (!unlocked && _isSwimming)
                ExitWater();
        }

        #endregion

        #region Private Methods

        public void EnterWater()
        {
            _isSwimming = true;

            // Hand off movement control to this script
            _playerMovement.SetMovementEnabled(true);

            // Kill any vertical momentum (no splashing through the surface)
            _rb.linearVelocity = new Vector3(0f, 0f, 0f);
            _rb.useGravity = false;

            UpdateAnimator();
        }

        public void ExitWater()
        {
            _isSwimming = false;

            _rb.useGravity = true;
            _rb.linearVelocity = new Vector3(0f, 0f, 0f);
            _playerMovement.SetMovementEnabled(true);

            UpdateAnimator();
        }

        private void ApplySwimMovement()
        {
            // Reuse the move direction already computed by Movement.cs
            Vector3 swimDirection = new Vector3(
                _playerMovement.MoveDirection.x,
                0f,
                _playerMovement.MoveDirection.z
            ).normalized;

            Vector3 targetVelocity = swimDirection * _swimSpeed;
            targetVelocity.y = _rb.linearVelocity.y; // let LockToSurface handle Y

            _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, targetVelocity, 0.2f);

            // Rotate toward swim direction
            if (swimDirection.magnitude > 0.1f)
            {
                Quaternion toRotate = Quaternion.LookRotation(swimDirection);
                _playerTransform.transform.rotation = Quaternion.Lerp(_playerTransform.transform.rotation, toRotate, 10f * Time.fixedDeltaTime);
            }

            // Drive animator
            if (_animator != null)
                _animator.SetFloat(_animSwimSpeed, targetVelocity.magnitude);
        }

        private void LockToSurface()
        {
            float currentY = _playerTransform.transform.position.y;
            float diff = _waterSurfaceY - currentY;

            if (Mathf.Abs(diff) > _surfaceTolerance)
            {
                // Apply a corrective upward/downward force to stay at surface
                _rb.AddForce(Vector3.up * diff * _surfaceLockStrength, ForceMode.Acceleration);
            }
            else
            {
                // Close enough -> kill vertical velocity to avoid weird oscillations
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            }
        }

        private void UpdateAnimator()
        {
            if (_animator == null) return;

            _animator.SetBool(_animIsSwimming, _isSwimming);

            if (!_isSwimming)
                _animator.SetFloat(_animSwimSpeed, 0f);
        }

        #endregion
    }
}