using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GlimmerOfHope.Gameplay.Characters
{
    [CreateAssetMenu(menuName = "GlimmerOfHope/Characters/Category")]
    public class CharacterCategorySO : ScriptableObject
    {
        #region Constants
        private const string GROUP_IDENTITY = "Identity";
        private const string GROUP_PARTS    = "Parts";
        #endregion

        #region Serialized Fields
        [BoxGroup(GROUP_IDENTITY)]
        [FormerlySerializedAs("_categoryID")]
        [SerializeField] private string _categoryId;

        [BoxGroup(GROUP_IDENTITY)]
        [SerializeField] private string _displayName;

        [BoxGroup(GROUP_IDENTITY)]
        [ShowAssetPreview]
        [SerializeField] private Sprite _categoryIcon;

        [BoxGroup(GROUP_IDENTITY)]
        [SerializeField] private CharacterPartType _defaultPartType = CharacterPartType.Prefab3D;

        [BoxGroup(GROUP_IDENTITY)]
        [Tooltip("Prefixes de noms de mesh FBX mappes a cette categorie. Ex: 'A_Tenue', 'Hair_M'. Convention finale : '{CategoryId}_'.")]
        [SerializeField] private string[] _meshNameFilters = new string[0];

        [BoxGroup(GROUP_PARTS)]
        [InfoBox("Ajouter une part : lancer Tools > GlimmerOfHope > 4 - Import Character Parts.", EInfoBoxType.Normal)]
        [SerializeField] private List<CharacterPartSO> _parts = new();
        #endregion

        #region Public Properties
        public string CategoryID                  => _categoryId;
        public string DisplayName                 => _displayName;
        public Sprite CategoryIcon                => _categoryIcon;
        public CharacterPartType DefaultPartType  => _defaultPartType;
        public IReadOnlyList<string> MeshNameFilters => _meshNameFilters;
        public IReadOnlyList<CharacterPartSO> Parts  => _parts;
        #endregion

        #region Public Methods
        public CharacterPartSO GetPartById(string partId)
        {
            foreach (var part in _parts)
            {
                if (part != null && part.PartID == partId)
                    return part;
            }
            return null;
        }

        public bool MatchesMeshName(string meshName)
        {
            foreach (var filter in _meshNameFilters)
            {
                if (!string.IsNullOrEmpty(filter) &&
                    meshName.StartsWith(filter, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
        #endregion

        #region Editor
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_categoryId))
                Debug.LogWarning($"[CharacterCategorySO] '{name}' : categoryId est vide.", this);

            if (_parts == null || _parts.Count == 0)
                Debug.LogWarning($"[CharacterCategorySO] '{name}' : aucune part assignee.", this);
        }
        #endregion
    }
}
