using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// CameraController using a swipe on the right side of the screen.
    /// Use EnhancedTouchSupport to avoid conflict with other Touch Inputs.
    /// </summary>
    [RequireComponent(typeof(CinemachineOrbitalFollow))]
    public class RightSideCameraController : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Sensibility")]
        [SerializeField] private float _horizontalGain = 0.3f;
        [SerializeField] private float _verticalGain = 0.3f;

        [Header("Active zone")]
        [Range(0f, 1f)]
        [Tooltip("Horizontal limit => every touches left are ignored")]
        [SerializeField] private float _horizontalSplitRatio = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("Vertical limit => every touches under are ignored")]
        [SerializeField] private float _verticalSplitRatio = 0.2f;
        #endregion

        #region Private Fields
        private CinemachineOrbitalFollow _orbitalFollow;
        private int _trackedFingerId = -1;
        private Vector2 _lookDelta;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            _orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
        }

        
        void OnEnable() => EnhancedTouchSupport.Enable();
        void OnDisable() => EnhancedTouchSupport.Disable();

        void Update()
        {
            _lookDelta = Vector2.zero;

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
