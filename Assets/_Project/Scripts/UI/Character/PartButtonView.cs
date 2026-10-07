using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GlimmerOfHope.Gameplay.Characters;
using DG.Tweening;

namespace GlimmerOfHope.UI.Widgets
{
    public class PartButtonView : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Références UI — drag depuis ton prefab")]
        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image    _thumbnail;
        [SerializeField] private Button   _button;

        [Header("Visuel sélection (optionnel)")]
        [Tooltip("GameObject à activer quand cette part est sélectionnée (ex: outline, checkmark).")]
        [SerializeField] private Transform _selectionIndicator;
        [Header("Animation")]
        [SerializeField] private float _selectionShowAnimTime = 0.2f;
        [SerializeField] private Ease _selectionShowAnimEase = Ease.OutBack;
        [SerializeField] private float _selectionHideAnimTime = 0.2f;
        [SerializeField] private Ease _selectionHideAnimEase = Ease.InBack;
        #endregion

        #region Private Fields
        private string _categoryId;
        private string _partId;
        private System.Action<string, string> _onClickCallback;
        #endregion

        #region Public API
        public void Setup(string categoryId, CharacterPartSO part, System.Action<string, string> onClickCallback)
        {
            _categoryId      = categoryId;
            _partId          = part.PartID;
            _onClickCallback = onClickCallback;

            if (_label     != null) _label.text         = part.DisplayName;
            if (_thumbnail != null && part.Thumbnail != null) _thumbnail.sprite = part.Thumbnail;

            SetSelected(false, false);

            if (_button != null)
                _button.onClick.AddListener(OnClick);
        }

        public void SetSelected(bool selected, bool animate = true)
        {
            _selectionIndicator.DOKill();

            if (!animate)
            {
                _selectionIndicator.gameObject.SetActive(selected);
                return;
            }

            if (selected)
            {
                _selectionIndicator.gameObject.SetActive(true);
                _selectionIndicator.DOScale(1f, _selectionShowAnimTime)
                    .SetEase(_selectionShowAnimEase);
            }
            else
            {
                _selectionIndicator.DOScale(0f, _selectionHideAnimTime)
                    .SetEase(_selectionHideAnimEase)
                    .OnComplete(() => _selectionIndicator.gameObject.SetActive(false));
            }
        }

        public string PartId => _partId;
        #endregion

        #region Private Methods
        private void OnClick()
        {
            _onClickCallback?.Invoke(_categoryId, _partId);
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClick);
        }
        #endregion
    }
}
