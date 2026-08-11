using System.Collections.Generic;
using GlimmerOfHope.Core.Save;
using GlimmerOfHope.Core.Services;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.Characters
{
    // Place ce composant sur n'importe quel GameObject de scene de jeu
    // qui doit afficher le personnage tel que configure dans le CharacterCreator.
    public class PlayerCharacterApplier : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Registry contenant les categories, les parts, et le FBX maitre.")]
        [SerializeField] private CharacterRegistrySO _registry;

        [Header("Transform")]
        [Tooltip("Decalage de position par rapport au pivot de ce GameObject.")]
        [SerializeField] private Vector3 _characterOffset = Vector3.zero;
        [Tooltip("Rotation en Y de l'instance (180 si le FBX est exporte de dos).")]
        [SerializeField] private float _characterYRotation = 0f;

        private GameObject _characterInstance;
        private readonly Dictionary<string, SkinnedMeshRenderer> _smrByMeshName = new();

        private void Start()
        {
            var prefab = _registry?.MasterCharacterPrefab;
            if (prefab == null)
            {
                Debug.LogWarning("[PlayerCharacterApplier] MasterCharacterPrefab non assigne sur le Registry.", this);
                return;
            }

            _characterInstance = Instantiate(prefab, transform);
            _characterInstance.transform.localPosition = _characterOffset;
            _characterInstance.transform.localRotation = Quaternion.Euler(0f, _characterYRotation, 0f);

            foreach (var smr in _characterInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh == null) continue;
                _smrByMeshName[smr.sharedMesh.name] = smr;
                smr.enabled = false;
            }

            var saveManager = ServiceLocator.Get<SaveManager>();
            if (saveManager == null)
            {
                Debug.LogWarning("[PlayerCharacterApplier] SaveManager introuvable - applique les defaults.", this);
                ApplyDefaults();
                return;
            }

            var selections = saveManager.CurrentSave?.progression?.characterSelections;
            if (selections == null || selections.Count == 0)
                ApplyDefaults();
            else
                ApplySelections(selections);
        }

        private void ApplySelections(List<CharacterSaveEntry> selections)
        {
            if (_registry == null) return;

            foreach (var entry in selections)
            {
                var part = _registry.GetPartById(entry.categoryId, entry.partId);
                if (part == null || part.PartType != CharacterPartType.SkinnedMesh || part.Mesh == null)
                    continue;

                if (_smrByMeshName.TryGetValue(part.Mesh.name, out var smr))
                    smr.enabled = true;
                else
                    Debug.LogWarning($"[PlayerCharacterApplier] SMR introuvable pour mesh '{part.Mesh.name}'.", this);
            }
        }

        // Active la premiere part SkinnedMesh de chaque categorie si pas de save.
        private void ApplyDefaults()
        {
            if (_registry == null) return;

            foreach (var category in _registry.Categories)
            {
                if (category == null) continue;
                foreach (var part in category.Parts)
                {
                    if (part == null || part.PartType != CharacterPartType.SkinnedMesh || part.Mesh == null)
                        continue;
                    if (_smrByMeshName.TryGetValue(part.Mesh.name, out var smr))
                    {
                        smr.enabled = true;
                        break;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (_characterInstance != null)
                Destroy(_characterInstance);
        }
    }
}
