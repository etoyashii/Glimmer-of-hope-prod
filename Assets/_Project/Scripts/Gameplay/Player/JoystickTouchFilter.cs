using UnityEngine;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay
{

#if UNITY_EDITOR
    using UnityEditor;
#endif
    public class JoystickTouchFilter : InputProcessor<Vector2>
    {
      
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Register() => InputSystem.RegisterProcessor<JoystickTouchFilter>();

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
        static void RegisterEditor() => InputSystem.RegisterProcessor<JoystickTouchFilter>();
#endif

        public override Vector2 Process(Vector2 value, InputControl control)
        {
         
            if (JoystickPointerTracker.JoystickTouchId == -1)
                return value;

            var touchscreen = Touchscreen.current;
            if (touchscreen == null) return value;

         
            foreach (var touch in touchscreen.touches)
            {
                if (!touch.isInProgress) continue;
                if (touch.touchId.ReadValue() != JoystickPointerTracker.JoystickTouchId)
                    return value; 
            }

  
            return Vector2.zero;
        }
    }
}
