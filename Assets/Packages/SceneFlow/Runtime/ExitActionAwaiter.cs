using System;
using System.Collections;
using System.Collections.Generic;
using Kebab.SceneFlow.Settings;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Kebab.SceneFlow
{
    /// <summary>
    /// Wait for the exit action to be pressed.
    /// </summary>
    public class ExitActionAwaiter : MonoBehaviour
    {
        private Action _action;
        private SceneFlowSettings.EKeyboardAction _keyboardActionFlags;
        private SceneFlowSettings.EGamepadAction _gamepadActionFlags;
        private SceneFlowSettings.EMouseAction _mouseActionFlags;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void Setup(Action action, SceneFlowSettings.EKeyboardAction keyboardActionFlags, SceneFlowSettings.EGamepadAction gamepadActionFlags, SceneFlowSettings.EMouseAction mouseActionFlags)
        {
            _action = action;
            _keyboardActionFlags = keyboardActionFlags;
            _gamepadActionFlags = gamepadActionFlags;
            _mouseActionFlags = mouseActionFlags;
        }


        private void Update()
        {
            if (ExitActionChecker.CheckExitInput(_keyboardActionFlags, _gamepadActionFlags, _mouseActionFlags))
            {
                _action.Invoke();
            }
        }

    }
}

