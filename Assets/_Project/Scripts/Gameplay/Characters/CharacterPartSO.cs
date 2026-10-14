using UnityEngine;
using UnityEngine.Serialization;
using NaughtyAttributes;
using System.Collections.Generic;

namespace GlimmerOfHope.Gameplay.Characters
{
    public enum CharacterPartType
    {
        Sprite2D,
        Prefab3D,
        SkinnedMesh,
        Color,
        FlipBook
    }

    [System.Serializable]
    public class FlipBookSetting
    {
        public string reference;
        public int value;
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
        [Required("L'ID est obligatoire pour referencer cette part.")]
        [FormerlySerializedAs("_partID")]
        [SerializeField] private string _partId;

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

        [BoxGroup(GROUP_ASSET)]
        [ShowIf(nameof(_partType), CharacterPartType.SkinnedMesh)]
        [Required("Un mesh est requis pour les parts SkinnedMesh.")]
        [SerializeField] private Mesh _mesh;

        [BoxGroup(GROUP_ASSET)]
        [ShowIf(nameof(_partType), CharacterPartType.Color)]
        [SerializeField] private Color _color;
        [SerializeField] private string _colorMeshTarget = "Body";
        [SerializeField] private string _colorMaterialColorReference = "_SkinColor";

        [BoxGroup(GROUP_ASSET)]
        [ShowIf(nameof(_partType), CharacterPartType.FlipBook)]
        [SerializeField] private List<FlipBookSetting> _flipBookSettings;
        [SerializeField] private string _flipBookMeshRendererTarget = "Face";

        [BoxGroup(GROUP_ASSET)]
        [ShowIf(nameof(_partType), CharacterPartType.SkinnedMesh)]
        [SerializeField] private Material[] _materials;

        [BoxGroup(GROUP_METADATA)]
        [SerializeField] private string[] _tags;
        #endregion

        #region Public Properties
        public string PartID         => _partId;
        public string DisplayName    => _displayName;
        public Sprite Thumbnail      => _thumbnail;
        public CharacterPartType PartType => _partType;
        public Sprite Sprite         => _sprite;
        public GameObject Prefab     => _prefab;
        public Mesh Mesh             => _mesh;
        public Material[] Materials  => _materials;
        public string[] Tags         => _tags;
        public Color Color           => _color;
        public string ColorMeshTarget=> _colorMeshTarget;
        public string ColorMaterialColorReference => _colorMaterialColorReference;
        public List<FlipBookSetting> FlipBookSettings => _flipBookSettings;
        public string FlipBookMeshRendererTarget => _flipBookMeshRendererTarget;

        public bool HasValidMaterials
        {
            get
            {
                if (_materials == null || _materials.Length == 0)
                    return false;

                foreach (var mat in _materials)
                {
                    if (mat == null)
                        return false;
                }
                return true;
            }
        }
        #endregion

        #region Editor
        private void OnValidate()
        {
            // Pas de validation si le SO n'a pas encore ete rempli par l'importer.
            if (string.IsNullOrWhiteSpace(_partId)) return;

            if (_partType == CharacterPartType.Sprite2D && _sprite == null)
                Debug.LogWarning($"[CharacterPartSO] `{name}` : type Sprite2D mais aucun sprite assigne.", this);

            if (_partType == CharacterPartType.Prefab3D && _prefab == null)
                Debug.LogWarning($"[CharacterPartSO] `{name}` : type Prefab3D mais aucun prefab assigne.", this);

            if (_partType == CharacterPartType.SkinnedMesh && _mesh == null)
                Debug.LogWarning($"[CharacterPartSO] `{name}` : type SkinnedMesh mais aucun mesh assigne.", this);
        }

#if UNITY_EDITOR
        // Appele par CharacterPartsImporter pour initialiser les champs avant CreateAsset.
        // Les champs sont remplis sur l'instance en memoire, Unity les serialise lors du CreateAsset.
        public void SetupFromImporter(string partId, string displayName,
                                      CharacterPartType partType, Mesh mesh, Material[] mats)
        {
            _partId      = partId;
            _displayName = displayName;
            _partType    = partType;
            _mesh        = mesh;
            _materials   = mats ?? new Material[0];
        }
#endif
        #endregion
    }
}
