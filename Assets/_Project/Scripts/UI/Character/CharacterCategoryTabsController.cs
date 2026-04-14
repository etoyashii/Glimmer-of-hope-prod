using System.Collections.Generic;
using UnityEngine;
using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Core.Services;

namespace GlimmerOfHope.UI.Widgets
{
    public class CharacterCategoryTabsController : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Prefab")]
        [Tooltip("Prefab d'onglet. Doit avoir CategoryTabView attaché à sa racine.")]
        [SerializeField] private GameObject _categoryTabPrefab;

        [Header("Events")]
        [Tooltip("Fired quand l'utilisateur clique sur un onglet (payload = categoryId).")]
        [SerializeField] private StringEventChannel _onCategorySelected;
        #endregion

        #region Private Fields
        private GlimmerOfHope.Gameplay.Characters.CharacterCreatorController _controller;
        private readonly List<CategoryTabView> _spawnedViews = new();
        private string _activeCategoryId;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            _controller = ServiceLocator.Get<GlimmerOfHope.Gameplay.Characters.CharacterCreatorController>();

            if (_controller == null)
            {
                Debug.LogError("[CharacterCategoryTabsController] CharacterCreatorController introuvable.");
                return;
            }

            if (_categoryTabPrefab == null)
            {
                Debug.LogError("[CharacterCategoryTabsController] _categoryTabPrefab non assigné.", this);
                return;
            }

            BuildTabs();

            if (_controller.Registry.Categories.Count > 0)
                SelectCategory(_controller.Registry.Categories[0].CategoryID);
        }
        #endregion

        #region Private Methods
        private void BuildTabs()
        {
            ClearTabs();

            foreach (var category in _controller.Registry.Categories)
            {
                if (category == null) continue;

                var tab = Instantiate(_categoryTabPrefab, transform);

                var view = tab.GetComponent<CategoryTabView>();
                if (view != null)
                {
                    view.Setup(category, SelectCategory);
                    _spawnedViews.Add(view);
                }
                else
                {
                    Debug.LogWarning($"[CharacterCategoryTabsController] CategoryTabView absent du prefab.", tab);
                }
            }
        }

        private void SelectCategory(string categoryId)
        {
            _activeCategoryId = categoryId;

            foreach (var view in _spawnedViews)
                view.SetActive(view.CategoryId == categoryId);

            _onCategorySelected?.Raise(categoryId);
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
