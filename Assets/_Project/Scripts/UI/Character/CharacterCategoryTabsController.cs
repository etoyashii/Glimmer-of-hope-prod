using System.Collections.Generic;
using UnityEngine;
using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Core.Services;

namespace GlimmerOfHope.UI.Widgets
{
    /// <summary>
    /// Gère l'affichage et la sélection des onglets de catégories.
    ///
    /// Setup designer :
    ///   1. Attacher ce script sur le container des onglets (ex: CategoryContent).
    ///   2. Assigner _categoryTabPrefab (doit avoir CategoryTabView attaché).
    ///   3. Assigner _onCategorySelected (StringEventChannel).
    ///
    /// Ce script est indépendant de CharacterPartsGridController —
    /// ils communiquent uniquement via l'event channel.
    /// </summary>
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
        private readonly List<GameObject> _spawnedTabs = new();

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

            // Sélectionner la première catégorie par défaut
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
                    view.Setup(category, SelectCategory);
                else
                    Debug.LogWarning($"[CharacterCategoryTabsController] CategoryTabView absent du prefab '{_categoryTabPrefab.name}'.", tab);

                _spawnedTabs.Add(tab);
            }
        }

        private void SelectCategory(string categoryId)
        {
            _onCategorySelected?.Raise(categoryId);
        }

        private void ClearTabs()
        {
            foreach (var go in _spawnedTabs)
                if (go != null) Destroy(go);
            _spawnedTabs.Clear();
        }

        #endregion
    }
}
