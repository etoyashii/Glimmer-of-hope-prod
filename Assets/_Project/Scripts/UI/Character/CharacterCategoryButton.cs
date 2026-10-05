using UnityEngine;
using UnityEngine.UI;
using GlimmerOfHope.Core.Events;

namespace GlimmerOfHope.UI.Widgets
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Button))]
    public class CharacterCategoryButton : MonoBehaviour
    {
        [SerializeField] private string _categoryId;
        [SerializeField] private StringEventChannel _onCategorySelected;
        [SerializeField] private bool _selectOnStart;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(Select);
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
        }

        public void Select()
        {
            if (_onCategorySelected == null || string.IsNullOrEmpty(_categoryId))
                return;

            _onCategorySelected.Raise(_categoryId);
        }
    }
}
