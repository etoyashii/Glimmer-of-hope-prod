using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Central input manager. Holds the current control scheme and notifies
    /// all listeners when it changes. All input scripts read from here.
    /// Use SetScheme() to switch between schemes manually, or let it switch
    /// automatically on the first input coming from another kind of device.
    /// </summary>

    [DefaultExecutionOrder(-100)]
    public class InputManager : MonoBehaviour
    {
        #region Singleton

        public static InputManager Instance { get; private set; }


        #endregion

        #region Inner Types

        public enum ControlScheme
        {
            Mobile,
            KeyboardMouse,
            Gamepad
        }

        #endregion

        #region Serialized Fields

        [Header("Default Scheme")]
        [Tooltip("Control scheme active on startup.")]
        [SerializeField] private ControlScheme _defaultScheme = ControlScheme.Mobile;

        [Header("Auto Switch")]
        [Tooltip("Switch scheme automatically when a button is pressed on another kind of device.")]
        [SerializeField] private bool _autoSwitchScheme = true;

        [Header("Mobile UI")]
        [Tooltip("All mobile-only UI root GameObjects to show/hide on scheme change.")]
        [SerializeField] private GameObject[] _mobileUIRoots;

        [SerializeField] private InputActionReference MenuInput;

        [Header("Events")]
        public UnityEvent<ControlScheme> OnSchemeChanged;

        public UnityEvent OpenMenu;
        #endregion

        #region Public Properties

        public ControlScheme CurrentScheme { get; private set; }

        private IDisposable _anyButtonPressListener;
        private ControlScheme? _pendingScheme;

        public bool IsMobile => CurrentScheme == ControlScheme.Mobile;
        public bool IsKeyboardMouse => CurrentScheme == ControlScheme.KeyboardMouse;
        public bool IsGamepad => CurrentScheme == ControlScheme.Gamepad;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            ApplyScheme(DetectInitialScheme(), silent: true);
        }

        private void OnEnable()
        {
            if (MenuInput != null)
                MenuInput.action.performed += OnMenuInputPressed;

            if (_autoSwitchScheme)
                _anyButtonPressListener = InputSystem.onAnyButtonPress.Call(OnAnyButtonPressed);
        }

        private void Update()
        {
            // Applied here rather than in the input callback: changing binding masks
            // while the Input System is processing events is not safe.
            if (_pendingScheme == null) return;
            SetScheme(_pendingScheme.Value);
            _pendingScheme = null;
        }

        private void OnDisable()
        {
            if (MenuInput != null)
                MenuInput.action.performed -= OnMenuInputPressed;

            _anyButtonPressListener?.Dispose();
            _anyButtonPressListener = null;
        }

        #endregion

        #region Public Methods

        /// <summary>Switches to the given control scheme and notifies all listeners.</summary>
        public void SetScheme(ControlScheme scheme)
        {
            if (CurrentScheme == scheme) return;
            ApplyScheme(scheme, silent: false);
        }

        public static void FreezeInput(bool isActive)
        {
            if (isActive)
            {
                InputSystem.actions.FindActionMap("Player").Enable();
            }
            else
            {
                InputSystem.actions.FindActionMap("Player").Disable();
            }
        }
        
        // Convenience wrappers for UI buttons
        public void SetSchemeMobile() => SetScheme(ControlScheme.Mobile);
        public void SetSchemeKeyboardMouse() => SetScheme(ControlScheme.KeyboardMouse);
        public void SetSchemeGamepad() => SetScheme(ControlScheme.Gamepad);

        public void QuitApp()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
        }
        #endregion

        #region Private Methods

        private void ApplyScheme(ControlScheme scheme, bool silent)
        {
            CurrentScheme = scheme;

            bool showMobileUI = scheme == ControlScheme.Mobile;
            foreach (GameObject root in _mobileUIRoots)
                if (root != null) root.SetActive(showMobileUI);

            ApplyMenuBindingMask(scheme);

            if (!silent)
                OnSchemeChanged?.Invoke(scheme);

            Debug.Log($"[InputManager] Scheme set to: {scheme}");
        }

        /// <summary>
        /// On mobile the menu action is disabled, the UI hamburger button calls
        /// OpenMenu directly. On other schemes only the relevant bindings are
        /// active, same masking pattern as Jump and Movement.
        /// </summary>
        private void ApplyMenuBindingMask(ControlScheme scheme)
        {
            if (MenuInput == null) return;

            MenuInput.action.bindingMask = scheme switch
            {
                ControlScheme.Mobile => null,
                ControlScheme.KeyboardMouse => InputBinding.MaskByGroup("Keyboard/Mouse"),
                ControlScheme.Gamepad => InputBinding.MaskByGroup("Gamepad"),
                _ => null
            };

            if (scheme == ControlScheme.Mobile)
                MenuInput.action.Disable();
            else
                MenuInput.action.Enable();
        }

        private void OnMenuInputPressed(InputAction.CallbackContext ctx)
        {
            OpenMenu?.Invoke();
        }

        private void OnAnyButtonPressed(InputControl control)
        {
            ControlScheme? scheme = GetSchemeForDevice(control.device);
            if (scheme != null && scheme != CurrentScheme)
                _pendingScheme = scheme;
        }

        private ControlScheme? GetSchemeForDevice(InputDevice device)
        {
            // On-screen controls (virtual stick/buttons) feed non-native devices, ignore them
            // so touching the mobile UI doesn't switch to Gamepad.
            if (!device.native) return null;

            return device switch
            {
                Touchscreen => ControlScheme.Mobile,
                Gamepad => ControlScheme.Gamepad,
                Keyboard => ControlScheme.KeyboardMouse,
                // A mouse click while on Mobile is most likely the editor simulating touch.
                Mouse => CurrentScheme == ControlScheme.Mobile ? null : ControlScheme.KeyboardMouse,
                _ => null
            };
        }

        private ControlScheme DetectInitialScheme()
        {
#if UNITY_ANDROID || UNITY_IOS
    return ControlScheme.Mobile;
#else
            return Gamepad.all.Count > 0 ? ControlScheme.Gamepad : ControlScheme.KeyboardMouse;
#endif
        }
        #endregion
    }
}