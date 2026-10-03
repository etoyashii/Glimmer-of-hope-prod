using UnityEngine;
using UnityEngine.UI;
using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Core.Services;

namespace GlimmerOfHope.UI.Widgets
{
    [RequireComponent(typeof(Button))]
    public class CharacterResetButton : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Event")]
        [Tooltip("Le channel de catégorie, pour rafraîchir la grille après reset.")]
        [SerializeField] private StringEventChannel _onCategorySelected;
        [SerializeField] private StringEventChannel _onPartChanged;
        #endregion

        #region Private Fields
        private Button _button;
        private GlimmerOfHope.Gameplay.Characters.CharacterCreatorController _controller;
        private string _lastCategoryId;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void Start()
        {
            _controller = ServiceLocator.Get<GlimmerOfHope.Gameplay.Characters.CharacterCreatorController>();
            _button.onClick.AddListener(OnClick);
        }

        private void OnEnable()
        {
            if (_onCategorySelected != null)
                _onCategorySelected.Subscribe(RememberCategory);
        }

        private void OnDisable()
        {
            if (_onCategorySelected != null)
                _onCategorySelected.Unsubscribe(RememberCategory);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(OnClick);
        }
        #endregion

        #region Private Methods
        private void RememberCategory(string categoryId)
        {
            _lastCategoryId = categoryId;
        }

        private void OnClick()
        {
            if (_controller == null) return;

            _controller.ResetToDefaults();

            if (_onPartChanged != null)
            {
                foreach (var category in _controller.Registry.GetAllLeafCategories())
                {
                    if (category != null)
                        _onPartChanged.Raise(category.CategoryID);
                }
            }

            var categoryId = _lastCategoryId;
            if (string.IsNullOrEmpty(categoryId) && _controller.Registry.Categories.Count > 0)
                categoryId = _controller.Registry.Categories[0].CategoryID;

            if (_onCategorySelected != null && !string.IsNullOrEmpty(categoryId))
                _onCategorySelected.Raise(categoryId);

            Debug.Log("[CharacterResetButton] Personnage réinitialisé.");
        }
        #endregion
    }
}
