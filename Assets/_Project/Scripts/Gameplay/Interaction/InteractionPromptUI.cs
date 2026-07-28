using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GlimmerOfHope.Gameplay.Interaction
{
    /// <summary>
    /// Screen space prompt following the currently focused Interactable.
    /// Shows an input hint on KeyboardMouse and Gamepad, and a tappable
    /// button on Mobile, matching the direct UI call pattern used by Jump.
    /// </summary>
    public class InteractionPromptUI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [SerializeField] private RectTransform _root;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Camera _playerCamera;

        [Header("Desktop and Gamepad Prompt")]
        [SerializeField] private GameObject _desktopPromptGroup;
        [SerializeField] private TMP_Text _inputHintText;
        [SerializeField] private TMP_Text _labelText;

        [Header("Mobile Prompt")]
        [SerializeField] private GameObject _mobilePromptGroup;
        [SerializeField] private Button _mobileTapButton;
        [SerializeField] private TMP_Text _mobileLabelText;

        #endregion

        #region Private Fields

        private Interactable _target;
        private System.Action _onTapCallback;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _mobileTapButton.onClick.AddListener(OnMobileTapButtonClicked);
            Hide();
        }

        private void OnEnable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnSchemeChanged.AddListener(OnSchemeChanged);
                RefreshSchemeVisuals(InputManager.Instance.CurrentScheme);
            }
        }

        private void OnDisable()
        {
            if (InputManager.Instance != null)
                InputManager.Instance.OnSchemeChanged.RemoveListener(OnSchemeChanged);
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector2 screenPoint = _playerCamera.WorldToScreenPoint(_target.PromptAnchor);
            _root.anchoredPosition = ScreenToCanvasPoint(screenPoint);
        }

        #endregion

        #region Public Methods

        public void Show(Interactable target, System.Action onTapCallback)
        {
            _target = target;
            _onTapCallback = onTapCallback;

            _labelText.text = target.PromptLabel;
            _mobileLabelText.text = target.PromptLabel;

            _root.gameObject.SetActive(true);
        }

        public void Hide()
        {
            _target = null;
            _onTapCallback = null;
            _root.gameObject.SetActive(false);
        }

        #endregion

        #region Private Methods

        private void OnMobileTapButtonClicked()
        {
            _onTapCallback?.Invoke();
        }

        private void OnSchemeChanged(InputManager.ControlScheme scheme)
        {
            RefreshSchemeVisuals(scheme);
        }

        private void RefreshSchemeVisuals(InputManager.ControlScheme scheme)
        {
            bool isMobile = scheme == InputManager.ControlScheme.Mobile;

            _desktopPromptGroup.SetActive(!isMobile);
            _mobilePromptGroup.SetActive(isMobile);

            if (!isMobile)
                _inputHintText.text = scheme == InputManager.ControlScheme.Gamepad ? "Button" : "E";
        }

        private Vector2 ScreenToCanvasPoint(Vector2 screenPos)
        {
            Camera cam = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _canvas.worldCamera;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _root.parent as RectTransform,
                screenPos,
                cam,
                out Vector2 localPoint
            );
            return localPoint;
        }

        #endregion
    }
}