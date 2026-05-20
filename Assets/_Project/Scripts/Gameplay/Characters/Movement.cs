using UnityEngine;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay.Character.SpecialActions
{
    /// <summary>
    /// For the player movement. Based on gravity, velocity and deltaTime.
    /// </summary>

    #region Dependancies

    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(SkillManager))]

    #endregion
    public class Movement : MonoBehaviour
    {
        #region SerializeField

        [Header("Mouvement")]
        [Range(1.0f, 100.0f)]
        [SerializeField] private float _speed = 20.0f;
        [Range(-30.0f, 30.0f)]
        [SerializeField] private float _gravity = -9.81f;
        [Header("Rotation")]
        [Range(0.01f, 1.0f)]
        [SerializeField] private float _rotationSensitivity = 0.2f;

        [Header("References")]
        [SerializeField] private InputActionReference _moveAction;
        [SerializeField] private InputActionReference _swipeAction;
        [SerializeField] private CharacterController _controller;


        #endregion

        #region Public Properties

        public Vector3 MoveDirection => new(_direction.x, 0f, _direction.y);

        #endregion

        #region Private Fields

        private Vector2 _direction;
        private Vector2 _swipeDelta;
        private float _verticalVelocity;
        private bool _movementEnabled = true;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            _moveAction.action.Enable();
            _moveAction.action.performed += OnMovementStarted;
            _moveAction.action.canceled += OnMovementCanceled;

            _swipeAction.action.Enable();
            _swipeAction.action.performed += OnSwipePerformed;
            _swipeAction.action.canceled += OnSwipeCanceled;
        }

        private void Update()
        {
            if (!_movementEnabled) return;

            if (_controller.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;

            _verticalVelocity += _gravity * Time.deltaTime;

            Vector3 move = transform.right * _direction.x + transform.forward * _direction.y;
            move.y = _verticalVelocity;

            _controller.Move(_speed * Time.deltaTime * move);

            if (_swipeDelta.sqrMagnitude < 0.01f) return;

            float yRotation = _swipeDelta.x * _rotationSensitivity;
            transform.Rotate(Vector3.up, yRotation, Space.World);
        }

        private void OnDisable()
        {
            _moveAction.action.Disable();
            _moveAction.action.performed -= OnMovementStarted;
            _moveAction.action.canceled -= OnMovementCanceled;

            _swipeAction.action.Disable();
            _swipeAction.action.performed -= OnSwipePerformed;
            _swipeAction.action.canceled -= OnSwipeCanceled;
        }

        #endregion

        #region Public Methods

        public void SetMovementEnabled(bool enabled)
        {
            _movementEnabled = enabled;

            if (!enabled) _direction = Vector2.zero;
        }

        #endregion

        #region Private Methods

        private void OnMovementStarted(InputAction.CallbackContext context)
        {
            _direction = context.ReadValue<Vector2>();
        }

        private void OnMovementCanceled(InputAction.CallbackContext context)
        {
            _direction = Vector2.zero;
        }

        private void OnSwipePerformed(InputAction.CallbackContext context)
            => _swipeDelta = context.ReadValue<Vector2>();

        private void OnSwipeCanceled(InputAction.CallbackContext context)
            => _swipeDelta = Vector2.zero;
        #endregion
    }
}
