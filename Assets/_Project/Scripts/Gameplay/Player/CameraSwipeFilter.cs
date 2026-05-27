using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay
{
    public class CameraSwipeFilter : MonoBehaviour
    {
        [SerializeField] private InputActionReference swipeAction;
        private CinemachineInputAxisController _cmController;
        private Vector2 _filteredDelta;

        void Awake() => _cmController = GetComponent<CinemachineInputAxisController>();

        void OnEnable() => swipeAction.action.performed += OnSwipe;
        void OnDisable() => swipeAction.action.performed -= OnSwipe;

        private void OnSwipe(InputAction.CallbackContext ctx)
        {
            // Vérifie si le touch actif appartient au joystick
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                foreach (var touch in touchscreen.touches)
                {
                    // pointerId dans EventSystem = touchId - 1 dans InputSystem
                    if (touch.isInProgress &&
                        touch.touchId.ReadValue() == JoystickPointerTracker.JoystickPointerId + 1)
                    {
                        _filteredDelta = Vector2.zero; // ce touch vient du joystick, on ignore
                        return;
                    }
                }
            }
            _filteredDelta = ctx.ReadValue<Vector2>();
        }
    }
}
