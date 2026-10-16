using System;
using System.Collections.Generic;
using System.Linq;
using Kebab.SceneFlow.Settings;
using UnityEngine;

#if NEW_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

/// <summary>
/// Check if the exit action is pressed.
/// </summary>
public static class ExitActionChecker
{
    #if NEW_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM

    private static ButtonControl GetButtonControl(Gamepad gamepad, SceneFlowSettings.EGamepadAction gamepadAction)
    {
        return gamepadAction switch
        {
            SceneFlowSettings.EGamepadAction.A => gamepad.aButton,
            SceneFlowSettings.EGamepadAction.B => gamepad.bButton,
            SceneFlowSettings.EGamepadAction.X => gamepad.xButton,
            SceneFlowSettings.EGamepadAction.Y => gamepad.yButton,
            SceneFlowSettings.EGamepadAction.Start => gamepad.startButton,
            _ => throw new ArgumentException($"Unsupported gamepad action: {gamepadAction}", nameof(gamepadAction))
        };
    }

    private static ButtonControl GetKeyControl(Keyboard keyboard, SceneFlowSettings.EKeyboardAction keyboardAction)
    {
        return keyboardAction switch
        {

            SceneFlowSettings.EKeyboardAction.Space => keyboard.spaceKey,
            SceneFlowSettings.EKeyboardAction.Enter => keyboard.enterKey,
            SceneFlowSettings.EKeyboardAction.Escape => keyboard.escapeKey,
            _ => throw new ArgumentException($"Unsupported keyboard action: {keyboardAction}", nameof(keyboardAction))
        };
    }

    private static ButtonControl GetMouseControl(Mouse mouse, SceneFlowSettings.EMouseAction mouseAction)
    {
        return mouseAction switch
        {
            SceneFlowSettings.EMouseAction.LeftClick => mouse.leftButton,
            SceneFlowSettings.EMouseAction.RightClick => mouse.rightButton,
            _ => throw new ArgumentException($"Unsupported mouse action: {mouseAction}", nameof(mouseAction))
        };
    }


    #elif ENABLE_LEGACY_INPUT_MANAGER
    private static readonly Dictionary<SceneFlowSettings.EKeyboardAction, KeyCode> KEYCODE_FROM_KEYBOARD_ACTION = new()
    {
        { SceneFlowSettings.EKeyboardAction.Space, KeyCode.Space },
        { SceneFlowSettings.EKeyboardAction.Enter, KeyCode.Return },
        { SceneFlowSettings.EKeyboardAction.Escape, KeyCode.Escape },
    };


    private static readonly  Dictionary<SceneFlowSettings.EGamepadAction, KeyCode> KEYCODE_FROM_GAMEPAD_ACTION = new()
    {
        { SceneFlowSettings.EGamepadAction.A, KeyCode.JoystickButton0 },
        { SceneFlowSettings.EGamepadAction.B, KeyCode.JoystickButton1 },
        { SceneFlowSettings.EGamepadAction.X, KeyCode.JoystickButton2 },
        { SceneFlowSettings.EGamepadAction.Y, KeyCode.JoystickButton3 },
        { SceneFlowSettings.EGamepadAction.Start, KeyCode.JoystickButton7 },
    };

    private static readonly  Dictionary<SceneFlowSettings.EMouseAction, KeyCode> KEYCODE_FROM_MOUSE_ACTION = new()
    {
        { SceneFlowSettings.EMouseAction.LeftClick, KeyCode.Mouse0 },
        { SceneFlowSettings.EMouseAction.RightClick, KeyCode.Mouse1 },
    };
    #endif

    public static bool CheckExitInput(SceneFlowSettings.EKeyboardAction keyboardActionFlags, SceneFlowSettings.EGamepadAction gamepadActionFlags, SceneFlowSettings.EMouseAction mouseActionFlags)
    {
        SceneFlowSettings.EKeyboardAction[] keyboardActionArray = GetEnumArray(keyboardActionFlags);
        SceneFlowSettings.EGamepadAction[] gamepadActionArray = GetEnumArray(gamepadActionFlags);
        SceneFlowSettings.EMouseAction[] mouseActionArray = GetEnumArray(mouseActionFlags);

        #if NEW_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM

        if (Gamepad.current != null)
        {
            foreach (SceneFlowSettings.EGamepadAction gamepadAction in gamepadActionArray)
            {
                ButtonControl buttonControl = GetButtonControl(Gamepad.current, gamepadAction);
                if (buttonControl != null && buttonControl.wasPressedThisFrame) return true;
            }
        }

        if (Keyboard.current != null)
        {
            foreach (SceneFlowSettings.EKeyboardAction keyboardAction in keyboardActionArray)
            {
                ButtonControl buttonControl = GetKeyControl(Keyboard.current, keyboardAction);
                if (buttonControl != null && buttonControl.wasPressedThisFrame) return true;
            }
        }

        if (Mouse.current != null)
        {
            foreach (SceneFlowSettings.EMouseAction mouseAction in mouseActionArray)
            {
                ButtonControl buttonControl = GetMouseControl(Mouse.current, mouseAction);
                if (buttonControl != null && buttonControl.wasPressedThisFrame) return true;
            }
        }

        #elif ENABLE_LEGACY_INPUT_MANAGER


        KeyCode keyCode = KeyCode.None;

        // Check keyboard actions
        foreach (SceneFlowSettings.EKeyboardAction keyboardAction in keyboardActionArray)
        {
            if (KEYCODE_FROM_KEYBOARD_ACTION.TryGetValue(keyboardAction, out keyCode))
                if (Input.GetKeyDown(keyCode)) return true;
        }


        // Check gamepad actions
        foreach (SceneFlowSettings.EGamepadAction gamepadAction in gamepadActionArray)
        {
            if (KEYCODE_FROM_GAMEPAD_ACTION.TryGetValue(gamepadAction, out keyCode))
                if (Input.GetKeyDown(keyCode)) return true;
        }

        // Check mouse actions
        foreach (SceneFlowSettings.EMouseAction mouseAction in mouseActionArray)
        {
            if (KEYCODE_FROM_MOUSE_ACTION.TryGetValue(mouseAction, out keyCode))
                if (Input.GetKeyDown(keyCode)) return true;
        }
        #endif

        return false;
    }

    private static T[] GetEnumArray<T>(T action) where T : Enum
    {
        return Enum.GetValues(typeof(T))
            .Cast<T>()
            .Where(x => action.HasFlag(x))
            .ToArray();
    }
}

