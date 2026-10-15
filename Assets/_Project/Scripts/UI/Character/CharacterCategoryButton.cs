using UnityEngine;
using UnityEngine.UI;
using GlimmerOfHope.Core.Events;
using System;

namespace GlimmerOfHope.UI.Widgets
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Button))]
    public class CharacterCategoryButton : MonoBehaviour
    {
        [SerializeField] private string _categoryId;
        [SerializeField] private StringEventChannel _onCategorySelected;
        [SerializeField] private bool _selectOnStart;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Color _inactiveColor;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(Select);
            _iconImage.color = _inactiveColor;

            _onCategorySelected.Subscribe(OnCategoryChanged);
        }

        private void OnCategoryChanged(string category)
        {
            _iconImage.color = category == _categoryId ? Color.white : _inactiveColor;
        }


        private void Start()
        {
            if (_selectOnStart)
                Select();
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(Select);
            _onCategorySelected.Subscribe(OnCategoryChanged);
        }

        public void Select()
        {
            if (_onCategorySelected == null || string.IsNullOrEmpty(_categoryId))
                return;

            _onCategorySelected.Raise(_categoryId);
        }
    }
}
