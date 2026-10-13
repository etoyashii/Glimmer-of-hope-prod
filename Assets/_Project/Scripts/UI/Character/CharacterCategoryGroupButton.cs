using System;
using DG.Tweening;
using GlimmerOfHope.Core.Events;
using UnityEngine;
using UnityEngine.UI;

namespace GlimmerOfHope.UI
{
    public partial class CharacterCategoryGroupButton : MonoBehaviour
    {
        public event Action<CharacterCategoryGroups> OnSelect;

        [Header("References")]
        [SerializeField] private Button _button;
        [SerializeField] private RectTransform _subMenuRT;
        [SerializeField] private ContentSizeFitter _subMenuContentSizeFitter;
        [SerializeField] private LayoutGroup _subMenuLayoutGroup;
        [SerializeField] private StringEventChannel _onCategorySelected;
        [Header("Animation")]
        [SerializeField] private float _subMenuShowAnimTime = 0.2f;
        [SerializeField] private Ease _subMenuShowAnimEase = Ease.OutBack;
        [SerializeField] private float _subMenuHideAnimTime = 0.2f;
        [SerializeField] private Ease _subMenuHideAnimEase = Ease.InBack;
        [Header("Settings")]
        [SerializeField] private string _selectCategoryOnClick;
        public CharacterCategoryGroups CategoryGroup;

        private bool _isOpen = false;
        private Vector2? _defaultSizeDelta = null;
        private Tween _currentTween;

        private void Awake()
        {
            _button.onClick.AddListener(OnClick);
            _isOpen = _subMenuRT.gameObject.activeSelf;
        }

        private void OnClick()
        {
            OnSelect.Invoke(CategoryGroup);
            if (_selectCategoryOnClick != null && _selectCategoryOnClick != string.Empty)
                _onCategorySelected.Raise(_selectCategoryOnClick);
        }

        public void ShowSubMenu(bool animate = true)
        {
            if (_isOpen == true || CategoryGroup == CharacterCategoryGroups.Fav)
                return;
            _isOpen = true;
            if (_defaultSizeDelta == null)
                _defaultSizeDelta = GetDefaultSizeDelta();
            _subMenuContentSizeFitter.enabled = false;
            _subMenuLayoutGroup.enabled = false;
            if (!animate)
                return;


            if (_currentTween.IsActive())
                _currentTween.Kill();

            _subMenuRT.gameObject.SetActive(true);
            _subMenuRT.sizeDelta = new Vector2(_defaultSizeDelta.Value.x, 0f);
            _currentTween = SizeDeltaTween(_subMenuRT, _defaultSizeDelta.Value, _subMenuShowAnimTime, _subMenuShowAnimEase);
        }

        public void HideSubMenu(bool animate = false)
        {
            if (_isOpen == false)
                return;
            _isOpen = false;
            if (!animate)
                _subMenuRT.gameObject.SetActive(false);
            if (_defaultSizeDelta == null)
                _defaultSizeDelta = GetDefaultSizeDelta();

            if (_currentTween.IsActive())
                _currentTween.Kill();
            _currentTween = SizeDeltaTween(_subMenuRT, new Vector2(_defaultSizeDelta.Value.x, 0f), _subMenuHideAnimTime, _subMenuHideAnimEase)
                .OnComplete(() => _subMenuRT.gameObject.SetActive(false));
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

        private Vector2 GetDefaultSizeDelta()
        {
            bool wasActive = _subMenuRT.gameObject.activeSelf;
            Vector2 sizeDelta = Vector2.zero;

            _subMenuRT.gameObject.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(_subMenuRT);
            sizeDelta = _subMenuRT.sizeDelta;
            _subMenuRT.gameObject.SetActive(wasActive);

            return sizeDelta;
        }

    }
}