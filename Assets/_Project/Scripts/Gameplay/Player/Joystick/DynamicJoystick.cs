using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.LowLevel;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// A dynamically placed joystick only on the left side of the screen.
    /// Using EnhancedTouchSupport to avoid conflicts with other Touch Inputs.
    /// Automatically disables itself when the active scheme is not Mobile.
    /// </summary>
    public class DynamicJoystick : MonoBehaviour
    {
        #region Serialized Fields

        [Header("UI References")]
        [SerializeField] private RectTransform _joystickBackground;
        [SerializeField] private RectTransform _joystickHandle;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Canvas _canvas;

        [Header("Activation Zone (bottom-left)")]
        [Range(0f, 1f)][SerializeField] private float _zoneWidth = 0.5f;
        [Range(0f, 1f)][SerializeField] private float _zoneHeight = 0.5f;

        [Header("Joystick Parameters")]
        [SerializeField] private float _joystickRadius = 100f;

        #endregion

        #region Private Fields

        private Gamepad _virtualGamepad;
        private int _trackedFingerId = -1;
        private Vector2 _anchorScreenPos;
        private bool _isActive;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _virtualGamepad = InputSystem.AddDevice<Gamepad>("VirtualGamepad");
        }

        private void OnDestroy()
        {
            if (_virtualGamepad != null)
                InputSystem.RemoveDevice(_virtualGamepad);
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();

            if (InputManager.Instance != null)
                InputManager.Instance.OnSchemeChanged.AddListener(OnSchemeChanged);
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();

            if (InputManager.Instance != null)
                InputManager.Instance.OnSchemeChanged.RemoveListener(OnSchemeChanged);
        }

        private void Start()
        {
            // Sync with current scheme on startup
            if (InputManager.Instance != null)
                _isActive = InputManager.Instance.IsMobile;
        }

        private void Update()
        {
            if (!_isActive) return;
            foreach (var touch in Touch.activeTouches)
            {
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        TryBeginJoystick(touch);
                        break;

                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        if (touch.touchId == _trackedFingerId)
                            UpdateJoystick(touch.screenPosition);
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (touch.touchId == _trackedFingerId)
                            ReleaseJoystick();
                        break;
                }
            }
        }

        #endregion

        #region Private Methods — Scheme

        private void OnSchemeChanged(InputManager.ControlScheme scheme)
        {
            _isActive = scheme == InputManager.ControlScheme.Mobile;

            // Release joystick immediately if scheme switches away from mobile mid-drag
            if (!_isActive && _trackedFingerId != -1)
                ReleaseJoystick();
        }

        #endregion

        #region Private Methods — Joystick

        private void TryBeginJoystick(Touch touch)
        {
            if (!IsInZone(touch.screenPosition)) return;
            if (_trackedFingerId != -1) return;

            _trackedFingerId = touch.touchId;
            _anchorScreenPos = touch.screenPosition;

            Vector2 canvasPos = ScreenToCanvasPoint(touch.screenPosition);
            _joystickBackground.anchoredPosition = new Vector2(
                canvasPos.x - _joystickBackground.rect.width / 2f,
                canvasPos.y - _joystickBackground.rect.height / 2f
            );
            _joystickHandle.anchoredPosition = new Vector2(
                -_joystickHandle.rect.width / 2f,
                -_joystickHandle.rect.height / 2f
            );

            _canvasGroup.alpha = 1f;
        }

        private void UpdateJoystick(Vector2 screenPos)
        {
            Vector2 currentLocal = ScreenToCanvasPoint(screenPos);
            Vector2 anchorLocal = ScreenToCanvasPoint(_anchorScreenPos);

            Vector2 deltaUI = currentLocal - anchorLocal;
            Vector2 clamped = Vector2.ClampMagnitude(deltaUI, _joystickRadius);
            Vector2 normalized = clamped / _joystickRadius;

            _joystickHandle.anchoredPosition = new Vector2(
                clamped.x - _joystickHandle.rect.width / 2f,
                clamped.y - _joystickHandle.rect.height / 2f
            );

            SendStickValue(normalized);
        }

        private void ReleaseJoystick()
        {
            _canvasGroup.alpha = 0f;
            _joystickHandle.anchoredPosition = Vector2.zero;
            _trackedFingerId = -1;
            SendStickValue(Vector2.zero);
        }

        private bool IsInZone(Vector2 screenPos)
        {
            return screenPos.x < Screen.width * _zoneWidth
                && screenPos.y < Screen.height * _zoneHeight;
        }

        private Vector2 ScreenToCanvasPoint(Vector2 screenPos)
        {
            Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _canvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _joystickBackground.parent as RectTransform,
                screenPos,
                cam,
                out Vector2 localPoint
            );
            return localPoint;
        }

        private void SendStickValue(Vector2 value)
        {
            InputSystem.QueueStateEvent(_virtualGamepad, new GamepadState { leftStick = value });
        }

        #endregion
    }
}