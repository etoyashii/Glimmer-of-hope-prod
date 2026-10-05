using GlimmerOfHope.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay.Character.SpecialActions
{
    public class ViewEmotionInput : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputActionReference _viewEmotionAction;
        [SerializeField] private GlobalValueShader _shader;

        private void Awake()
        {
            if (_shader == null)
                _shader = FindFirstObjectByType<GlobalValueShader>();

            if (_shader == null)
                Debug.LogWarning("[ViewEmotionInput] No GlobalValueShader found in scene.");
        }

        private void OnEnable()
        {
            _viewEmotionAction.action.Enable();
            _viewEmotionAction.action.performed += OnViewEmotion;

            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnSchemeChanged.AddListener(OnSchemeChanged);
                ApplyBindingMask(InputManager.Instance.CurrentScheme);
            }
        }

        private void OnDisable()
        {
            _viewEmotionAction.action.performed -= OnViewEmotion;
            _viewEmotionAction.action.Disable();

            if (InputManager.Instance != null)
                InputManager.Instance.OnSchemeChanged.RemoveListener(OnSchemeChanged);
        }

        public void OnViewEmotionButton() => Toggle();

        private void OnViewEmotion(InputAction.CallbackContext ctx) => Toggle();

        private void Toggle()
        {
            if (_shader != null)
                _shader.SwitchViewEmotionMode();
        }

        private void OnSchemeChanged(InputManager.ControlScheme scheme) => ApplyBindingMask(scheme);

        private void ApplyBindingMask(InputManager.ControlScheme scheme)
        {
            if (scheme == InputManager.ControlScheme.Mobile)
            {
                _viewEmotionAction.action.Disable();
                return;
            }

            _viewEmotionAction.action.bindingMask = scheme switch
            {
                InputManager.ControlScheme.KeyboardMouse => InputBinding.MaskByGroup("Keyboard/Mouse"),
                InputManager.ControlScheme.Gamepad => InputBinding.MaskByGroup("Gamepad"),
                _ => null
            };
            _viewEmotionAction.action.Enable();
        }
    }
}