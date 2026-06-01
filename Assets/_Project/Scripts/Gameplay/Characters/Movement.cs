using UnityEngine;
using UnityEngine.InputSystem;

using Unity.Cinemachine;

namespace GlimmerOfHope.Gameplay.Character.SpecialActions
{
    /// <summary>
    /// For the player movement. Based on gravity, velocity and deltaTime.
    /// </summary>

    #region Dependancies

    [RequireComponent(typeof(CharacterController))]

    #endregion
    public class Movement : MonoBehaviour
    {
        #region SerializeField

        [Header("Mouvement")]
        [Range(1.0f, 100.0f)]
        [SerializeField] private float _speed = 20.0f;
        [Range(-30.0f, 30.0f)]
        [SerializeField] private float _gravity = -9.81f;


        [Header("References")]
        [SerializeField] private InputActionReference _moveAction;
        [SerializeField] private CharacterController _controller;
        [SerializeField] private CinemachineCamera _playerCamera;

        #endregion

        #region Public Properties

        public Vector3 MoveDirection => new(_direction.x, 0f, _direction.y);

        #endregion

        #region Private Fields

        private Vector2 _direction;
        private bool _movementEnabled = true;

        #endregion

        #region Public Fields

        public float verticalVelocity;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            _moveAction.action.Enable();
            _moveAction.action.performed += OnMovementStarted;
            _moveAction.action.canceled += OnMovementCanceled;


        }

        private void Update()
        {
            if (!_movementEnabled) return;

            if (_controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = -2f;

            verticalVelocity += _gravity * Time.deltaTime;

            Vector3 cameraForward = _playerCamera.transform.forward;
            Vector3 cameraRight = _playerCamera.transform.right;
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 moveDirection = (cameraRight * _direction.x + cameraForward * _direction.y).normalized;


            if (moveDirection.magnitude > 0.1f)
            {
                Quaternion toRotate = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Lerp(transform.rotation, toRotate, 10f * Time.deltaTime);
            }

            moveDirection.y = verticalVelocity;
            _controller.Move(moveDirection * _speed * Time.deltaTime);
        }

        private void OnDisable()
        {
            _moveAction.action.Disable();
            _moveAction.action.performed -= OnMovementStarted;
            _moveAction.action.canceled -= OnMovementCanceled;


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

        #endregion
    }
}
