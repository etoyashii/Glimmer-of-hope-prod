using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.Characters
{
    [CreateAssetMenu(menuName = "GlimmerOfHope/Characters/Category")]
    public class CharacterCategorySO : ScriptableObject
    {
        #region Constants
        private const string GROUP_IDENTITY = "Identity";
        private const string GROUP_PARTS = "Parts";
        #endregion

        #region Serialized Fields
        [BoxGroup(GROUP_IDENTITY)]
        [Required("L'ID est obligatoire pour référencer cette catégorie.")]
        [SerializeField] private string _categoryID;

        [BoxGroup(GROUP_IDENTITY)]
        [Required]
        [SerializeField] private string _displayName;

        [BoxGroup(GROUP_IDENTITY)]
        [ShowAssetPreview]
        [SerializeField] private Sprite _categoryIcon;

        [BoxGroup(GROUP_PARTS)]
        [InfoBox("Ajouter une part : créer un CharacterPartSO puis le drag ici.", EInfoBoxType.Normal)]
        [SerializeField] private List<CharacterPartSO> _parts = new();
        #endregion

        #region Public Properties
        public string CategoryID => _categoryID;
        public string DisplayName => _displayName;
        public Sprite CategoryIcon => _categoryIcon;
        public IReadOnlyList<CharacterPartSO> Parts => _parts;
        #endregion

        #region Public Methods
        public CharacterPartSO GetPartById(string partID)
        {
            foreach (var part in _parts)
            {
                if (part != null && part.PartID == partID)
                    return part;
            }

            return null;
        }
        #endregion

        #region Editor
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_categoryID))
                Debug.LogWarning($"[CharacterCategorySO] '{name}' : categoryID est vide.", this);

            if (_parts == null || _parts.Count == 0)
                Debug.LogWarning($"[CharacterCategorySO] '{name}' : aucune part assignée.", this);
        }
        #endregion
    }
}
