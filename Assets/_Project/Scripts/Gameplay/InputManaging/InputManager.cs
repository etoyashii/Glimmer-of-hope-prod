using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Central input manager. Holds the current control scheme and notifies
    /// all listeners when it changes. All input scripts read from here.
    /// Use SetScheme() to switch between schemes manually.
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
        }

        private void OnDisable()
        {
            if (MenuInput != null)
                MenuInput.action.performed -= OnMenuInputPressed;
        }

        #endregion

        #region Public Methods

        /// <summary>Switches to the given control scheme and notifies all listeners.</summary>
        public void SetScheme(ControlScheme scheme)
        {
            if (CurrentScheme == scheme) return;
            ApplyScheme(scheme, silent: false);
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