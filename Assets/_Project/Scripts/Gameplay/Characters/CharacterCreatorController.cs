using System.Collections.Generic;
using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Core.Save;
using GlimmerOfHope.Core.Services;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.Characters
{
    public class CharacterCreatorController : IService
    {
        #region Private Fields
        private readonly CharacterRegistrySO _registry;
        private readonly StringEventChannel _onPartChanged;
        private readonly Dictionary<string, string> _currentSelections = new();
        #endregion

        #region Public Properties
        public CharacterRegistrySO Registry => _registry;
        #endregion

        #region Constructor
        public CharacterCreatorController(CharacterRegistrySO registry, StringEventChannel onPartChanged)
        {
            _registry      = registry;
            _onPartChanged = onPartChanged;
        }
        #endregion

        #region IService
        public void Initialize()
        {
            ResetToDefaults();
            LoadSavedSelections();
            Debug.Log("[CharacterCreatorController] Initialise.");
        }

        public void Shutdown()
        {
            _currentSelections.Clear();
            Debug.Log("[CharacterCreatorController] Shutdown.");
        }
        #endregion

        #region Public Methods
        public void SelectPart(string categoryId, string partId)
        {
            if (_registry.GetCategoryById(categoryId) == null)
            {
                Debug.LogWarning($"[CharacterCreatorController] Categorie inconnue : '{categoryId}'.");
                return;
            }

            _currentSelections[categoryId] = partId;
            _onPartChanged?.Raise(categoryId);
        }

        public CharacterPartSO GetSelectedPart(string categoryId)
        {
            if (!_currentSelections.TryGetValue(categoryId, out var partId))
                return null;

            return _registry.GetPartById(categoryId, partId);
        }

        public void ResetToDefaults()
        {
            _currentSelections.Clear();

            foreach (var category in _registry.Categories)
            {
                if (category == null || category.Parts.Count == 0) continue;

                CharacterPartSO first = null;
                CharacterPartSO firstSkinnedMesh = null;

                foreach (var part in category.Parts)
                {
                    if (part == null) continue;
                    if (first == null) first = part;
                    if (firstSkinnedMesh == null && part.PartType == CharacterPartType.SkinnedMesh)
                        firstSkinnedMesh = part;
                }

                var defaultPart = firstSkinnedMesh ?? first;
                if (defaultPart != null)
                    _currentSelections[category.CategoryID] = defaultPart.PartID;
            }
        }

        public void SaveCurrentSelections()
        {
            var saveManager = ServiceLocator.Get<SaveManager>();
            if (saveManager == null)
            {
                Debug.LogWarning("[CharacterCreatorController] SaveManager introuvable, selection non sauvegardee.");
                return;
            }

            var list = saveManager.CurrentSave.progression.characterSelections;
            list.Clear();
            foreach (var kvp in _currentSelections)
                list.Add(new CharacterSaveEntry { categoryId = kvp.Key, partId = kvp.Value });

            saveManager.Save();
        }
        #endregion

        #region Private Methods
        private void LoadSavedSelections()
        {
            var saveManager = ServiceLocator.Get<SaveManager>();
            if (saveManager == null) return;

            var saved = saveManager.CurrentSave?.progression?.characterSelections;
            if (saved == null || saved.Count == 0) return;

            foreach (var entry in saved)
            {
                if (!string.IsNullOrEmpty(entry.categoryId) && !string.IsNullOrEmpty(entry.partId))
                    _currentSelections[entry.categoryId] = entry.partId;
            }
        }
        #endregion
    }
}
