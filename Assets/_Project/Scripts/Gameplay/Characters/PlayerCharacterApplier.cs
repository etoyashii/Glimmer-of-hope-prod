using System;
using System.Collections.Generic;
using GlimmerOfHope.Core.Save;
using GlimmerOfHope.Core.Services;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.Characters
{
    // Place ce composant sur le GameObject parent du personnage (ex: MC).
    //
    // Mode "existing root" : assigner _characterRoot -> scanne le rig deja dans la scene, aucun Instantiate.
    // Mode "prefab"        : laisser _characterRoot vide -> instancie le FBX et place ses enfants
    //                        directement sous ce transform, sans couche intermediaire.
    public class PlayerCharacterApplier : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Registry contenant les categories, les parts, et le FBX maitre.")]
        [SerializeField] private CharacterRegistrySO _registry;

        [Header("Mode")]
        [Tooltip("Racine du personnage deja dans la scene. Si assigne : aucun Instantiate. Si vide : instancie depuis le Registry.")]
        [SerializeField] private Transform _characterRoot;

        [Header("Prefab mode uniquement")]
        [Tooltip("Decalage local applique avant l'unwrap du FBX instancie.")]
        [SerializeField] private Vector3 _characterOffset = Vector3.zero;
        [Tooltip("Rotation en Y du FBX instancie (180 si exporte de dos).")]
        [SerializeField] private float _characterYRotation = 0f;

        [Header("Meshes permanents")]
        [Tooltip("Ces meshes restent toujours actives, independamment des selections (body, etc.).")]
        [SerializeField] private string[] _alwaysOnMeshNames = { "body" };

        // StringComparer.OrdinalIgnoreCase : evite les bugs de casse entre noms de meshes FBX
        // et les entrees de _alwaysOnMeshNames (ex: "Body" vs "body").
        private readonly Dictionary<string, Renderer> _smrByMeshName
            = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, MeshRenderer> _mrByMeshName
            = new(StringComparer.OrdinalIgnoreCase);

        private void Start()
        {
            if (_characterRoot != null)
            {
                BuildSmrMap(_characterRoot);
            }
            else
            {
                var prefab = _registry?.MasterCharacterPrefab;
                if (prefab == null)
                {
                    Debug.LogWarning("[PlayerCharacterApplier] MasterCharacterPrefab non assigne sur le Registry.", this);
                    return;
                }

                // Instancie le FBX, applique l'offset/rotation, puis deplace tous ses enfants
                // directement sous ce transform. Le root vide est detruit.
                var fbxInstance = Instantiate(prefab);
                fbxInstance.transform.SetParent(transform, false);
                fbxInstance.transform.localPosition = _characterOffset;
                fbxInstance.transform.localRotation = Quaternion.Euler(0f, _characterYRotation, 0f);

                for (int i = fbxInstance.transform.childCount - 1; i >= 0; i--)
                    fbxInstance.transform.GetChild(i).SetParent(transform, true);

                Destroy(fbxInstance);

                BuildSmrMap(transform);

                var animator = GetComponentInParent<Animator>();
                if (animator != null) animator.Rebind();
            }

            var saveManager = ServiceLocator.Get<ISaveService>();
            if (saveManager == null)
            {
                Debug.LogWarning("[PlayerCharacterApplier] SaveManager introuvable - applique les defaults.", this);
                ApplyDefaults();
                EnableAlwaysOnMeshes();
                return;
            }

            var selections = saveManager.CurrentSave?.progression?.characterSelections;
            if (selections == null || selections.Count == 0)
                ApplyDefaults();
            else
                ApplySelections(selections);

            EnableAlwaysOnMeshes();
            ApplySavedColors(saveManager.CurrentSave?.progression?.characterColors);
        }

        private void BuildSmrMap(Transform root)
        {
            _smrByMeshName.Clear();
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh == null) continue;
                _smrByMeshName[smr.sharedMesh.name] = smr;
                smr.enabled = false;
            }
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                _mrByMeshName[mr.name] = mr;
                mr.enabled = false;
            }
        }

        private void ApplySelections(List<CharacterSaveEntry> selections)
        {
            if (_registry == null) return;

            foreach (var entry in selections)
            {
                CharacterPartSO part = _registry.GetPartById(entry.categoryId, entry.partId);
                if (part == null)
                    continue;

                switch (part.PartType)
                {
                    case CharacterPartType.SkinnedMesh: ApplySkinnedMeshSelection(part); break;
                    case CharacterPartType.Color: ApplyColorSelection(part); break;
                    case CharacterPartType.FlipBook: ApplyFlipbookSelection(part); break;
                }
            }
        }

        private void ApplySkinnedMeshSelection(CharacterPartSO part)
        {
            if (_smrByMeshName.TryGetValue(part.Mesh.name, out var smr))
            {
                smr.enabled = true;
                if (part.HasValidMaterials)
                    smr.sharedMaterials = part.Materials;
            }
            else
                Debug.LogWarning($"[PlayerCharacterApplier] SMR introuvable pour mesh '{part.Mesh.name}'.", this);
        }

        private void ApplyColorSelection(CharacterPartSO part)
        {
            if (_smrByMeshName.TryGetValue(part.ColorMeshTarget, out Renderer renderer))
                renderer.material.SetColor(part.ColorMaterialColorReference, part.Color);
            else
                Debug.LogWarning($"[PlayerCharacterApplier] SMR introuvable pour mesh '{part.ColorMeshTarget}'.", this);
        }

        private void ApplyFlipbookSelection(CharacterPartSO part)
        {
            if (_mrByMeshName.TryGetValue(part.FlipBookMeshRendererTarget, out MeshRenderer mr))
            {
                foreach (var setting in part.FlipBookSettings)
                    mr.material.SetFloat(setting.reference, setting.value);
            }
        }

        private void EnableAlwaysOnMeshes()
        {
            foreach (var meshName in _alwaysOnMeshNames)
            {
                if (_smrByMeshName.TryGetValue(meshName, out var smr))
                    smr.enabled = true;
                if (_mrByMeshName.TryGetValue(meshName, out var mr))
                    mr.enabled = true;
            }
        }

        private void ApplySavedColors(List<CharacterColorEntry> entries)
        {
            if (entries == null || _registry == null) return;

            foreach (var entry in entries)
            {
                var category = _registry.GetCategoryById(entry.categoryId);
                if (category == null) continue;

                var color = new Color(entry.r, entry.g, entry.b);
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
        }

        private void ApplyDefaults()
        {
            if (_registry == null) return;

            // GetAllLeafCategories couvre les sous-categories (Hauts, Bas, etc.)
            // que _registry.Categories (top-level seulement) manquerait.
            foreach (var category in _registry.GetAllLeafCategories())
            {
                if (category == null) continue;
                if (category.ExcludesSiblings) continue;
                foreach (var part in category.Parts)
                {
                    if (part == null || part.PartType != CharacterPartType.SkinnedMesh || part.Mesh == null)
                        continue;
                    if (_smrByMeshName.TryGetValue(part.Mesh.name, out var smr))
                    {
                        smr.enabled = true;
                        if (part.HasValidMaterials)
                            smr.sharedMaterials = part.Materials;
                        break;
                    }
                }
            }
        }
    }
}
