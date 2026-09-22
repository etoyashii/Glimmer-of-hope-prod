using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GlimmerOfHope.Gameplay.Characters;

namespace GlimmerOfHope.UI.Widgets
{
    // Un onglet dans la sidebar verticale.
    // Version "parent" : possede un sous-panel qui s'expand via DOTween.
    // Version "feuille" : _subItemsLayoutElement null -> HasSubItems = false.
    public class CategorySidebarItemView : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _iconImage;
        [SerializeField] private GameObject _selectionHighlight;

        [Header("Accordeon (parent uniquement)")]
        [Tooltip("LayoutElement sur le panel de sous-items. Null si feuille.")]
        [SerializeField] private LayoutElement _subItemsLayoutElement;
        [Tooltip("Transform parent des sous-items spawnes a la runtime.")]
        [SerializeField] private Transform _subItemsContent;

        [Header("Placeholder (sans sprite)")]
        [Tooltip("TextMeshPro dans le container d'icone. Affiche l'initiale de la categorie si aucun sprite n'est assigne.")]
        [SerializeField] private TMP_Text _iconLabel;

        [Header("Config")]
        [SerializeField] private float _animDuration = 0.22f;
        #endregion

        #region Private Fields
        private string _categoryId;
        private System.Action<string> _onClickCallback;
        private float _expandedHeight;
        private Tween _expandTween;
        #endregion

        #region Public Properties
        public string CategoryId   => _categoryId;
        public bool HasSubItems    => _subItemsLayoutElement != null;
        public Transform SubItemsContent => _subItemsContent;
        #endregion

        #region Public API
        public void Setup(
            CharacterCategorySO category,
            System.Action<string> onClick,
            float expandedHeight = 0f)
        {
            _categoryId      = category.CategoryID;
            _onClickCallback = onClick;
            _expandedHeight  = expandedHeight;

            if (_iconImage != null)
            {
                if (category.CategoryIcon != null)
                {
                    _iconImage.sprite = category.CategoryIcon;
                    _iconImage.color  = Color.white;
                    if (_iconLabel != null) _iconLabel.gameObject.SetActive(false);
                }
                else
                {
                    _iconImage.sprite = null;
                    _iconImage.color  = Color.clear;
                    if (_iconLabel != null)
                    {
                        _iconLabel.gameObject.SetActive(true);
                        var displayName = category.DisplayName ?? category.CategoryID;
                        _iconLabel.text = displayName.Length > 0
                            ? displayName.Substring(0, 1).ToUpper()
                            : "?";
                    }
                }
            }

            SetSelected(false);

            if (HasSubItems)
            {
                _subItemsLayoutElement.preferredHeight = 0f;
                _subItemsLayoutElement.minHeight       = 0f;
                if (_subItemsContent != null)
                    _subItemsContent.gameObject.SetActive(false);
            }

            _button?.onClick.AddListener(OnClick);
        }

        public void SetSelected(bool selected)
        {
            if (_selectionHighlight != null)
                _selectionHighlight.SetActive(selected);
        }

        public void SetExpanded(bool expanded, bool animate = true)
        {
            if (!HasSubItems) return;

            _expandTween?.Kill();

            if (expanded && _subItemsContent != null)
                _subItemsContent.gameObject.SetActive(true);

            float target = expanded ? _expandedHeight : 0f;

            if (animate)
            {
                _expandTween = DOTween
                    .To(
                        () => _subItemsLayoutElement.preferredHeight,
                        h  => _subItemsLayoutElement.preferredHeight = h,
                        target,
                        _animDuration)
                    .SetEase(Ease.OutCubic)
                    .OnComplete(() =>
                    {
                        if (!expanded && _subItemsContent != null)
                            _subItemsContent.gameObject.SetActive(false);
                    });
            }
            else
            {
                _subItemsLayoutElement.preferredHeight = target;
                if (!expanded && _subItemsContent != null)
                    _subItemsContent.gameObject.SetActive(false);
            }
        }
        #endregion

        #region Private Methods
        private void OnClick() => _onClickCallback?.Invoke(_categoryId);

        private void OnDestroy()
        {
            _expandTween?.Kill();
            _button?.onClick.RemoveListener(OnClick);
        }
        #endregion
    }
}
