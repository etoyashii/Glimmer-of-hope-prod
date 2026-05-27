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
            // Pas de joystick actif  comportement normal
            if (JoystickPointerTracker.JoystickTouchId == -1)
                return value;

            var touchscreen = Touchscreen.current;
            if (touchscreen == null) return value;

            foreach (var touch in touchscreen.touches)
            {
                if (!touch.isInProgress) continue;

                // On ignore le touch du joystick
                if (touch.touchId.ReadValue() == JoystickPointerTracker.JoystickTouchId)
                    continue;

                // On retourne le delta du 2ème doigt (le vrai swipe caméra)
                return touch.delta.ReadValue();
            }

            // Seul le joystick est actif  on bloque
            return Vector2.zero;
        }
    }
}
