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

                // Sous-categories : dossier vetements/hauts/, sinon juste category.CategoryID/
                var parentCat = registry.GetParentCategory(category.CategoryID);
                var folderPath = parentCat != null
                    ? $"{DATA_ROOT}/{parentCat.CategoryID}/{category.CategoryID}"
                    : $"{DATA_ROOT}/{category.CategoryID}";

                bool wasCreated = ProcessSkinnedPart(
                    partId, smr.sharedMesh, smr.sharedMaterials, category, folderPath, out var error);

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
                if (category == null) continue;

                // Sous-categories en priorite (filtres plus specifiques que le parent).
                foreach (var sub in category.SubCategories)
                {
                    if (sub != null && sub.MatchesMeshName(meshName))
                        return sub;
                }

                // Categorie parente uniquement si elle n'a pas de sous-categories.
                if (!category.HasSubCategories && category.MatchesMeshName(meshName))
                    return category;
            }
            return null;
        }

        private static string BuildPartId(string categoryId, string meshName)
        {
            // Le nom du mesh est unique dans un FBX - pas besoin du prefixe de categorie.
            // Les espaces sont normalises en underscores pour la serialisation.
            _ = categoryId;
            return meshName.Replace(' ', '_');
        }

        private static bool ProcessSkinnedPart(
            string partId,
            Mesh mesh,
            Material[] materials,
            CharacterCategorySO category,
            string folderPath,
            out string error)
        {
            error = null;

            var partAssetPath = $"{folderPath}/{partId}.asset";
            var displayName   = FormatDisplayName(mesh.name, category);

            // Recherche 1 : par PartID (SO correctement importe)
            var existing = FindPartSOByPartId(partId);
            // Recherche 2 : par chemin exact (SO orphelin avec PartID vide)
            if (existing == null)
                existing = AssetDatabase.LoadAssetAtPath<CharacterPartSO>(partAssetPath);

            bool isNew = existing == null;

            if (isNew)
            {
                // Remplir les champs AVANT CreateAsset : Unity serialise l'etat courant de
                // l'instance, donc les donnees sont deja presentes au moment de l'ecriture sur disque.
                existing = ScriptableObject.CreateInstance<CharacterPartSO>();
                existing.SetupFromImporter(partId, displayName, CharacterPartType.SkinnedMesh, mesh, materials);
                EnsureFolderExists(folderPath);
                AssetDatabase.CreateAsset(existing, partAssetPath);
            }
            else
            {
                // Mise a jour d'un SO existant
                existing.SetupFromImporter(partId, displayName, CharacterPartType.SkinnedMesh, mesh, materials);
                EditorUtility.SetDirty(existing);
            }

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

        private static string FormatDisplayName(string meshName, CharacterCategorySO category = null)
        {
            var name = meshName;

            // Retire le prefixe "A_" utilise par les arts (convention temporaire)
            if (name.StartsWith("A_", System.StringComparison.OrdinalIgnoreCase))
                name = name.Substring(2);

            // Retire le prefixe de categorie pour ne garder que la partie descriptive.
            // Ex: mesh "cheveux_courts" + filtre "cheveux_" -> display "Courts"
            if (category != null)
            {
                foreach (var filter in category.MeshNameFilters)
                {
                    if (string.IsNullOrEmpty(filter)) continue;

                    // Normalise le filtre : retire aussi le A_ s'il en a un
                    var f = filter.StartsWith("A_", System.StringComparison.OrdinalIgnoreCase)
                        ? filter.Substring(2)
                        : filter;

                    if (name.StartsWith(f, System.StringComparison.OrdinalIgnoreCase))
                    {
                        var suffix = name.Substring(f.Length).TrimStart('_');
                        if (!string.IsNullOrEmpty(suffix))
                        {
                            name = suffix;
                            break;
                        }
                    }
                }
            }

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
        // Reset complet : clear + import + cleanup + patch en une seule operation
        // --------------------------------------------------------

        [MenuItem("Tools/GlimmerOfHope/Reset complet + Reimport depuis FBX")]
        public static void FullResetAndReimport()
        {
            var registry = AssetDatabase.LoadAssetAtPath<CharacterRegistrySO>(REGISTRY_PATH);
            if (registry == null)
            {
                EditorUtility.DisplayDialog("Reset", "Registry introuvable :\n" + REGISTRY_PATH, "OK");
                return;
            }

            if (!AssetDatabase.IsValidFolder(IMPORT_ROOT))
            {
                EditorUtility.DisplayDialog("Reset",
                    "Dossier FBX introuvable :\n" + IMPORT_ROOT +
                    "\n\nDeplace les FBX dans Art/Characters/ avant de continuer.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                "Reset complet + Reimport",
                "Supprime TOUS les CharacterPartSO et vide toutes les categories,\n" +
                "puis reimporte tout depuis les FBX dans Art/Characters/.\n\n" +
                "Prerequis :\n" +
                "  - Registry > MasterCharacterPrefab pointe vers le bon FBX rig\n" +
                "  - Chaque CategorySO a ses MeshNameFilters a jour\n" +
                "  - La scene CharacterCreator est ouverte (pour le patch Preview)\n\n" +
                "Continuer ?",
                "Oui, reset + reimporter", "Annuler"))
                return;

            // 1. Vide les categories et supprime tous les PartSOs
            ClearAllParts();

            // 2. Reimporte depuis les FBX
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

            // 3. Retire les placeholders non-SkinnedMesh des categories qui ont des SkinnedMesh
            int removed = RunCleanupPlaceholders(registry);

            // 4. Patch CharacterPreviewRenderer si present dans la scene
            string patchInfo = RunPatchMasterFbxSilent(registry);

            // 5. Sauvegarde scene pour que les modifs persistent en Play mode
            EditorSceneManager.SaveOpenScenes();

            // Rapport final unique
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Parts crees : {created}");
            sb.AppendLine($"Parts mis a jour : {updated}");
            if (removed > 0) sb.AppendLine($"Placeholders retires : {removed}");
            sb.AppendLine(patchInfo);
            if (errors.Count > 0)
                sb.AppendLine("\nErreurs :\n" + string.Join("\n", errors));

            EditorUtility.DisplayDialog("Reset + Reimport termine", sb.ToString(), "OK");
        }

        private static void ClearAllParts()
        {
            // 1. Vide les listes _parts de toutes les CategorySOs
            var registry = AssetDatabase.LoadAssetAtPath<CharacterRegistrySO>(REGISTRY_PATH);
            if (registry != null)
            {
                foreach (var category in registry.Categories)
                {
                    if (category == null) continue;
                    ClearCategoryPartsList(category);
                    foreach (var sub in category.SubCategories)
                        if (sub != null) ClearCategoryPartsList(sub);
                }
                AssetDatabase.SaveAssets();
            }

            // 2. Supprime le dossier Parts entier.
            // MoveAssetToTrash gere les sous-dossiers et les .meta ; DeleteAsset ne fonctionne
            // pas sur les dossiers de facon fiable sur toutes les versions Unity/Windows.
            if (AssetDatabase.IsValidFolder(DATA_ROOT))
            {
                bool moved = AssetDatabase.MoveAssetToTrash(DATA_ROOT);
                if (!moved)
                {
                    // Fallback : suppression directe via FileUtil si MoveToTrash echoue
                    FileUtil.DeleteFileOrDirectory(DATA_ROOT);
                    FileUtil.DeleteFileOrDirectory(DATA_ROOT + ".meta");
                }
            }

            AssetDatabase.Refresh();

            // 3. Recrée le dossier vide
            AssetDatabase.CreateFolder("Assets/_Project/Data/Characters", "Parts");
            AssetDatabase.Refresh();
        }

        private static void ClearCategoryPartsList(CharacterCategorySO category)
        {
            var so = new SerializedObject(category);
            so.FindProperty("_parts").arraySize = 0;
            so.ApplyModifiedProperties();
        }

        // --------------------------------------------------------
        // Purge des references "Missing" dans les _parts de toutes les categories.
        // A lancer quand des CharacterPartSO ont ete supprimes manuellement du projet.
        // --------------------------------------------------------

        [MenuItem("Tools/GlimmerOfHope/Cleanup - Purger references Missing dans les categories")]
        public static void PurgeMissingPartReferences()
        {
            var registry = AssetDatabase.LoadAssetAtPath<CharacterRegistrySO>(REGISTRY_PATH);
            if (registry == null)
            {
                EditorUtility.DisplayDialog("Purge", "Registry introuvable.", "OK");
                return;
            }

            int removed = 0;

            void PurgeCategory(CharacterCategorySO cat)
            {
                if (cat == null) return;
                var catSo = new SerializedObject(cat);
                var parts = catSo.FindProperty("_parts");
                for (int i = parts.arraySize - 1; i >= 0; i--)
                {
                    var elem = parts.GetArrayElementAtIndex(i);
                    var partRef = elem.objectReferenceValue as CharacterPartSO;
                    // Detecte les refs null ET les refs "Missing" (objectReferenceValue non nul mais PartID vide)
                    bool isMissing = partRef == null
                        || string.IsNullOrEmpty(partRef.PartID);
                    if (isMissing)
                    {
                        parts.DeleteArrayElementAtIndex(i);
                        removed++;
                    }
                }
                catSo.ApplyModifiedProperties();
            }

            foreach (var category in registry.Categories)
            {
                PurgeCategory(category);
                if (category == null) continue;
                foreach (var sub in category.SubCategories)
                    PurgeCategory(sub);
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Purge terminee",
                removed > 0
                    ? $"{removed} reference(s) manquante(s) retiree(s) de toutes les categories."
                    : "Aucune reference manquante trouvee.",
                "OK");
        }

        // --------------------------------------------------------
        // Diagnostic : liste les meshes trouves dans les FBX et leur categorie
        // --------------------------------------------------------

        [MenuItem("Tools/GlimmerOfHope/Diagnostic - Lister les meshes FBX")]
        public static void ListFbxMeshes()
        {
            var registry = AssetDatabase.LoadAssetAtPath<CharacterRegistrySO>(REGISTRY_PATH);
            var guids    = AssetDatabase.FindAssets("t:Model", new[] { IMPORT_ROOT });
            var sb       = new System.Text.StringBuilder();
            sb.AppendLine($"Dossier : {IMPORT_ROOT}\n");

            int totalMatched = 0, totalSkipped = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;

                var prefab = AssetDatabase.LoadMainAssetAtPath(path) as GameObject;
                if (prefab == null) continue;

                sb.AppendLine($"[FBX] {System.IO.Path.GetFileName(path)}");
                foreach (var smr in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (smr.sharedMesh == null) continue;
                    var meshName = smr.sharedMesh.name;
                    var cat      = registry != null ? FindCategoryForMesh(meshName, registry) : null;
                    if (cat != null)
                    {
                        sb.AppendLine($"  OK  {meshName}  ->  {cat.CategoryID}");
                        totalMatched++;
                    }
                    else
                    {
                        sb.AppendLine($"  --  {meshName}  (aucune categorie)");
                        totalSkipped++;
                    }
                }
                sb.AppendLine();
            }

            sb.AppendLine($"Total : {totalMatched} importes, {totalSkipped} ignores (aucun filtre ne correspond).");
            Debug.Log(sb.ToString());
            EditorUtility.DisplayDialog("Diagnostic FBX",
                $"{totalMatched} meshes importables, {totalSkipped} ignores.\nDetail complet dans la Console Unity.", "OK");
        }

        // --------------------------------------------------------
        // Helpers internes (utilises par FullResetAndReimport)
        // --------------------------------------------------------

        // Retire les parts non-SkinnedMesh des categories qui en ont deja un.
        // Couvre aussi les sous-categories, contrairement a CleanupOldPlaceholders.
        private static int RunCleanupPlaceholders(CharacterRegistrySO registry)
        {
            int removed = 0;
            foreach (var category in registry.Categories)
            {
                if (category == null) continue;
                removed += CleanupCategoryPlaceholders(category);
                foreach (var sub in category.SubCategories)
                    if (sub != null) removed += CleanupCategoryPlaceholders(sub);
            }
            if (removed > 0) AssetDatabase.SaveAssets();
            return removed;
        }

        private static int CleanupCategoryPlaceholders(CharacterCategorySO category)
        {
            bool hasSkinnedMesh = false;
            foreach (var part in category.Parts)
            {
                if (part != null && part.PartType == CharacterPartType.SkinnedMesh)
                { hasSkinnedMesh = true; break; }
            }
            if (!hasSkinnedMesh) return 0;

            int removed = 0;
            var catSo = new SerializedObject(category);
            var parts  = catSo.FindProperty("_parts");
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
            return removed;
        }

        // Patch silencieux du CharacterPreviewRenderer : retourne une ligne de rapport.
        private static string RunPatchMasterFbxSilent(CharacterRegistrySO registry)
        {
            var renderer = Object.FindAnyObjectByType<CharacterPreviewRenderer>(FindObjectsInactive.Include);
            if (renderer == null)
                return "Preview : CharacterPreviewRenderer absent de la scene.";

            var masterPrefab = registry.MasterCharacterPrefab ?? FindMasterCharacterPrefab();
            if (masterPrefab == null)
                return "Preview : aucun FBX master trouve (assigne-le sur le Registry SO).";

            var so = new SerializedObject(renderer);
            so.FindProperty("_masterCharacterPrefab").objectReferenceValue = masterPrefab;
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(renderer.gameObject.scene);
            return $"Preview patche : {masterPrefab.name}";
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
