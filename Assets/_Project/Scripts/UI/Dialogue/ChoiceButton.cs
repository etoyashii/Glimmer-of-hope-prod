using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GlimmerOfHope.UI.Dialogue
{
    [RequireComponent(typeof(Button))]
    public class ChoiceButton : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private TMP_Text _label;
        [SerializeField] private Image _highlight;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _selectedColor = Color.yellow;

        #endregion

        #region Private Fields

        private Button _button;
        private int _index;
        private Action<int> _onClick;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(HandleClick);
        }

        #endregion

        #region Public Methods

        public void Setup(int index, string text, Action<int> onClick)
        {
            _index = index;
            _onClick = onClick;

            if (_label != null)
                _label.text = text;

            Deselect();
        }

        public void Select()
        {
            if (_highlight != null)
                _highlight.color = _selectedColor;

            if (_label != null)
                _label.color = _selectedColor;
        }

        public void Deselect()
        {
            if (_highlight != null)
                _highlight.color = _normalColor;

            if (_label != null)
                _label.color = _normalColor;
        }

        #endregion

        #region Private Methods

        private void HandleClick()
        {
            _onClick?.Invoke(_index);
        }

        #endregion
    }
}
