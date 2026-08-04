using System;
using UnityEngine;
using UnityEngine.UI;

namespace GlimmerOfHope.UI.BookMenu
{
    #region Dependencies

    [RequireComponent(typeof(Button))]

    #endregion
    public class BookmarkTab : MonoBehaviour
    {
        #region Private Fields

        [Header("References")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _background;
        [SerializeField] private Button _button;

        [Header("Active / Inactive Colors")]
        [SerializeField] private Color _activeColor = new Color(0.79f, 0.31f, 0.24f);
        [SerializeField] private Color _inactiveColor = new Color(0.54f, 0.18f, 0.13f);

        [Header("Active / Inactive Height")]
        [SerializeField] private float _activeHeight = 46f;
        [SerializeField] private float _inactiveHeight = 34f;

        private Action _onClick;

        #endregion

        #region Public Methods

        public void Setup(Sprite icon, string label, Action clickCallback)
        {
            if (_iconImage != null) _iconImage.sprite = icon;
            gameObject.name = "Bookmark_" + label;
            _onClick = clickCallback;

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => _onClick?.Invoke());
        }

        public void SetActiveState(bool isActive)
        {
            if (_background != null)
                _background.color = isActive ? _activeColor : _inactiveColor;

            var rectTransform = (RectTransform)transform;
            var size = rectTransform.sizeDelta;
            size.y = isActive ? _activeHeight : _inactiveHeight;
            rectTransform.sizeDelta = size;
        }

        #endregion
    }
}