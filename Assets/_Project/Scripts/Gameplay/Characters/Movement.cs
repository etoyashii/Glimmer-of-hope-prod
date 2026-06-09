using Unity.Cinemachine;
using Unity.Plastic.Newtonsoft.Json.Bson;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

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
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private Climbing _climbing;

        #endregion

        #region Public Properties

        public Vector3 MoveDirection => new(_direction.x, _direction.z, _direction.y);

        #endregion

        #region Private Fields

        private Vector3 _direction;
        private bool _movementEnabled = true;

        private Vector3 _airCurrentForce = Vector3.zero;
        private bool _inAirCurrent = false;
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

            //Gravity + AirCurrent upwards if in one
            float verticalCurrent = _inAirCurrent ? _airCurrentForce.y : 0f;

            if (_climbing != null)
                if (!_climbing.climbing)
                {
                    if (!_controller.isGrounded)
                        verticalVelocity += (_gravity + verticalCurrent) * Time.deltaTime;
                }
                
            Vector3 cameraForward = _playerCamera.transform.forward;
            Vector3 cameraRight = _playerCamera.transform.right;
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();

            //Position + AirCurrent horizontal if in one
            Vector3 moveDirection = (cameraRight * _direction.x + cameraForward * _direction.y).normalized;

            if (_inAirCurrent)
                moveDirection += new Vector3(_airCurrentForce.x, 0f, _airCurrentForce.z) * Time.deltaTime;

            //Rotation
            if (_climbing)
                if (!_climbing.climbing)
                    if (moveDirection.magnitude > 0.1f)
                    {
                        Quaternion toRotate = Quaternion.LookRotation(moveDirection);
                        transform.rotation = Quaternion.Lerp(transform.rotation, toRotate, 10f * Time.deltaTime);
                    }

            //Apply Everything to move the Player
            if (_climbing)
                if (_climbing.climbing)
                {
                    Vector3 wallNormal = _climbing.frontWallHit.normal;

                    float verticalInput = _direction.y;

                    Vector3 temp = new Vector3(moveDirection.x, verticalInput, moveDirection.z);

                    Vector3 moveAlongWall = Vector3.ProjectOnPlane(temp, wallNormal);

                    verticalVelocity = temp.y;
                    moveDirection.x = moveAlongWall.x;
                    moveDirection.z = moveAlongWall.z;
                }
            moveDirection.y = verticalVelocity;

            if (_climbing)
                if (_climbing.climbing)
                    moveDirection /= 5f;

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

        //Method called in WindCurrent.cs
        public void SetAirCurrent(bool active, Vector3 airCurrentForce = default)
        {
            _inAirCurrent = active;
            _airCurrentForce = active ? airCurrentForce : Vector3.zero;

            // If the current pushes upward, cancel downward velocity for immediate response
            if (active && airCurrentForce.y > 0f && verticalVelocity < 0f)
                verticalVelocity = 0f;
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
