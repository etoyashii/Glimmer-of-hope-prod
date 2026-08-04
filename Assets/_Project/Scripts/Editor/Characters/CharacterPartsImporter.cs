using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GlimmerOfHope.Gameplay.Characters;

namespace GlimmerOfHope.Editor.Characters
{
    public static class CharacterPartsImporter
    {
        private const string IMPORT_ROOT   = "Assets/_Project/Art/Characters";
        private const string DATA_ROOT     = "Assets/_Project/Data/Characters/Parts";
        private const string REGISTRY_PATH = "Assets/_Project/Data/Characters/_Registry.asset";

        [MenuItem("Tools/GlimmerOfHope/4 - Import Character Parts")]
        public static void Import()
        {
            var registry = AssetDatabase.LoadAssetAtPath<CharacterRegistrySO>(REGISTRY_PATH);
            if (registry == null)
            {
                EditorUtility.DisplayDialog("Import", "Registry introuvable :\n" + REGISTRY_PATH, "OK");
                return;
            }

            if (!AssetDatabase.IsValidFolder(IMPORT_ROOT))
            {
                EditorUtility.DisplayDialog(
                    "Import",
                    "Dossier d'import introuvable :\n" + IMPORT_ROOT +
                    "\n\nDeplacer les FBX dans Art/Characters/ avant de lancer l'import.",
                    "OK");
                return;
            }

            int created = 0, updated = 0;
            var errors = new List<string>();

            var guids = AssetDatabase.FindAssets("t:Model", new[] { IMPORT_ROOT });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;

                ProcessFbx(path, registry, ref created, ref updated, errors);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var report = $"Cree : {created}\nMis a jour : {updated}";
            if (errors.Count > 0)
                report += $"\n\nErreurs ({errors.Count}) :\n" + string.Join("\n", errors);

            EditorUtility.DisplayDialog("Import termine", report, "OK");
        }

        private static void ProcessFbx(
            string fbxPath,
            CharacterRegistrySO registry,
            ref int created,
            ref int updated,
            List<string> errors)
        {
            var prefab = AssetDatabase.LoadMainAssetAtPath(fbxPath) as GameObject;
            if (prefab == null)
            {
                errors.Add($"Impossible de charger : {fbxPath}");
                return;
            }

            foreach (var smr in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (smr.sharedMesh == null) continue;

                var category = FindCategoryForMesh(smr.sharedMesh.name, registry);
                if (category == null) continue;

                var partId = BuildPartId(category.CategoryID, smr.sharedMesh.name);
                bool wasCreated = ProcessSkinnedPart(
                    partId, smr.sharedMesh, smr.sharedMaterials, category, out var error);

                if (error != null)  errors.Add(error);
                else if (wasCreated) created++;
                else                 updated++;
            }
        }

        private static CharacterCategorySO FindCategoryForMesh(
            string meshName, CharacterRegistrySO registry)
        {
            foreach (var category in registry.Categories)
            {
                if (category != null && category.MatchesMeshName(meshName))
                    return category;
            }
            return null;
        }

        private static string BuildPartId(string categoryId, string meshName)
        {
            return $"{categoryId}_{meshName}";
        }

        private static bool ProcessSkinnedPart(
            string partId,
            Mesh mesh,
            Material[] materials,
            CharacterCategorySO category,
            out string error)
        {
            error = null;

            var existing = FindPartSOByPartId(partId);
            bool isNew   = existing == null;

            if (isNew)
            {
                existing = ScriptableObject.CreateInstance<CharacterPartSO>();
                EnsureFolderExists($"{DATA_ROOT}/{category.CategoryID}");
                AssetDatabase.CreateAsset(existing, $"{DATA_ROOT}/{category.CategoryID}/{partId}.asset");
            }

            var so = new SerializedObject(existing);
            so.FindProperty("_partId").stringValue       = partId;
            so.FindProperty("_displayName").stringValue  = FormatDisplayName(mesh.name);
            so.FindProperty("_partType").enumValueIndex  = (int)CharacterPartType.SkinnedMesh;
            so.FindProperty("_mesh").objectReferenceValue = mesh;

            var matsProperty = so.FindProperty("_materials");
            matsProperty.arraySize = materials?.Length ?? 0;
            for (int i = 0; i < matsProperty.arraySize; i++)
                matsProperty.GetArrayElementAtIndex(i).objectReferenceValue = materials[i];

            so.ApplyModifiedProperties();

            if (!CategoryContainsPart(category, partId))
            {
                var catSo = new SerializedObject(category);
                var parts = catSo.FindProperty("_parts");
                parts.arraySize++;
                parts.GetArrayElementAtIndex(parts.arraySize - 1).objectReferenceValue = existing;
                catSo.ApplyModifiedProperties();
            }

            return isNew;
        }

        private static CharacterPartSO FindPartSOByPartId(string partId)
        {
            var guids = AssetDatabase.FindAssets("t:CharacterPartSO");
            foreach (var guid in guids)
            {
                var so = AssetDatabase.LoadAssetAtPath<CharacterPartSO>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (so != null && so.PartID == partId)
                    return so;
            }
            return null;
        }

        private static bool CategoryContainsPart(CharacterCategorySO category, string partId)
        {
            foreach (var part in category.Parts)
                if (part != null && part.PartID == partId)
                    return true;
            return false;
        }

