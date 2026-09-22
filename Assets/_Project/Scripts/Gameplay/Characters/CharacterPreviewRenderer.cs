using System;
using System.Collections.Generic;
using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Core.Services;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.Characters
{
    [System.Serializable]
    public class CategoryAnchor
    {
        public string categoryId;
        public Transform anchor;
    }

    [System.Serializable]
    public class CategorySpriteRenderer
    {
        public string categoryId;
        public SpriteRenderer spriteRenderer;
    }

    public class CharacterPreviewRenderer : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Event")]
        [SerializeField] private StringEventChannel _onPartChanged;

        [Header("SkinnedMesh Character")]
        [Tooltip("Prefab FBX contenant tous les SkinnedMeshRenderers du personnage.")]
        [SerializeField] private GameObject _masterCharacterPrefab;

        [Tooltip("Decalage de position du personnage instancie par rapport au pivot CharacterPreview.")]
        [SerializeField] private Vector3 _characterOffset = Vector3.zero;

        [Tooltip("Rotation en Y du personnage instancie (180 si le FBX est exporte de dos).")]
        [SerializeField] private float _characterYRotation = 0f;

        [Header("Meshes permanents")]
        [Tooltip("Ces meshes restent toujours actives, independamment des selections (body, etc.).")]
        [SerializeField] private string[] _alwaysOnMeshNames = { "body" };

        [Header("Anchor Points 3D")]
        [Tooltip("Associe chaque categoryId a un Transform parent pour les prefabs 3D.")]
        [SerializeField] private List<CategoryAnchor> _anchors3D = new();

        [Header("Anchor Points 2D")]
        [Tooltip("Associe chaque categoryId a un SpriteRenderer pour les sprites 2D.")]
        [SerializeField] private List<CategorySpriteRenderer> _spriteRenderers = new();
        #endregion

        #region Private Fields
        private CharacterCreatorController _controller;

        private GameObject _characterInstance;
        // StringComparer.OrdinalIgnoreCase : evite les bugs de casse entre noms de meshes FBX
        // et les entrees de _alwaysOnMeshNames (ex: "Body" vs "body").
        private readonly Dictionary<string, SkinnedMeshRenderer> _smrByMeshName
            = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, GameObject> _spawnedPrefabs = new();
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            _onPartChanged.Subscribe(OnPartChanged);
        }

        private void Start()
        {
            _controller = ServiceLocator.Get<CharacterCreatorController>();

            if (_controller != null)
                _controller.OnColorChanged += OnColorChanged;

            // Le Registry prime sur le champ de scene quand il est assigne.
            if (_controller?.Registry?.MasterCharacterPrefab != null)
                _masterCharacterPrefab = _controller.Registry.MasterCharacterPrefab;

            if (_masterCharacterPrefab != null)
                InstantiateCharacter();

            RefreshAll();
        }

        private void OnDisable()
        {
            _onPartChanged.Unsubscribe(OnPartChanged);
        }

        private void OnValidate()
        {
            if (_characterInstance == null) return;
            _characterInstance.transform.localPosition = _characterOffset;
            _characterInstance.transform.localRotation = Quaternion.Euler(0f, _characterYRotation, 0f);
        }

        private void OnDestroy()
        {
            if (_controller != null)
                _controller.OnColorChanged -= OnColorChanged;
            if (_characterInstance != null)
                Destroy(_characterInstance);
        }
        #endregion

        #region Private Methods
        private void InstantiateCharacter()
        {
            _characterInstance = Instantiate(_masterCharacterPrefab, transform);
            _characterInstance.transform.localPosition = _characterOffset;
            _characterInstance.transform.localRotation = Quaternion.Euler(0f, _characterYRotation, 0f);

            foreach (var smr in _characterInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh == null) continue;
                _smrByMeshName[smr.sharedMesh.name] = smr;
                smr.enabled = false;
            }
            // Desactive aussi les MeshRenderer non skinnes (ex: parts sans bone weights dans le FBX).
            // Patch temporaire jusqu'a ce que ces meshes soient skinnes dans l'outil 3D.
            foreach (var mr in _characterInstance.GetComponentsInChildren<MeshRenderer>(true))
                mr.enabled = false;

            EnableAlwaysOnMeshes();
        }

        private void EnableAlwaysOnMeshes()
        {
            foreach (var meshName in _alwaysOnMeshNames)
            {
                if (_smrByMeshName.TryGetValue(meshName, out var smr))
                    smr.enabled = true;
            }
        }

        private void OnPartChanged(string categoryId)
        {
            RefreshCategory(categoryId);
        }

        private void OnColorChanged(string categoryId, Color color)
        {
            var category = _controller?.Registry?.GetCategoryById(categoryId);
            if (category == null) return;

            foreach (var part in category.Parts)
            {
                if (part?.PartType != CharacterPartType.SkinnedMesh || part.Mesh == null) continue;
                if (!_smrByMeshName.TryGetValue(part.Mesh.name, out var smr) || !smr.enabled) continue;

                var block = new MaterialPropertyBlock();
                smr.GetPropertyBlock(block);
                block.SetColor("_BaseColor", color);
                smr.SetPropertyBlock(block);
                break;
            }
        }

        private void RefreshAll()
        {
            if (_controller == null) return;
            foreach (var category in _controller.Registry.GetAllLeafCategories())
            {
                if (category != null)
                    RefreshCategory(category.CategoryID);
            }
        }

        private void RefreshCategory(string categoryId)
        {
            var part = _controller.GetSelectedPart(categoryId);
            if (part == null)
            {
                ClearCategory(categoryId);
                return;
            }

            switch (part.PartType)
            {
                case CharacterPartType.SkinnedMesh: RefreshSkinnedMesh(categoryId, part); break;
                case CharacterPartType.Prefab3D:    Refresh3D(categoryId, part);          break;
                case CharacterPartType.Sprite2D:    Refresh2D(categoryId, part);          break;
            }
        }

        private void RefreshSkinnedMesh(string categoryId, CharacterPartSO part)
        {
            if (part.Mesh == null) return;

            // Desactive toutes les parts SkinnedMesh de cette categorie
            var category = _controller.Registry.GetCategoryById(categoryId);
            if (category != null)
            {
                foreach (var catPart in category.Parts)
                {
                    if (catPart?.PartType != CharacterPartType.SkinnedMesh || catPart.Mesh == null)
                        continue;
                    if (_smrByMeshName.TryGetValue(catPart.Mesh.name, out var smr))
                        smr.enabled = false;
                }
            }

            // Active la part selectionnee et reapplique la couleur sauvegardee
            if (_smrByMeshName.TryGetValue(part.Mesh.name, out var selectedSmr))
            {
                selectedSmr.enabled = true;
                var block = new MaterialPropertyBlock();
                selectedSmr.GetPropertyBlock(block);
                block.SetColor("_BaseColor", _controller.GetCategoryColor(categoryId));
                selectedSmr.SetPropertyBlock(block);
            }
            else
                Debug.LogWarning($"[CharacterPreviewRenderer] SMR introuvable pour mesh '{part.Mesh.name}'.");
        }

        private void Refresh3D(string categoryId, CharacterPartSO part)
        {
            var anchor = GetAnchor3D(categoryId);
            if (anchor == null)
            {
                Debug.LogWarning($"[CharacterPreviewRenderer] Aucun anchor 3D pour '{categoryId}'.");
                return;
            }

            if (_spawnedPrefabs.TryGetValue(categoryId, out var existing))
            {
                Destroy(existing);
                _spawnedPrefabs.Remove(categoryId);
            }

            if (part.Prefab == null) return;

            var instance = Instantiate(part.Prefab, anchor);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            _spawnedPrefabs[categoryId] = instance;
        }

        private void Refresh2D(string categoryId, CharacterPartSO part)
        {
            var sr = GetSpriteRenderer(categoryId);
            if (sr == null)
            {
                Debug.LogWarning($"[CharacterPreviewRenderer] Aucun SpriteRenderer pour '{categoryId}'.");
                return;
            }
            sr.sprite   = part.Sprite;
            sr.enabled  = part.Sprite != null;
        }

        private void ClearCategory(string categoryId)
        {
            // SkinnedMesh : desactive toutes les parts de la categorie
            var category = _controller?.Registry.GetCategoryById(categoryId);
            if (category != null)
            {
                foreach (var catPart in category.Parts)
                {
                    if (catPart?.PartType != CharacterPartType.SkinnedMesh || catPart.Mesh == null)
                        continue;
                    if (_smrByMeshName.TryGetValue(catPart.Mesh.name, out var smr))
                        smr.enabled = false;
                }
            }

            // Prefab3D : detruit l'instance
            if (_spawnedPrefabs.TryGetValue(categoryId, out var go))
            {
                Destroy(go);
                _spawnedPrefabs.Remove(categoryId);
            }

            // Sprite2D : vide et desactive le SpriteRenderer
            var spriteRenderer = GetSpriteRenderer(categoryId);
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite  = null;
                spriteRenderer.enabled = false;
            }
        }
        #endregion

        #region Helpers
        private Transform GetAnchor3D(string categoryId)
        {
            foreach (var anchor in _anchors3D)
                if (anchor.categoryId == categoryId) return anchor.anchor;
            return null;
        }

        private SpriteRenderer GetSpriteRenderer(string categoryId)
        {
            foreach (var entry in _spriteRenderers)
                if (entry.categoryId == categoryId) return entry.spriteRenderer;
            return null;
        }
        #endregion
    }
}
