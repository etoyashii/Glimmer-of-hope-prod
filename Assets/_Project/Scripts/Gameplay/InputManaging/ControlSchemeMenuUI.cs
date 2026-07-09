using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Simple control scheme selection menu.
    /// Attach to a UI panel with three buttons (Mobile, Keyboard/Mouse, Gamepad).
    /// The active button is highlighted; the others are dimmed.
    /// </summary>
    public class ControlSchemeMenuUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Buttons")]
        [SerializeField] private Button _mobileButton;
        [SerializeField] private Button _keyboardMouseButton;
        [SerializeField] private Button _gamepadButton;

        [Header("Highlight Colors")]
        [SerializeField] private Color _activeColor = new Color(0.3f, 0.8f, 0.4f);
        [SerializeField] private Color _inactiveColor = new Color(0.6f, 0.6f, 0.6f);

        [Header("Menu Panel")]
        [Tooltip("Root panel to show/hide when toggling the menu.")]
        [SerializeField] private GameObject _menuPanel;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            _mobileButton.onClick.AddListener(OnMobileClicked);
            _keyboardMouseButton.onClick.AddListener(OnKeyboardMouseClicked);
            _gamepadButton.onClick.AddListener(OnGamepadClicked);

            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnSchemeChanged.AddListener(RefreshButtonStates);
                RefreshButtonStates(InputManager.Instance.CurrentScheme);
            }
        }

        private void OnDisable()
        {
            _mobileButton.onClick.RemoveListener(OnMobileClicked);
            _keyboardMouseButton.onClick.RemoveListener(OnKeyboardMouseClicked);
            _gamepadButton.onClick.RemoveListener(OnGamepadClicked);

            if (InputManager.Instance != null)
                InputManager.Instance.OnSchemeChanged.RemoveListener(RefreshButtonStates);
        }

        #endregion

        #region Public Methods

        /// <summary>Opens or closes the scheme menu panel.</summary>
        public void ToggleMenu()
        {
            if (_menuPanel != null)
                _menuPanel.SetActive(!_menuPanel.activeSelf);
        }

        /// <summary>Forces the menu panel open.</summary>
        public void OpenMenu()
        {
            if (_menuPanel != null)
                _menuPanel.SetActive(true);
        }

        /// <summary>Forces the menu panel closed.</summary>
        public void CloseMenu()
        {
            if (_menuPanel != null)
                _menuPanel.SetActive(false);
        }

        #endregion

        #region Private Methods

        private void OnMobileClicked()
        {
            InputManager.Instance?.SetSchemeMobile();
            CloseMenu();
        }

        private void OnKeyboardMouseClicked()
        {
            InputManager.Instance?.SetSchemeKeyboardMouse();
            CloseMenu();
        }

        private void OnGamepadClicked()
        {
            InputManager.Instance?.SetSchemeGamepad();
            CloseMenu();
        }

        private void RefreshButtonStates(InputManager.ControlScheme scheme)
        {
            SetButtonColor(_mobileButton, scheme == InputManager.ControlScheme.Mobile);
            SetButtonColor(_keyboardMouseButton, scheme == InputManager.ControlScheme.KeyboardMouse);
            SetButtonColor(_gamepadButton, scheme == InputManager.ControlScheme.Gamepad);
        }

        private void SetButtonColor(Button button, bool isActive)
        {
            if (button == null) return;

            Image img = button.GetComponent<Image>();
            if (img != null)
                img.color = isActive ? _activeColor : _inactiveColor;
        }

        #endregion
    }
}