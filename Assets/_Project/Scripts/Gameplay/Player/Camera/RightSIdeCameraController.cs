using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// For the Thrid Person Camera mouvements
    /// </summary>

    #region Dependancies
    [RequireComponent(typeof(CinemachineOrbitalFollow))]
    #endregion
    public class RightSideCameraController : MonoBehaviour
    {
        #region SerializeFields
        [Header("Sensibility")]
        [SerializeField] private float _horizontalGain = 0.3f;
        [SerializeField] private float _verticalGain = 0.3f;

        [Header("Input Zone")]
        [Range(0f, 1f)]
        [Tooltip("Horizontal ratio allowing inputs to control the camera")]
        [SerializeField] private float _horizontalScreenSplitRatio = 0.5f;
        [Range(0f, 1f)]
        [Tooltip("Vertical ratio allowing inputs to control the camera")]
        [SerializeField] private float _verticalScreenSplitRatio = 0.2f;

        [SerializeField] private InputActionReference _lookAction;
        #endregion

        #region PrivateFields
        private CinemachineOrbitalFollow _orbitalFollow;
        private Vector2 _lookDelta;
        private float splitX => Screen.width * _horizontalScreenSplitRatio;
        private float splitY => Screen.height * _verticalScreenSplitRatio;
        #endregion
        #region Unity Lifecycle
        void Awake()
        {
            _orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
        }

        void OnEnable()
        {
            _lookAction.action.Enable();
            _lookAction.action.started += OnLookStarted;
            _lookAction.action.performed += OnLookPerformed;
            _lookAction.action.canceled += OnLookCanceled;
        }

        void OnDisable()
        {
            _lookAction.action.Disable();
            _lookAction.action.started -= OnLookStarted;
            _lookAction.action.performed -= OnLookPerformed;
            _lookAction.action.canceled -= OnLookCanceled;
        }

        void Update()
        {
            _orbitalFollow.HorizontalAxis.Value += _lookDelta.x * _horizontalGain;
            float newVertical = _orbitalFollow.VerticalAxis.Value - (_lookDelta.y * _verticalGain);
            _orbitalFollow.VerticalAxis.Value = Mathf.Clamp(newVertical, _orbitalFollow.VerticalAxis.Range.x, _orbitalFollow.VerticalAxis.Range.y);
        }
        #endregion

        #region Private Methods

        private void OnLookStarted(InputAction.CallbackContext context)
        {

        }
        private void OnLookPerformed(InputAction.CallbackContext context)
        {

            foreach (var touch in Touch.activeTouches)
            {
                // Filter touches with their initial position (left touches can't migrate right and vice versa)
                if (touch.startScreenPosition.x > splitX & touch.startScreenPosition.y > splitY)
                {
                    _lookDelta = context.ReadValue<Vector2>();
                }
            }
        }
        private void OnLookCanceled(InputAction.CallbackContext context)
        {
            _lookDelta = Vector2.zero;
        }
        #endregion
    }
}
