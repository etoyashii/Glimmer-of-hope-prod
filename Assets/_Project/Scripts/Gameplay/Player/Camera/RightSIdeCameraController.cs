using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// CameraController using either a swipe on the right side of the screen (mobile)
    /// or a right-click + mouse drag (PC), depending on the current platform.
    /// Use EnhancedTouchSupport to avoid conflict with other Touch Inputs.
    /// </summary>
    [RequireComponent(typeof(CinemachineOrbitalFollow))]
    public class RightSideCameraController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Sensibility")]
        [SerializeField] private float _horizontalGain = 0.3f;
        [SerializeField] private float _verticalGain = 0.3f;

        [Header("Active zone (mobile only)")]
        [Range(0f, 1f)]
        [Tooltip("Horizontal limit => every touches left are ignored")]
        [SerializeField] private float _horizontalSplitRatio = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("Vertical limit => every touches under are ignored")]
        [SerializeField] private float _verticalSplitRatio = 0.2f;

        [Header("References")]
        [Tooltip("Swipe action: One Modifier (Right Mouse Button) + Delta [Mouse], used on PC.")]
        [SerializeField] private InputActionReference _swipeAction;

        #endregion

        #region Private Fields

        private CinemachineOrbitalFollow _orbitalFollow;
        private int _trackedFingerId = -1;
        private Vector2 _lookDelta;

        // True on Desktop/Editor builds, false on mobile (Android/iOS)
        private bool _useMouseInput;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _orbitalFollow = GetComponent<CinemachineOrbitalFollow>();

#if UNITY_ANDROID || UNITY_IOS
            _useMouseInput = false;
#else
            _useMouseInput = true;
#endif
        }

        private void OnEnable()
        {
            if (_useMouseInput)
            {
                _swipeAction.action.Enable();
            }
            else
            {
                EnhancedTouchSupport.Enable();
            }
        }

        private void OnDisable()
        {
            if (_useMouseInput)
            {
                _swipeAction.action.Disable();
            }
            else
            {
                EnhancedTouchSupport.Disable();
            }
        }

        private void Update()
        {
            _lookDelta = Vector2.zero;

            if (_useMouseInput)
                UpdateMouseLook();
            else
                UpdateTouchLook();

            ApplyCameraRotation();
        }

        #endregion

        #region Private Methods — Mouse (PC)

        private void UpdateMouseLook()
        {
            // The action is only "performed" while the Right Mouse Button modifier is held,
            // so reading its value already gives us drag delta or zero otherwise.
            _lookDelta = _swipeAction.action.ReadValue<Vector2>();
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
                            _lookDelta = touch.delta;
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
        }

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

        private bool IsInRightZone(Vector2 screenPos)
        {
            return screenPos.x > Screen.width * _horizontalSplitRatio
                && screenPos.y > Screen.height * _verticalSplitRatio;
        }

        #endregion

        #region Private Methods — Shared

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

        #endregion
    }
}