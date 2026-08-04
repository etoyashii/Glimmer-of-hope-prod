using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GlimmerOfHope.UI.BookMenu.Panels
{
    public class CyclerControl : MonoBehaviour
    {
        #region Private Fields

        [SerializeField] private TMP_Text _valueLabel;
        [SerializeField] private Button _leftButton;
        [SerializeField] private Button _rightButton;

        private string[] _options;
        private int _index;
        private Action<string, int> _onChange;

        #endregion

        #region Public Methods

        public void Setup(string[] options, int startIndex, Action<string, int> onChange)
        {
            _options = options;
            _index = Mathf.Clamp(startIndex, 0, options.Length - 1);
            _onChange = onChange;
            UpdateLabel();

            _leftButton.onClick.RemoveAllListeners();
            _rightButton.onClick.RemoveAllListeners();
            _leftButton.onClick.AddListener(() => Move(-1));
            _rightButton.onClick.AddListener(() => Move(1));
        }

        #endregion

        #region Private Methods

        private void Move(int direction)
        {
            _index = (_index + direction + _options.Length) % _options.Length;
            UpdateLabel();
            _onChange?.Invoke(_options[_index], _index);
        }

        private void UpdateLabel()
        {
            if (_valueLabel != null) _valueLabel.text = _options[_index];
        }

        #endregion
    }
}