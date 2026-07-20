using GlimmerOfHope.Gameplay.Character.SpecialActions;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Makes the player jump.
    /// On mobile: call PerformJump() directly from a UI button.
    /// On Keyboard/Mouse and Gamepad: jump is triggered by the Jump InputAction
    /// (Space for keyboard, Button South for gamepad).
    /// </summary>
    #region Dependancies

    [RequireComponent(typeof(Rigidbody))]

    #endregion
    public class Jump : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Refs")]
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private Movement _playerMovement;
        [SerializeField] private Animator _animator;
        [SerializeField] private Slide _slide;

        [Header("VFX")]
        [Tooltip("VFX played on a normal jump.")]
        [SerializeField] private ParticleSystem _jumpVFX;

        [Tooltip("VFX played on a strong impulse jump.")]
        [SerializeField] private ParticleSystem _impulseVFX;

        [Tooltip("jumpForce threshold above which the impulse VFX plays instead of the jump VFX.")]
        [SerializeField] private float _jumpImpulseLimit;

        [Header("Input")]
        [Tooltip("Jump action — bind Space [Keyboard] and Button South [Gamepad] here.")]
        [SerializeField] private InputActionReference _jumpAction;

        #endregion

        #region Events

        public event Action OnStartJumping;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (_jumpAction != null)
            {
                _jumpAction.action.Enable();
                _jumpAction.action.performed += OnJumpActionPerformed;
            }

            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnSchemeChanged.AddListener(OnSchemeChanged);
                ApplyBindingMask(InputManager.Instance.CurrentScheme);
            }
        }

        private void OnDisable()
        {
            if (_jumpAction != null)
            {
                _jumpAction.action.Disable();
                _jumpAction.action.performed -= OnJumpActionPerformed;
            }

            if (InputManager.Instance != null)
                InputManager.Instance.OnSchemeChanged.RemoveListener(OnSchemeChanged);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gives the player a vertical impulse.
        /// Called directly from a UI button on mobile,
        /// or automatically from the Jump InputAction on other schemes.
        /// </summary>
        public void PerformJump(float jumpForce)
        {
            if (!_playerMovement.IsGrounded()) return;

            PlayJumpVFX(jumpForce);
            
            if (_slide.isSliding)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.AddForce(_playerMovement.lastHit.normal * jumpForce, ForceMode.Impulse);
                _slide.isSliding = false;
                Debug.Log("Jump slide");
            }
            else
            {
                _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
                _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            }

            if (_animator != null)
                _animator.SetTrigger("Jump");

            OnStartJumping?.Invoke();
        }

        #endregion

        #region Private Methods

        private void OnJumpActionPerformed(InputAction.CallbackContext context)
        {
            // Default jump force when triggered by keyboard or gamepad.
            // Adjust this value or expose it as a SerializeField if needed.
            PerformJump(3.1f);
        }

        private void OnSchemeChanged(InputManager.ControlScheme scheme)
        {
            ApplyBindingMask(scheme);
        }

        /// <summary>
        /// On mobile the jump action is disabled entirely (UI button calls PerformJump directly).
        /// On other schemes only the relevant bindings are active.
        /// </summary>
        private void ApplyBindingMask(InputManager.ControlScheme scheme)
        {
            if (_jumpAction == null) return;

            _jumpAction.action.bindingMask = scheme switch
            {
                InputManager.ControlScheme.Mobile => null, // action disabled below
                InputManager.ControlScheme.KeyboardMouse => InputBinding.MaskByGroup("Keyboard/Mouse"),
                InputManager.ControlScheme.Gamepad => InputBinding.MaskByGroup("Gamepad"),
                _ => null
            };

            // On mobile the input action is not needed — the UI button handles it
            if (scheme == InputManager.ControlScheme.Mobile)
                _jumpAction.action.Disable();
            else
                _jumpAction.action.Enable();
        }

        private void PlayJumpVFX(float jumpForce)
        {
            if (jumpForce < _jumpImpulseLimit)
            {
                if (_jumpVFX != null)
                {
                    _jumpVFX.transform.position = transform.position;
                    _jumpVFX.transform.rotation = Quaternion.LookRotation(transform.up);
                    _jumpVFX.Play();
                }
            }
            else
            {
                if (_impulseVFX != null)
                {
                    _impulseVFX.transform.position = transform.position;
                    _impulseVFX.transform.rotation = Quaternion.LookRotation(transform.up);
                    _impulseVFX.Play();
                }
            }
        }

        #endregion
    }
}