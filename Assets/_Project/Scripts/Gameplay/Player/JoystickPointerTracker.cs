using UnityEngine;
using UnityEngine.EventSystems;

namespace GlimmerOfHope.Gameplay
{
    public class JoystickPointerTracker : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public static int JoystickPointerId { get; private set; } = int.MinValue;

        public void OnPointerDown(PointerEventData eventData)
            => JoystickPointerId = eventData.pointerId;

        public void OnPointerUp(PointerEventData eventData)
            => JoystickPointerId = int.MinValue;
    }
}
