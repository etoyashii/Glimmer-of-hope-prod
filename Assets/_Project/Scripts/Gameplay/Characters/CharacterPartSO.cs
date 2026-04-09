using UnityEngine;
using NaughtyAttributes;

namespace GlimmerOfHope.Gameplay.Characters
{
    public enum CharacterPartType
    {
        Sprite2D,
        Prefab3D
    }

    [CreateAssetMenu(menuName = "GlimmerOfHope/Characters/Part")]
    public class CharacterPartSO : ScriptableObject
    {
        #region Constants
        private const string GROUP_IDENTITY = "Identity";
        private const string GROUP_ASSET = "Asset";
        private const string GROUP_METADATA = "Metadata";
        #endregion

        #region Serialized Fields
        [BoxGroup(GROUP_IDENTITY)]
        [Required("L'ID est obligatoire pour référencer cette part.")]
        [SerializeField] private string _partID;

        [BoxGroup(GROUP_IDENTITY)]
        [Required]
        [SerializeField] private string _displayName;

        [BoxGroup(GROUP_IDENTITY)]
        [ShowAssetPreview]
        [SerializeField] private Sprite _thumbnail;

        [BoxGroup(GROUP_ASSET)]
        [SerializeField] private CharacterPartType _partType;

        [BoxGroup(GROUP_ASSET)]
        [ShowIf(nameof(_partType), CharacterPartType.Sprite2D)]
        [Required("Un sprite est requis pour les parts 2D.")]
        [ShowAssetPreview]
        [SerializeField] private Sprite _sprite;

        [BoxGroup(GROUP_ASSET)]
        [ShowIf(nameof(_partType), CharacterPartType.Prefab3D)]
        [Required("Un prefab est requis pour les parts 3D.")]
        [SerializeField] private GameObject _prefab;

        [BoxGroup(GROUP_METADATA)]
        [SerializeField] private string[] _tags;
        #endregion

        #region Public Properties
        public string PartID => _partID;
        public string DisplayName => _displayName;
        public Sprite Thumbnail => _thumbnail;
        public CharacterPartType PartType => _partType;
        public Sprite Sprite => _sprite;
        public GameObject Prefab => _prefab;
        public string[] Tags => _tags;
        #endregion

        #region Editor
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_partID))
            {
                Debug.LogWarning($"[CharacterPartSO] `{name}` : partID est vide.", this);
            }
            if (_partType == CharacterPartType.Sprite2D && _sprite == null)
            {
                Debug.LogWarning($"[CharacterPartSO] `{name}` : type Sprite2D mais aucun sprite assigné.", this);
            }
            if (_partType == CharacterPartType.Prefab3D && _prefab == null)
            {
                Debug.LogWarning($"[CharacterPartSO] `{name}` : type Prefab3D mais aucun prefab assigné.", this);
            }
        }
        #endregion
    }
}
