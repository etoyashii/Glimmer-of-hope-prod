using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay
{
    public class JoystickPointerTracker : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        // L'ID InputSystem (1-based) du touch qui contrôle le joystick (-1 = inactif)
        public static int JoystickTouchId { get; private set; } = -1;

        public void OnPointerDown(PointerEventData eventData)
        {
            // On retrouve le touch InputSystem par proximité de position
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                foreach (var touch in touchscreen.touches)
                {
                    if (touch.isInProgress &&
                        Vector2.Distance(touch.position.ReadValue(), eventData.position) < 30f)
                    {
                        JoystickTouchId = touch.touchId.ReadValue();
                        return;
                    }
                }
            }
            JoystickTouchId = -1;
        }

        public void OnPointerUp(PointerEventData eventData)
            => JoystickTouchId = -1;

        private void OnDisable()
            => JoystickTouchId = -1;
    }
}
