using NaughtyAttributes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.Characters
{
    [Serializable]
    public class CharacterPartSelection
    {
        public string categoryID;
        public string partID;
    }

    [CreateAssetMenu(menuName = "GlimmerOfHope/Characters/Data")]
    public class CharacterDataSO : ScriptableObject
    {
        #region Constants
        private const string GROUP_IDENTITY = "Identity";
        private const string GROUP_PARTS = "Selected Parts";
        private const string GROUP_META = "Metadata";
        #endregion

        #region Serialized Fields
        [BoxGroup(GROUP_IDENTITY)]
        [Required]
        [SerializeField] private string _characterName = "Mon Personnage";

        [BoxGroup(GROUP_META)]
        [ReadOnly]
        [SerializeField] private string _lastModified;

        [BoxGroup(GROUP_PARTS)]
        [InfoBox("Chaque entrée = categoryId -> partID sélectionné.", EInfoBoxType.Normal)]
        [SerializeField] private List<CharacterPartSelection> _selectedParts = new();
        #endregion

        #region Public Properties
        public string CharacterName => _characterName;
        public string LastModified => _lastModified;
        #endregion

        #region Public Methods
        public string GetSelectedPartId(string categoryID)
        {
            foreach (var selection in _selectedParts)
            {
                if (selection.categoryID == categoryID)
                    return selection.partID;
            }

            return null;
        }

        public void SetSelectedPart(string categoryID, string partID)
        {
            foreach (var selection in _selectedParts)
            {
                if (selection.categoryID == categoryID)
                {
                    selection.partID = partID;
                    UpdateTimestamp();
                    return;
                }
            }

            _selectedParts.Add(new CharacterPartSelection
            {
                categoryID = categoryID,
                partID = partID
            });

            UpdateTimestamp();
        }

        public void Clear()
        {
            _selectedParts.Clear();
            UpdateTimestamp();
        }
        #endregion

        #region Private Methods
        private void UpdateTimestamp()
        {
            _lastModified = DateTime.Now.ToString("o");
        }
        #endregion

        #region Editor
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_characterName))
                Debug.LogWarning($"[CharacterDataSO] '{name}' : characterName est vide.", this);
        }
        #endregion
    }
}
