using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Core.Services;

namespace GlimmerOfHope.UI.Widgets
{
    public class CharacterCreatorUI : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Events")]
        [SerializeField] private StringEventChannel _onPartChanged;

        [Header("Category Navigation")]
        [SerializeField] private Transform _categoryTabContainer;
        [SerializeField] private GameObject _categoryTabPrefab;

        [Header("Parts Grid")]
        [SerializeField] private Transform _partsGridContainer;
        [SerializeField] private GameObject _partButtonPrefab;

        [Header("Actions")]
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _resetButton;
        #endregion

        #region Private Fields
        private GlimmerOfHope.Gameplay.Characters.CharacterCreatorController _controller;
        private string _activeCategoryId;

        private readonly List<GameObject> _spawnedTabs = new();
        private readonly List<GameObject> _spawnedButtons = new();
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            _controller = ServiceLocator.Get<GlimmerOfHope.Gameplay.Characters.CharacterCreatorController>();

            BuildCategoryTabs();
            BindButtons();

            // Afficher la première catégorie par défaut
            if (_controller.Registry.Categories.Count > 0)
                ShowCategory(_controller.Registry.Categories[0].CategoryID);
        }

        private void OnEnable()
        {
            _onPartChanged.Subscribe(OnPartChanged);
        }

        private void OnDisable()
        {
            _onPartChanged.Unsubscribe(OnPartChanged);
        }
        #endregion

        #region Private Methods
        private void BuildCategoryTabs()
        {
            ClearList(_spawnedTabs, _categoryTabContainer);

            foreach (var category in _controller.Registry.Categories)
            {
                if (category == null)
                    continue;

                var tab = Instantiate(_categoryTabPrefab, _categoryTabContainer);

                // Label
                var label = tab.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = category.DisplayName;

                // Icône
                var icon = tab.GetComponentInChildren<Image>();
                if (icon != null && category.CategoryIcon != null)
                    icon.sprite = category.CategoryIcon;

                // Click
                var btn = tab.GetComponent<Button>();
                var captured = category.CategoryID;
                btn?.onClick.AddListener(() => ShowCategory(captured));

                _spawnedTabs.Add(tab);
            }
        }

        private void ShowCategory(string categoryId)
        {
            _activeCategoryId = categoryId;
            ClearList(_spawnedButtons, _partsGridContainer);

            var category = _controller.Registry.GetCategoryById(categoryId);
            if (category == null)
                return;

            foreach (var part in category.Parts)
            {
                if (part == null)
                    continue;

                var btn = Instantiate(_partButtonPrefab, _partsGridContainer);

                // Thumbnail
                var img = btn.GetComponentInChildren<Image>();
                if (img != null && part.Thumbnail != null)
                    img.sprite = part.Thumbnail;

                // Label
                var label = btn.GetComponentInChildren<Text>();
                if (label != null)
                    label.text = part.DisplayName;

                // Click
                var capturedCat = categoryId;
                var capturedPart = part.PartID;
                btn.GetComponent<Button>()?.onClick.AddListener(
                    () => _controller.SelectPart(capturedCat, capturedPart)
                );

                _spawnedButtons.Add(btn);
            }
        }

        private void BindButtons()
        {
            _confirmButton?.onClick.AddListener(OnConfirm);
            _resetButton?.onClick.AddListener(OnReset);
        }

        private void OnPartChanged(string categoryId)
        {
            // Ici : mettre à jour visuellement le bouton actif si nécessaire
            // (outline de sélection, highlight, etc.)
        }

        private void OnConfirm()
        {
            // F4 — à brancher sur SaveManager quand activé
            Debug.Log("[CharacterCreatorUI] Personnage confirmé.");
        }

        private void OnReset()
        {
            _controller.ResetToDefaults();

            if (!string.IsNullOrEmpty(_activeCategoryId))
                ShowCategory(_activeCategoryId);
        }

        private void ClearList(List<GameObject> list, Transform container)
        {
            foreach (var go in list)
            {
                if (go != null)
                    Destroy(go);
            }
            list.Clear();
        }
        #endregion
    }
}
