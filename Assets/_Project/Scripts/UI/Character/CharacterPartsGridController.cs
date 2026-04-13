using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Core.Services;

namespace GlimmerOfHope.UI.Widgets
{
    /// <summary>
    /// Gère l'affichage de la grille de parts.
    /// Se met à jour quand une catégorie est sélectionnée via _onCategorySelected.
    ///
    /// Setup designer :
    ///   1. Attacher ce script sur le container de la grille (ex: PartsContent).
    ///   2. Assigner _partButtonPrefab (doit avoir PartButtonView attaché).
    ///   3. Assigner _onCategorySelected (même channel que CharacterCategoryTabsController).
    ///   4. Assigner _onPartChanged (channel notifiant le preview renderer).
    /// </summary>
    public class CharacterPartsGridController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Prefab")]
        [Tooltip("Prefab de bouton de part. Doit avoir PartButtonView attaché à sa racine.")]
        [SerializeField] private GameObject _partButtonPrefab;

        [Header("Events")]
        [Tooltip("Reçoit le categoryId quand l'utilisateur clique un onglet.")]
        [SerializeField] private StringEventChannel _onCategorySelected;

        [Tooltip("Fired quand l'utilisateur sélectionne une part (payload = categoryId).")]
        [SerializeField] private StringEventChannel _onPartChanged;

        #endregion

        #region Private Fields

        private GlimmerOfHope.Gameplay.Characters.CharacterCreatorController _controller;
        private readonly List<PartButtonView> _spawnedViews = new();
        private string _currentCategoryId;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (_onCategorySelected != null)
                _onCategorySelected.Subscribe(OnCategorySelected);
        }

        private void OnDisable()
        {
            if (_onCategorySelected != null)
                _onCategorySelected.Unsubscribe(OnCategorySelected);
        }

        private void Start()
        {
            _controller = ServiceLocator.Get<GlimmerOfHope.Gameplay.Characters.CharacterCreatorController>();

            if (_controller == null)
                Debug.LogError("[CharacterPartsGridController] CharacterCreatorController introuvable.");

            if (_partButtonPrefab == null)
                Debug.LogError("[CharacterPartsGridController] _partButtonPrefab non assigné.", this);
        }

        #endregion

        #region Event Handlers

        private void OnCategorySelected(string categoryId)
        {
            if (_controller == null || _partButtonPrefab == null) return;
            _currentCategoryId = categoryId;
            PopulateGrid(categoryId);
        }

        #endregion

        #region Grid Population

        private void PopulateGrid(string categoryId)
        {
            ClearGrid();

            var category = _controller.Registry.GetCategoryById(categoryId);
            if (category == null)
            {
                Debug.LogWarning($"[CharacterPartsGridController] Catégorie '{categoryId}' introuvable.");
                return;
            }

            var selectedPartId = _controller.GetSelectedPart(categoryId)?.PartID;

            foreach (var part in category.Parts)
            {
                if (part == null) continue;

                var btn  = Instantiate(_partButtonPrefab, transform);
                var view = btn.GetComponent<PartButtonView>();

                if (view != null)
                {
                    view.Setup(categoryId, part, OnPartClicked);
                    view.SetSelected(part.PartID == selectedPartId);
                    _spawnedViews.Add(view);
                }
                else
                {
                    Debug.LogWarning($"[CharacterPartsGridController] PartButtonView absent du prefab '{_partButtonPrefab.name}'.", btn);
                }
            }

            // Attendre la frame suivante pour que ContentSizeFitter
            // et GridLayoutGroup calculent les dimensions correctement.
            StartCoroutine(RebuildLayout());
        }

        private void OnPartClicked(string categoryId, string partId)
        {
            _controller.SelectPart(categoryId, partId);
            _onPartChanged?.Raise(categoryId);

            // Mettre à jour le visuel de sélection sur tous les boutons
            foreach (var view in _spawnedViews)
                view.SetSelected(view.PartId == partId);
        }

        private void ClearGrid()
        {
            foreach (var view in _spawnedViews)
                if (view != null) Destroy(view.gameObject);
            _spawnedViews.Clear();
        }

        #endregion

        #region Layout Rebuild

        private IEnumerator RebuildLayout()
        {
            // Frame 1 : laisser Unity instancier et placer les éléments
            yield return null;

            // Rebuild ce container (PartsContent avec GridLayoutGroup)
            var rt = GetComponent<RectTransform>();
            if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

            // Frame 2 : laisser le ContentSizeFitter calculer la hauteur finale
            yield return null;

            // Rebuild le parent (PartsGridScroll / ScrollRect)
            if (transform.parent != null)
            {
                var parentRt = transform.parent.GetComponent<RectTransform>();
                if (parentRt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(parentRt);
            }

            // Forcer Unity à recalculer tous les canvas
            Canvas.ForceUpdateCanvases();
        }

        #endregion
    }
}
