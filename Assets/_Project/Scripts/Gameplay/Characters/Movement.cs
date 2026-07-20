using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay.Character.SpecialActions
{
    /// <summary>
    /// For the player movement. Based on Rigidbody physics.
    /// Supports Mobile (virtual gamepad), Keyboard/Mouse and Gamepad schemes
    /// via InputManager. Movement input is read from the Move InputActionReference
    /// which already has bindings for WASD/ZQSD, Left Stick (Gamepad) and the
    /// virtual Gamepad fed by DynamicJoystick.
    /// </summary>

    #region Dependencies

    [RequireComponent(typeof(Rigidbody))]

    #endregion
    public class Movement : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Movement")]
        [Range(1.0f, 100.0f)]
        [SerializeField] private float _speed = 20.0f;

        [Range(0f, 1f)]
        [Tooltip("How quickly the player reaches full speed (1 = instant, 0 = never).")]
        [SerializeField] private float _acceleration = 0.15f;

        [Range(0f, 50f)]
        [Tooltip("Extra gravity applied only to the player.")]
        [SerializeField] private float _extraGravity = 20f;

        [Header("References")]
        [SerializeField] private InputActionReference _moveAction;
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private Climbing _climbing;

        [Header("Ground Check")]
        [SerializeField] private float _groundCheckDistance = 0.2f;
        [SerializeField] private LayerMask _groundLayer;

        #endregion

        #region Public Properties

        public Vector3 MoveDirection => _targetMoveDirection;
        public float GroundCheckDistance => _groundCheckDistance;

        public bool IsSwimming
        {
            get => _isSwimming;

            set => _isSwimming = value;
        }

        public RaycastHit lastHit;
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
        private Animator _animator;
        private bool _isSwimming = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_rb == null)
                _rb = GetComponent<Rigidbody>();

            _rb.freezeRotation = true;
            _animator = GetComponent<Animator>();

            if (_animator == null)
                Debug.LogWarning("[Movement] No Animator found on this GameObject.");
        }

        private void OnEnable()
        {
            _moveAction.action.Enable();
            _moveAction.action.performed += OnMovementStarted;
            _moveAction.action.canceled += OnMovementCanceled;

            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnSchemeChanged.AddListener(OnSchemeChanged);
                ApplyBindingMask(InputManager.Instance.CurrentScheme);
            }
        }

        private void OnDisable()
        {
            _moveAction.action.Disable();
            _moveAction.action.performed -= OnMovementStarted;
            _moveAction.action.canceled -= OnMovementCanceled;

            if (InputManager.Instance != null)
                InputManager.Instance.OnSchemeChanged.RemoveListener(OnSchemeChanged);
        }

        private void FixedUpdate()
        {
            if (!_movementEnabled) return;

            ApplyMovement();
            ApplyAirCurrent();
            ApplyRotation();

            if (_animator != null)
                _animator.SetBool("IsGrounded", IsGrounded());
        }

        #endregion

        #region Public Methods

        public bool IsGrounded()
        {
            if (_animator != null)
                _animator.SetBool("IsGrounded", true);

            return Physics.Raycast(transform.position, Vector3.down, out lastHit, _groundCheckDistance, _groundLayer);
        }

        public void SetMovementEnabled(bool enabled)
        {
            _movementEnabled = enabled;
            if (!enabled)
            {
                _targetMoveDirection = Vector3.zero;
                _input = Vector2.zero;
                // Stop horizontal movement but preserve vertical (gravity)
                _rb.linearVelocity = new Vector3(0f, 0f, 0f);
                if (_animator != null)
                    _animator.SetFloat("Speed", 0f);
            }
        }

        // Called by AirCurrent.cs
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

            if (_movementEnabled)
            {
                _targetMoveDirection = Vector3.ProjectOnPlane((cameraRight * _input.x + cameraForward * _input.y), lastHit.normal).normalized;
            }
            else
            {
                _targetMoveDirection = Vector3.zero;
                _input = Vector2.zero;
            }

            if (_climbing != null && _climbing.climbing)
            {
                Vector3 wallNormal = _climbing.frontWallHit.normal;
                Vector3 temp = new Vector3(_targetMoveDirection.x, _input.y, _targetMoveDirection.z);
                Vector3 moveAlongWall = Vector3.ProjectOnPlane(temp, wallNormal);
                _rb.linearVelocity = moveAlongWall * (_speed / 5f);
            }
            else if (!IsGrounded() && !IsSwimming)
            {
                //Normal air movement preserve vertical velocity
                //_rb.useGravity = true;

                Vector3 targetVelocity = _targetMoveDirection * _speed;
                targetVelocity.y = _rb.linearVelocity.y;
                //Debug.Log("In the air : " + _rb.linearVelocity.y);
                _rb.linearVelocity = Vector3.Lerp(_rb.linearVelocity, targetVelocity, _acceleration);

            }
            else
            {
                //Normal ground movement nulify gravity
                //_rb.useGravity = false;
                Vector3 targetVelocity = _targetMoveDirection * _speed;
                _rb.linearVelocity = targetVelocity;

            }

            if (_animator != null)
                _animator.SetFloat("Speed", new Vector3(_rb.linearVelocity.x, 0, _rb.linearVelocity.z).magnitude);

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

        private void OnSchemeChanged(InputManager.ControlScheme scheme)
        {
            // Cancel any ongoing input when switching schemes
            _input = Vector2.zero;
            OnPlayerStopMoving?.Invoke();

            ApplyBindingMask(scheme);
        }

        /// <summary>
        /// Restricts which bindings are active based on the current control scheme.
        /// Mobile  => only Gamepad (virtual gamepad fed by DynamicJoystick)
        /// KeyboardMouse => only Keyboard and Mouse
        /// Gamepad => only physical Gamepad (excludes the virtual one via group name)
        /// </summary>
        private void ApplyBindingMask(InputManager.ControlScheme scheme)
        {
            _moveAction.action.bindingMask = scheme switch
            {
                InputManager.ControlScheme.Mobile => InputBinding.MaskByGroup("Gamepad"),
                InputManager.ControlScheme.KeyboardMouse => InputBinding.MaskByGroup("Keyboard/Mouse"),
                InputManager.ControlScheme.Gamepad => InputBinding.MaskByGroup("Gamepad"),
                _ => null
            };
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