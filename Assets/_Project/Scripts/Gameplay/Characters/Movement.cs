using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay.Character.SpecialActions
{
    /// <summary>
    /// For the player movement. Based on Rigidbody physics.
    /// </summary>

    #region Dependancies

    [RequireComponent(typeof(Rigidbody))]

    #endregion
    public class Movement : MonoBehaviour
    {
        #region SerializeField

        [Header("Mouvement")]
        [Range(1.0f, 100.0f)]
        [SerializeField] private float _speed = 20.0f;

        [Range(0f, 1f)]
        [Tooltip("How quickly the player reaches full speed (1 = instant, 0 = never).")]
        [SerializeField] private float _acceleration = 0.15f;

        [Range(0f, 50f)]
        [Tooltip("Extra gravity to apply only to the player")]
        [SerializeField] private float _extraGravity = 20f;

        [Header("References")]
        [SerializeField] private InputActionReference _moveAction;
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private Climbing _climbing;

        #endregion

        #region Public Properties

        public Vector3 MoveDirection => _targetMoveDirection;

        #endregion

        #region Event Actions

        public event Action OnPlayerStartMoving;
        public event Action OnPlayerStopMoving;

        #endregion

        #region Private Fields

        private Vector2 _input;
        private Vector3 _targetMoveDirection;
        private bool _movementEnabled = true;

        private Vector3 _airCurrentForce = Vector3.zero;
        private bool _inAirCurrent = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_rb == null)
                _rb = GetComponent<Rigidbody>();
            
            _rb.freezeRotation = true;
        }

        private void OnEnable()
        {
            _moveAction.action.Enable();
            _moveAction.action.performed += OnMovementStarted;
            _moveAction.action.canceled += OnMovementCanceled;
        }

        private void OnDisable()
        {
            _moveAction.action.Disable();
            _moveAction.action.performed -= OnMovementStarted;
            _moveAction.action.canceled -= OnMovementCanceled;
        }

        private void FixedUpdate()
        {
            if (!_movementEnabled) return;

            ApplyMovement();
            ApplyAirCurrent();
            ApplyRotation();
        }

        #endregion

        #region Public Methods

        public void SetMovementEnabled(bool enabled)
        {
            _movementEnabled = enabled;

            if (!enabled)
            {
                _input = Vector2.zero;
                // Stop horizontal movement but preserve vertical (gravity)
                _rb.linearVelocity = new Vector3(0f, _rb.linearVelocity.y, 0f);
            }
        }

        // Method called in AirCurrent.cs
        public void SetAirCurrent(bool active, Vector3 airCurrentForce = default)
        {
            _inAirCurrent = active;
            _airCurrentForce = active ? airCurrentForce : Vector3.zero;
        }

        #endregion

        #region Private Methods

        private void ApplyMovement()
        {
            Vector3 cameraForward = _playerCamera.transform.forward;
            Vector3 cameraRight = _playerCamera.transform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            _targetMoveDirection = (cameraRight * _input.x + cameraForward * _input.y).normalized;

            if (_climbing != null && _climbing.climbing)
            {
                // Project movement along the wall surface
                Vector3 wallNormal = _climbing.frontWallHit.normal;
                Vector3 temp = new Vector3(_targetMoveDirection.x, _input.y, _targetMoveDirection.z);
                Vector3 moveAlongWall = Vector3.ProjectOnPlane(temp, wallNormal);

                Vector3 climbVelocity = moveAlongWall * (_speed / 5f);
                _rb.linearVelocity = climbVelocity;
            }
            else
            {
                // Normal ground/air movement — preserve vertical velocity
                Vector3 targetVelocity = _targetMoveDirection * _speed;
                targetVelocity.y = _rb.linearVelocity.y;
                _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, targetVelocity, _acceleration);
                _rb.AddForce(Vector3.down * _extraGravity, ForceMode.Acceleration);
            }
        }

        private void ApplyAirCurrent()
        {
            if (!_inAirCurrent) return;

            _rb.AddForce(_airCurrentForce, ForceMode.Impulse);
        }

        private void ApplyRotation()
        {
            if (_climbing != null && _climbing.climbing) return;
            if (_targetMoveDirection.magnitude < 0.1f) return;

            Quaternion toRotate = Quaternion.LookRotation(_targetMoveDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, toRotate, 10f * Time.fixedDeltaTime);
        }

        private void OnMovementStarted(InputAction.CallbackContext context)
        {
            _input = context.ReadValue<Vector2>();
            OnPlayerStartMoving?.Invoke();
        }

        private void OnMovementCanceled(InputAction.CallbackContext context)
        {
            _input = Vector2.zero;
            OnPlayerStopMoving?.Invoke();
        }

        #endregion
    }
}