using UnityEngine;
using TMPro;
using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Core.Services;

namespace GlimmerOfHope.UI.Widgets
{
    [RequireComponent(typeof(TMP_Text))]
    public class CharacterCategoryLabel : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Event")]
        [SerializeField] private StringEventChannel _onCategorySelected;

        [Header("Options")]
        [SerializeField] private bool   _uppercase = true;
        [SerializeField] private string _prefix    = "";
        #endregion

        #region Private Fields
        private TMP_Text _text;
        private GlimmerOfHope.Gameplay.Characters.CharacterCreatorController _controller;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void Start()
        {
            _controller = ServiceLocator.Get<GlimmerOfHope.Gameplay.Characters.CharacterCreatorController>();

            if (_controller != null && _controller.Registry.Categories.Count > 0)
            {
                var first = _controller.Registry.Categories[0];
                if (first != null)
                    ApplyLabel(first.CategoryID);
            }
        }

        private void OnEnable()
        {
            if (_onCategorySelected != null)
                _onCategorySelected.Subscribe(ApplyLabel);
        }

        private void OnDisable()
        {
            if (_onCategorySelected != null)
                _onCategorySelected.Unsubscribe(ApplyLabel);
        }
        #endregion

        #region Private Methods
        private void ApplyLabel(string categoryId)
        {
            if (_controller == null || _text == null) return;

            var category = _controller.Registry.GetCategoryById(categoryId);
            if (category == null) return;

            var displayName = _prefix + category.DisplayName;
            _text.text = _uppercase ? displayName.ToUpper() : displayName;
        }
        #endregion
    }
}