        private static string FormatDisplayName(string meshName)
        {
            var name = meshName;

            // Retire le prefixe "A_" utilise par les arts (convention temporaire)
            if (name.StartsWith("A_", System.StringComparison.OrdinalIgnoreCase))
                name = name.Substring(2);

            var parts = name.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                    parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
            }
            return string.Join(" ", parts);
        }

        private static void EnsureFolderExists(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            var child  = Path.GetFileName(path);
            EnsureFolderExists(parent);
            AssetDatabase.CreateFolder(parent, child);
        }

        // --------------------------------------------------------
        // Patch : assigne le bon FBX maitre sur CharacterPreviewRenderer
        // --------------------------------------------------------

        [MenuItem("Tools/GlimmerOfHope/5 - Patch CharacterPreview Master FBX")]
        public static void PatchMasterFbx()
        {
            var renderer = Object.FindAnyObjectByType<CharacterPreviewRenderer>(FindObjectsInactive.Include);
            if (renderer == null)
            {
                EditorUtility.DisplayDialog("Patch", "CharacterPreviewRenderer introuvable dans la scene active.", "OK");
                return;
            }

            // Priorite 1 : le champ defini sur le Registry
            var registry = AssetDatabase.LoadAssetAtPath<CharacterRegistrySO>(REGISTRY_PATH);
            var masterPrefab = registry != null ? registry.MasterCharacterPrefab : null;

            // Priorite 2 : detection automatique (FBX le plus reference par les PartSOs)
            if (masterPrefab == null) masterPrefab = FindMasterCharacterPrefab();

            if (masterPrefab == null)
            {
                EditorUtility.DisplayDialog("Patch",
                    "Aucun FBX adequat trouve.\n\nAssigne-le sur le Registry SO (champ MasterCharacterPrefab)\nou lance d'abord Tools > 4 - Import Character Parts.", "OK");
                return;
            }

            var so = new SerializedObject(renderer);
            so.FindProperty("_masterCharacterPrefab").objectReferenceValue = masterPrefab;
            so.ApplyModifiedProperties();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(renderer.gameObject.scene);

            EditorUtility.DisplayDialog("Patch",
                $"_masterCharacterPrefab mis a jour vers : {masterPrefab.name}\n({AssetDatabase.GetAssetPath(masterPrefab)})", "OK");
        }

        // --------------------------------------------------------
        // Nettoyage : retire les anciens placeholders Prefab3D / Sprite2D
        // des categories qui ont deja des parts SkinnedMesh.
        // --------------------------------------------------------

        [MenuItem("Tools/GlimmerOfHope/6 - Cleanup Old Placeholder Parts")]
        public static void CleanupOldPlaceholders()
        {
            var registry = AssetDatabase.LoadAssetAtPath<CharacterRegistrySO>(REGISTRY_PATH);
            if (registry == null)
            {
                EditorUtility.DisplayDialog("Cleanup", "Registry introuvable.", "OK");
                return;
            }

            int removed = 0;

            foreach (var category in registry.Categories)
            {
                if (category == null) continue;

                bool hasSkinnedMesh = false;
                foreach (var part in category.Parts)
                {
                    if (part != null && part.PartType == CharacterPartType.SkinnedMesh)
                    {
                        hasSkinnedMesh = true;
                        break;
                    }
                }

                if (!hasSkinnedMesh) continue;

                // Retire les parts non-SkinnedMesh de cette categorie
                var catSo = new SerializedObject(category);
                var parts = catSo.FindProperty("_parts");
                for (int i = parts.arraySize - 1; i >= 0; i--)
                {
                    var partRef = parts.GetArrayElementAtIndex(i).objectReferenceValue as CharacterPartSO;
                    if (partRef == null || partRef.PartType != CharacterPartType.SkinnedMesh)
                    {
                        parts.DeleteArrayElementAtIndex(i);
                        removed++;
                    }
                }
                catSo.ApplyModifiedProperties();
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Cleanup",
                removed > 0
                    ? $"{removed} placeholder(s) retires des categories SkinnedMesh."
                    : "Rien a nettoyer.",
                "OK");
        }

        // Retourne le FBX principal : le FBX dont les meshes sont references par le plus de PartSOs.
        internal static GameObject FindMasterCharacterPrefab()
        {
            var partGuids = AssetDatabase.FindAssets("t:CharacterPartSO");
            var fbxRefCounts = new Dictionary<string, int>();

            foreach (var partGuid in partGuids)
            {
                var partSO = AssetDatabase.LoadAssetAtPath<CharacterPartSO>(
                    AssetDatabase.GUIDToAssetPath(partGuid));
                if (partSO == null || partSO.Mesh == null) continue;

                var meshPath = AssetDatabase.GetAssetPath(partSO.Mesh);
                if (string.IsNullOrEmpty(meshPath)) continue;

                fbxRefCounts.TryGetValue(meshPath, out var count);
                fbxRefCounts[meshPath] = count + 1;
            }

            if (fbxRefCounts.Count == 0) return null;

            string bestPath = null;
            int bestCount   = 0;
            foreach (var kvp in fbxRefCounts)
            {
                if (kvp.Value > bestCount)
                {
                    bestCount = kvp.Value;
                    bestPath  = kvp.Key;
                }
            }

            return bestPath != null ? AssetDatabase.LoadMainAssetAtPath(bestPath) as GameObject : null;
        }
    }
}
