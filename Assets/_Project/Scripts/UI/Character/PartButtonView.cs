using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GlimmerOfHope.Gameplay.Characters;

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
        [SerializeField] private GameObject _selectionIndicator;
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

            SetSelected(false);

            if (_button != null)
                _button.onClick.AddListener(OnClick);
        }

        public void SetSelected(bool selected)
        {
            if (_selectionIndicator != null)
                _selectionIndicator.SetActive(selected);
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
