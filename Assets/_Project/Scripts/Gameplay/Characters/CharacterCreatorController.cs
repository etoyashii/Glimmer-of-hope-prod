using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Core.Services;
using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.Characters
{
    public class CharacterCreatorController : IService
    {
        #region Private Fields
        private CharacterRegistrySO _registry;
        private CharacterDataSO _currentData;

        private readonly Dictionary<string, string> _currentSelections = new();

        private StringEventChannel _onPartChanged;
        #endregion

        #region Public Properties
        public CharacterRegistrySO Registry => _registry;
        public CharacterDataSO CurrentData => _currentData;
        #endregion

        #region Constructor
        public CharacterCreatorController(CharacterRegistrySO registry, StringEventChannel onPartChanged)
        {
            _registry = registry;
            _onPartChanged = onPartChanged;
        }
        #endregion

        #region IService
        public void Initialize()
        {
            ResetToDefaults();
            Debug.Log("[CharacterCreatorController] Initialisé.");
        }

        public void Shutdown()
        {
            _currentSelections.Clear();
            Debug.Log("[CharacterCreatorController] Shutdown.");
        }
        #endregion

        #region Public Methods
        public void SelectPart(string categoryID, string partID)
        {
            if (_registry.GetCategoryById(categoryID) == null)
            {
                Debug.LogWarning($"[CharacterCreatorController] Catégorie inconnue : '{categoryID}'.");
                return;
            }

            _currentSelections[categoryID] = partID;
            _onPartChanged?.Raise(categoryID);
        }
        public CharacterPartSO GetSelectedPart(string categoryID)
        {
            if (!_currentSelections.TryGetValue(categoryID, out var partID))
                return null;

            return _registry.GetPartById(categoryID, partID);
        }
        public void ResetToDefaults()
        {
            _currentSelections.Clear();

            foreach (var category in _registry.Categories)
            {
                if (category == null || category.Parts.Count == 0)
                    continue;

                _currentSelections[category.CategoryID] = category.Parts[0].PartID;
            }
        }
        public void LoadFromData(CharacterDataSO data)
        {
            if (data == null)
            {
                Debug.LogWarning("[CharacterCreatorController] LoadFromData : data est null.");
                return;
            }

            _currentData = data;
            _currentSelections.Clear();

            foreach (var category in _registry.Categories)
            {
                var partID = data.GetSelectedPartId(category.CategoryID);

                if (!string.IsNullOrEmpty(partID))
                    _currentSelections[category.CategoryID] = partID;
            }
        }
        public void SaveToData(CharacterDataSO target)
        {
            if (target == null)
            {
                Debug.LogWarning("[CharacterCreatorController] SaveToData : target est null.");
                return;
            }

            target.Clear();

            foreach (var kvp in _currentSelections)
                target.SetSelectedPart(kvp.Key, kvp.Value);
        }

        #endregion
    }
}
