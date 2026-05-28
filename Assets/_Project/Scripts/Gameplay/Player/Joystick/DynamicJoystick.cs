using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.LowLevel;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// A dynamically placed joystick with restricted area.
    /// </summary>
    public class DynamicJoystick : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Références UI")]
        [SerializeField] private RectTransform _joystickBackground;
        [SerializeField] private RectTransform _joystickHandle;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Canvas _canvas;

        [Header("Zone d'activation (bas-gauche)")]
        [Range(0f, 1f)][SerializeField] private float _zoneWidth = 0.5f;
        [Range(0f, 1f)][SerializeField] private float _zoneHeight = 0.5f;

        [Header("Paramètres du joystick")]
        [SerializeField] private float _joystickRadius = 100f; // en pixels UI
        #endregion

        #region Private Fields
        private Gamepad _virtualGamepad;
        private int _trackedFingerId = -1;
        private Vector2 _anchorScreenPos;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            _virtualGamepad = InputSystem.AddDevice<Gamepad>("VirtualGamepad");
        }

        void OnDestroy()
        {
            if (_virtualGamepad != null)
                InputSystem.RemoveDevice(_virtualGamepad);
        }

        void OnEnable() => EnhancedTouchSupport.Enable();
        void OnDisable() => EnhancedTouchSupport.Disable();

        void Update()
        {
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

        #region Private Methods
        private void TryBeginJoystick(Touch touch)
        {
            if (!IsInZone(touch.screenPosition)) return;
            if (_trackedFingerId != -1) return;

            _trackedFingerId = touch.touchId;
            Debug.Log("Joystick = " + _trackedFingerId);
            _anchorScreenPos = touch.screenPosition;

            _joystickBackground.anchoredPosition = new Vector2 (ScreenToCanvasPoint(touch.screenPosition).x - (_joystickBackground.rect.width/2), ScreenToCanvasPoint(touch.screenPosition).y - (_joystickBackground.rect.height/2));
            _joystickHandle.anchoredPosition = new Vector2 (-_joystickHandle.rect.width/2, -_joystickHandle.rect.height / 2);

            _canvasGroup.alpha = 1f;
        }

        private void UpdateJoystick(Vector2 screenPos)
        {
            Vector2 currentLocal = ScreenToCanvasPoint(screenPos);
            Vector2 anchorLocal = ScreenToCanvasPoint(_anchorScreenPos);

            Vector2 deltaUI = currentLocal - anchorLocal;
            Vector2 clamped = Vector2.ClampMagnitude(deltaUI, _joystickRadius);
            Vector2 normalized = clamped / _joystickRadius;

            _joystickHandle.anchoredPosition = new Vector2 (clamped.x - (_joystickHandle.rect.width / 2), clamped.y - (_joystickHandle.rect.height / 2));
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
