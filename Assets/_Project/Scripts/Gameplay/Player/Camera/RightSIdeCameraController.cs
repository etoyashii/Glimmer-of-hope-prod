using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// CameraController using either:
    /// - Mobile: swipe on the right side of the screen
    /// - KeyboardMouse: right-click + mouse drag
    /// - Gamepad: right stick
    /// Switches automatically when InputManager.SetScheme() is called.
    /// </summary>
    [RequireComponent(typeof(CinemachineOrbitalFollow))]
    public class RightSideCameraController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Sensitivity")]
        [SerializeField] private float _horizontalGain = 0.3f;
        [SerializeField] private float _verticalGain = 0.3f;

        [Header("Active Zone (Mobile only)")]
        [Range(0f, 1f)]
        [Tooltip("Horizontal limit: touches to the left of this ratio are ignored.")]
        [SerializeField] private float _horizontalSplitRatio = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("Vertical limit: touches below this ratio are ignored.")]
        [SerializeField] private float _verticalSplitRatio = 0.2f;

        [Header("References")]
        [Tooltip("Swipe action: One Modifier (Right Mouse Button) + Delta [Mouse], used on PC.")]
        [SerializeField] private InputActionReference _swipeAction;

        [Tooltip("Right stick action for gamepad camera control.")]
        [SerializeField] private InputActionReference _rightStickAction;

        [Header("Gamepad Sensitivity")]
        [Tooltip("Multiplier applied to the right stick value each frame.")]
        [SerializeField] private float _gamepadHorizontalGain = 3f;
        [SerializeField] private float _gamepadVerticalGain = 2f;
        #endregion

        #region Private Fields
        private CinemachineOrbitalFollow _orbitalFollow;
        private int _trackedFingerId = -1;
        private Vector2 _lookDelta;

        private InputManager.ControlScheme _currentScheme;

        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            _orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            _swipeAction.action.Enable();
            _rightStickAction.action.Enable();

            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnSchemeChanged.AddListener(OnSchemeChanged);
                _currentScheme = InputManager.Instance.CurrentScheme;
            }
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
            _swipeAction.action.Disable();
            _rightStickAction.action.Disable();

            if (InputManager.Instance != null)
                InputManager.Instance.OnSchemeChanged.RemoveListener(OnSchemeChanged);
        }

        private void Update()
        {
            _lookDelta = Vector2.zero;

            switch (_currentScheme)
            {
                case InputManager.ControlScheme.Mobile:
                    UpdateTouchLook();
                    break;

                case InputManager.ControlScheme.KeyboardMouse:
                    UpdateMouseLook();
                    break;

                case InputManager.ControlScheme.Gamepad:
                    UpdateGamepadLook();
                    break;
            }

            ApplyCameraRotation();
        }

        #endregion

        #region Private Methods — Scheme

        private void OnSchemeChanged(InputManager.ControlScheme scheme)
        {
            _currentScheme = scheme;

            // Release any tracked touch immediately when leaving mobile
            if (scheme != InputManager.ControlScheme.Mobile && _trackedFingerId != -1)
                ReleaseLook();
        }

        #endregion

        #region Private Methods — Mouse (PC)

        private void UpdateMouseLook()
        {
            // The action only fires while Right Mouse Button is held
            _lookDelta = _swipeAction.action.ReadValue<Vector2>();
        }

        #endregion

        #region Private Methods — Gamepad

        private void UpdateGamepadLook()
        {
            Vector2 stick = _rightStickAction.action.ReadValue<Vector2>();

            // Scale by gains and deltaTime so speed is frame-rate independent
            _lookDelta = new Vector2(
                stick.x * _gamepadHorizontalGain * Time.deltaTime * 100f,
                stick.y * _gamepadVerticalGain * Time.deltaTime * 100f
            );
        }

        #endregion

        #region Private Methods — Touch (Mobile)

        private void UpdateTouchLook()
        {
            foreach (var touch in Touch.activeTouches)
            {
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        TryBeginLook(touch);
                        break;

                    case TouchPhase.Moved:
                        if (touch.touchId == _trackedFingerId)
                        {
                            _lookDelta = touch.delta;
                        }
                        break;

                    case TouchPhase.Stationary:
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (touch.touchId == _trackedFingerId)
                            ReleaseLook();
                        break;
                }
            }

            ApplyCameraRotation();
        }
        #endregion

        #region Private Methods
        private void TryBeginLook(Touch touch)
        {
            if (_trackedFingerId != -1) return;
            if (!IsInRightZone(touch.screenPosition)) return;
            _trackedFingerId = touch.touchId;
        }

        private void ReleaseLook()
        {
            _trackedFingerId = -1;
            _lookDelta = Vector2.zero;
        }

        private void ApplyCameraRotation()
        {
            if (_lookDelta == Vector2.zero) return;

            _orbitalFollow.HorizontalAxis.Value += _lookDelta.x * _horizontalGain;

            float newVertical = _orbitalFollow.VerticalAxis.Value - (_lookDelta.y * _verticalGain);
            _orbitalFollow.VerticalAxis.Value = Mathf.Clamp(
                newVertical,
                _orbitalFollow.VerticalAxis.Range.x,
                _orbitalFollow.VerticalAxis.Range.y
            );
        }

        private bool IsInRightZone(Vector2 screenPos)
        {
            return screenPos.x > Screen.width * _horizontalSplitRatio
                && screenPos.y > Screen.height * _verticalSplitRatio;
        }
        #endregion
    }
}
