using System.Collections.Generic;
using UnityEngine;
using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Core.Services;
using GlimmerOfHope.Gameplay.Characters;

namespace GlimmerOfHope.UI.Widgets
{
    // Gere la sidebar verticale accordeon.
    // Spawne les items depuis le Registry et orchestre expand/collapse + selection.
    public class CategorySidebarController : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Prefabs")]
        [Tooltip("Item avec sous-panel accordeon (categories parentes).")]
        [SerializeField] private GameObject _parentItemPrefab;
        [Tooltip("Item simple sans accordeon (categories feuilles et sous-categories).")]
        [SerializeField] private GameObject _leafItemPrefab;

        [Header("Config")]
        [Tooltip("Hauteur en pixels de chaque sous-item (used pour calculer la hauteur expandee).")]
        [SerializeField] private float _subItemHeight = 56f;

        [Header("Events")]
        [Tooltip("Raised quand une categorie (ou sous-categorie) est selectionnee.")]
        [SerializeField] private StringEventChannel _onCategorySelected;
        #endregion

        #region Private Fields
        private CharacterCreatorController _controller;
        private readonly List<CategorySidebarItemView> _topLevelItems = new();
        private readonly Dictionary<string, CategorySidebarItemView> _viewById = new();
        private string _expandedCategoryId;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            _controller = ServiceLocator.Get<CharacterCreatorController>();
            if (_controller == null)
            {
                Debug.LogError("[CategorySidebarController] CharacterCreatorController introuvable.");
                return;
            }

            BuildSidebar();
        }
        #endregion

        #region Build
        private void BuildSidebar()
        {
            foreach (var item in _topLevelItems)
                if (item != null) Destroy(item.gameObject);
            _topLevelItems.Clear();
            _viewById.Clear();

            foreach (var category in _controller.Registry.Categories)
            {
                if (category == null) continue;

                bool   hasSubs  = category.HasSubCategories;
                var    prefab   = hasSubs ? _parentItemPrefab : _leafItemPrefab;
                if (prefab == null) continue;

                var go   = Instantiate(prefab, transform);
                var view = go.GetComponent<CategorySidebarItemView>();
                if (view == null) continue;

                float expandH = hasSubs ? category.SubCategories.Count * _subItemHeight : 0f;
                view.Setup(category, OnItemClicked, expandH);

                if (hasSubs) PopulateSubItems(view, category);

                _topLevelItems.Add(view);
                _viewById[category.CategoryID] = view;
            }
        }

        private void PopulateSubItems(CategorySidebarItemView parentView, CharacterCategorySO parentCat)
        {
            var content = parentView.SubItemsContent;
            if (content == null || _leafItemPrefab == null) return;

            foreach (var sub in parentCat.SubCategories)
            {
                if (sub == null) continue;

                var go   = Instantiate(_leafItemPrefab, content);
                var view = go.GetComponent<CategorySidebarItemView>();
                if (view == null) continue;

                view.Setup(sub, OnItemClicked, 0f);

                // Force la hauteur des sous-items a _subItemHeight
                var le = go.GetComponent<UnityEngine.UI.LayoutElement>();
                if (le != null)
                {
                    le.preferredHeight = _subItemHeight;
                    le.minHeight       = _subItemHeight;
                }

                _viewById[sub.CategoryID] = view;
            }
        }
        #endregion

        #region Click Handling
        private void OnItemClicked(string categoryId)
        {
            if (_controller == null) return;

            var category = _controller.Registry.GetCategoryById(categoryId);
            if (category == null) return;

            bool isParent = category.HasSubCategories;

            // Trouve le parent de la categorie cliquee (null si top-level)
            var parentCat = _controller.Registry.GetParentCategory(categoryId);
            string parentId = parentCat?.CategoryID;

            // Mise a jour de la selection : item clique + son parent (si sous-categorie)
            foreach (var kvp in _viewById)
            {
                bool selected = kvp.Key == categoryId
                    || (parentId != null && kvp.Key == parentId);
                kvp.Value.SetSelected(selected);
            }

            if (isParent)
            {
                bool willExpand = _expandedCategoryId != categoryId;

                // Collapse l'ancien parent ouvert
                if (!string.IsNullOrEmpty(_expandedCategoryId) && _expandedCategoryId != categoryId)
                {
                    if (_viewById.TryGetValue(_expandedCategoryId, out var prev))
                        prev.SetExpanded(false);
                }

                _expandedCategoryId = willExpand ? categoryId : null;

                if (_viewById.TryGetValue(categoryId, out var cur))
                    cur.SetExpanded(willExpand);

            }
            else
            {
                _onCategorySelected?.Raise(categoryId);
            }
        }
        #endregion
    }
}
