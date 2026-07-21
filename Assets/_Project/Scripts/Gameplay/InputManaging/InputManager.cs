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

        [Header("Events")]
        public UnityEvent<ControlScheme> OnSchemeChanged;

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

        #endregion

        #region Private Methods

        private void ApplyScheme(ControlScheme scheme, bool silent)
        {
            CurrentScheme = scheme;

            bool showMobileUI = scheme == ControlScheme.Mobile;
            foreach (GameObject root in _mobileUIRoots)
                if (root != null) root.SetActive(showMobileUI);

            if (!silent)
                OnSchemeChanged?.Invoke(scheme);

            Debug.Log($"[InputManager] Scheme set to: {scheme}");
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