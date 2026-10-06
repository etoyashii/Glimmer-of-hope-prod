using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace GlimmerOfHope.UI.Widgets
{
    [RequireComponent(typeof(Button))]
    public class CharacterMenuToggle : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private float _subMenuShowAnimTime = 0.2f;
        [SerializeField] private Ease _subMenuShowAnimEase = Ease.OutBack;
        [SerializeField] private float _subMenuHideAnimTime = 0.2f;
        [SerializeField] private Ease _subMenuHideAnimEase = Ease.InBack;
        [Header("References")]
        [SerializeField] private RectTransform _subMenuRectTransform;
        [SerializeField] private GameObject _subMenu;
        [SerializeField] private GameObject[] _otherSubMenus;
        [SerializeField] private bool _openOnStart;

        private Button _button;
        private bool _isOpen = false;
        private Vector2? _defaultSizeDelta = null;
        private Tween _currentTween;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(Toggle);
        }

        private void Start()
        {
            if (_openOnStart)
                SetOpen(true);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(Toggle);
        }

        private void Toggle()
        {
            if (_subMenu == null)
                return;

            SetOpen(!_subMenu.activeSelf);
        }

        public void SetOpen(bool open)
        {
            foreach (var menu in _otherSubMenus)
            {
                if (menu != null)
                    menu.SetActive(false);
            }

            if (_subMenu != null)
            {
                if (open)
                    ShowSubMenu();
                else
                    HideSubMenu();
            }
        }

        public void ShowSubMenu()
        {
            if (_isOpen == true)
                return;
            _isOpen = true;

            if (_defaultSizeDelta == null)
                _defaultSizeDelta = _subMenuRectTransform.sizeDelta;

            if (_currentTween.IsActive())
                _currentTween.Kill();

            _subMenu.gameObject.SetActive(true);
            _subMenuRectTransform.sizeDelta = new Vector2(_defaultSizeDelta.Value.x, 0f);
            _currentTween = SizeDeltaTween(_subMenuRectTransform, _defaultSizeDelta.Value, _subMenuShowAnimTime, _subMenuShowAnimEase);
        }

        public void HideSubMenu()
        {
            if (_isOpen == false)
                return;
            _isOpen = false;

            if (_defaultSizeDelta == null)
                _defaultSizeDelta = _subMenuRectTransform.sizeDelta;

            if (_currentTween.IsActive())
                _currentTween.Kill();
            _currentTween = SizeDeltaTween(_subMenuRectTransform, new Vector2(_defaultSizeDelta.Value.x, 0f), _subMenuHideAnimTime, _subMenuHideAnimEase)
                .OnComplete(() => _subMenu.SetActive(false));
        }

        private Tween SizeDeltaTween(RectTransform target, Vector2 targetSize, float duration, Ease ease)
        {
            return DOTween.To(
                () => target.sizeDelta,
                x => target.sizeDelta = x,
                targetSize,
                duration
            ).SetEase(ease);
        }
    }
}
