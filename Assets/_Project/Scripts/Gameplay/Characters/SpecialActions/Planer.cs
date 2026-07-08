using GlimmerOfHope.Gameplay.Character.SpecialActions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// A planer limiting falling speed.
    /// On mobile: call PerformPlaner() directly from a UI button.
    /// On Keyboard/Mouse and Gamepad: triggered by the same Jump InputAction
    /// (Space for keyboard, Button South for gamepad) when not grounded.
    /// </summary>
    public class Planer : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Refs")]
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private Movement _playerMovement;

        [Tooltip("Maximum falling speed while planer is active.")]
        [SerializeField] private float _planningVerticalVelocity = -1f;

        [Header("Input")]
        [Tooltip("Same Jump action used in Jump.cs — Space [Keyboard] and Button South [Gamepad].")]
        [SerializeField] private InputActionReference _jumpAction;

        #endregion

        #region Private Fields

        private bool _isPlanningActive = false;

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

        private void FixedUpdate()
        {
            // Disable planer automatically when grounded
            if (_playerMovement.IsGrounded())
            {
                if (_isPlanningActive) _isPlanningActive = false;
                return;
            }

            // Clamp falling speed when planer is active
            if (_isPlanningActive && _rb.linearVelocity.y < _planningVerticalVelocity)
            {
                Vector3 vel = _rb.linearVelocity;
                vel.y = Mathf.Lerp(vel.y, _planningVerticalVelocity, 0.5f);
                _rb.linearVelocity = vel;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Toggles the planer on or off.
        /// Called directly from a UI button on mobile,
        /// or automatically from the Jump InputAction when not grounded on other schemes.
        /// </summary>
        public void PerformPlaner()
        {
            if (_playerMovement.IsGrounded()) return;

            _isPlanningActive = !_isPlanningActive;
        }

        #endregion

        #region Private Methods

        private void OnJumpActionPerformed(InputAction.CallbackContext context)
        {
            // Only handle glide when airborne — Jump.cs handles the grounded case
            if (!_playerMovement.IsGrounded())
                PerformPlaner();
        }

        private void OnSchemeChanged(InputManager.ControlScheme scheme)
        {
            // Cancel active glide immediately on scheme switch
            _isPlanningActive = false;
            ApplyBindingMask(scheme);
        }

        /// <summary>
        /// On mobile the action is disabled — the UI button calls PerformPlaner() directly.
        /// Mirrors the exact same mask logic as Jump.cs.
        /// </summary>
        private void ApplyBindingMask(InputManager.ControlScheme scheme)
        {
            if (_jumpAction == null) return;

            _jumpAction.action.bindingMask = scheme switch
            {
                InputManager.ControlScheme.Mobile => null,
                InputManager.ControlScheme.KeyboardMouse => InputBinding.MaskByGroup("Keyboard/Mouse"),
                InputManager.ControlScheme.Gamepad => InputBinding.MaskByGroup("Gamepad"),
                _ => null
            };

            if (scheme == InputManager.ControlScheme.Mobile)
                _jumpAction.action.Disable();
            else
                _jumpAction.action.Enable();
        }

        #endregion
    }
}