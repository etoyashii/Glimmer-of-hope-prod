using System.Collections.Generic;
using UnityEngine;
using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Core.Services;
using GlimmerOfHope.Gameplay.Characters;

namespace GlimmerOfHope.UI.Widgets
{
    // Affiche des sous-onglets quand la categorie selectionnee a des sous-categories.
    // Utilise le meme StringEventChannel que les onglets principaux :
    //   - Ecoute les IDs de categories parentes pour construire les sous-onglets.
    //   - Raise l'ID de la sous-categorie selectionnee (CharacterPartsGridController s'en charge).
    //   - Ignore les IDs de sous-categories pour eviter la boucle (GetParentCategory != null).
    public class CharacterSubCategoryTabsController : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Prefab")]
        [Tooltip("Meme prefab que les onglets principaux (CategoryTabView requis).")]
        [SerializeField] private GameObject _subTabPrefab;

        [Header("Container")]
        [Tooltip("GameObject parent des sous-onglets. Active/desactive selon la categorie active.")]
        [SerializeField] private GameObject _container;

        [Header("Events")]
        [Tooltip("Ecoute la selection de categorie parente et raise la sous-categorie selectionnee.")]
        [SerializeField] private StringEventChannel _onCategorySelected;
        #endregion

        #region Private Fields
        private CharacterCreatorController _controller;
        private readonly List<CategoryTabView> _spawnedViews = new();
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            _onCategorySelected?.Subscribe(OnCategorySelected);
        }

        private void OnDisable()
        {
            _onCategorySelected?.Unsubscribe(OnCategorySelected);
        }

        private void Start()
        {
            _controller = ServiceLocator.Get<CharacterCreatorController>();
            SetContainerVisible(false);
        }
        #endregion

        #region Event Handlers
        private void OnCategorySelected(string categoryId)
        {
            if (_controller == null) return;

            // Si c'est une sous-categorie qui raise l'event, ignorer pour eviter la boucle.
            if (_controller.Registry.GetParentCategory(categoryId) != null) return;

            var category = _controller.Registry.GetCategoryById(categoryId);
            if (category == null || !category.HasSubCategories)
            {
                SetContainerVisible(false);
                ClearTabs();
                return;
            }

            BuildSubTabs(category);
            SetContainerVisible(true);

            if (category.SubCategories.Count > 0 && category.SubCategories[0] != null)
                SelectSubCategory(category.SubCategories[0].CategoryID);
        }
        #endregion

        #region Private Methods
        private void BuildSubTabs(CharacterCategorySO parentCategory)
        {
            ClearTabs();

            var parent = _container != null ? _container.transform : transform;
            foreach (var sub in parentCategory.SubCategories)
            {
                if (sub == null) continue;

                var tab  = Instantiate(_subTabPrefab, parent);
                var view = tab.GetComponent<CategoryTabView>();
                if (view != null)
                {
                    view.Setup(sub, SelectSubCategory);
                    _spawnedViews.Add(view);
                }
                else
                {
                    Debug.LogWarning("[CharacterSubCategoryTabsController] CategoryTabView absent du prefab.", tab);
                }
            }
        }

        private void SelectSubCategory(string subCategoryId)
        {
            foreach (var view in _spawnedViews)
                view.SetActive(view.CategoryId == subCategoryId);

            _onCategorySelected?.Raise(subCategoryId);
        }

        private void SetContainerVisible(bool visible)
        {
            if (_container != null) _container.SetActive(visible);
        }

        private void ClearTabs()
        {
            foreach (var view in _spawnedViews)
                if (view != null) Destroy(view.gameObject);
            _spawnedViews.Clear();
        }
        #endregion
    }
}
