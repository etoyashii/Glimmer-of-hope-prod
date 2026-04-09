using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Core.Services;
using System.Collections.Generic;
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

        [Header("Anchor Points 3D")]
        [Tooltip("Associe chaque categoryId à un Transform parent pour les prefabs 3D.")]
        [SerializeField] private List<CategoryAnchor> _anchors3D = new();

        [Header("Anchor Points 2D")]
        [Tooltip("Associe chaque categoryId à un SpriteRenderer pour les sprites 2D.")]
        [SerializeField] private List<CategorySpriteRenderer> _spriteRenderers = new();
        #endregion

        #region Private Fields
        private CharacterCreatorController _controller;

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
            RefreshAll();
        }

        private void OnDisable()
        {
            _onPartChanged.Unsubscribe(OnPartChanged);
        }
        #endregion

        #region Private Methods
        private void OnPartChanged(string categoryId)
        {
            RefreshCategory(categoryId);
        }

        private void RefreshAll()
        {
            if (_controller == null)
                return;

            foreach (var category in _controller.Registry.Categories)
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

            if (part.PartType == CharacterPartType.Prefab3D)
                Refresh3D(categoryId, part);
            else
                Refresh2D(categoryId, part);
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

            if (part.Prefab == null)
                return;

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

            sr.sprite = part.Sprite;
        }

        private void ClearCategory(string categoryId)
        {
            if (_spawnedPrefabs.TryGetValue(categoryId, out var existing))
            {
                Destroy(existing);
                _spawnedPrefabs.Remove(categoryId);
            }

            var sr = GetSpriteRenderer(categoryId);
            if (sr != null)
                sr.sprite = null;
        }
        #endregion

        #region Helpers
        private Transform GetAnchor3D(string categoryId)
        {
            foreach (var anchor in _anchors3D)
            {
                if (anchor.categoryId == categoryId)
                    return anchor.anchor;
            }
            return null;
        }

        private SpriteRenderer GetSpriteRenderer(string categoryId)
        {
            foreach (var entry in _spriteRenderers)
            {
                if (entry.categoryId == categoryId)
                    return entry.spriteRenderer;
            }
            return null;
        }
        #endregion
    }
}
