using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using GlimmerOfHope.Gameplay.Characters;

namespace GlimmerOfHope.Editor.Characters
{
    public static class CharacterTestDataGenerator
    {
        #region Paths

        private const string REGISTRY_PATH   = "Assets/_Project/Data/Characters/_Registry.asset";
        private const string PARTS_PATH      = "Assets/_Project/Data/Characters/Parts";
        private const string PREFABS_PATH    = "Assets/_Project/Prefabs/Characters/Placeholder";
        private const string MATERIALS_PATH  = "Assets/_Project/Art/Materials/Placeholder";

        #endregion

        #region Test Data

        private class PartDef
        {
            public string            Id;
            public string            Label;
            public PrimitiveType     Primitive;
            public Color             Color;
            public Vector3           Scale;
            public CharacterPartType PartType;
        }

        private static readonly Dictionary<string, List<PartDef>> TEST_DATA =
            new Dictionary<string, List<PartDef>>
        {
            ["tete"] = new List<PartDef>
            {
                new PartDef { Id="tete_ronde",  Label="Ronde",  Primitive=PrimitiveType.Sphere,  Color=new Color(0.95f,0.80f,0.62f), Scale=new Vector3(.22f,.22f,.22f), PartType=CharacterPartType.Prefab3D },
                new PartDef { Id="tete_carree", Label="Carrée", Primitive=PrimitiveType.Cube,    Color=new Color(0.88f,0.68f,0.50f), Scale=new Vector3(.20f,.20f,.20f), PartType=CharacterPartType.Prefab3D },
                new PartDef { Id="tete_longue", Label="Longue", Primitive=PrimitiveType.Capsule, Color=new Color(0.78f,0.60f,0.45f), Scale=new Vector3(.16f,.22f,.16f), PartType=CharacterPartType.Prefab3D },
            },
            ["cheveux"] = new List<PartDef>
            {
                new PartDef { Id="che_courts",  Label="Courts",  Primitive=PrimitiveType.Cylinder, Color=new Color(0.18f,0.12f,0.08f), Scale=new Vector3(.24f,.06f,.24f), PartType=CharacterPartType.Prefab3D },
                new PartDef { Id="che_longs",   Label="Longs",   Primitive=PrimitiveType.Capsule,  Color=new Color(0.65f,0.35f,0.15f), Scale=new Vector3(.14f,.28f,.14f), PartType=CharacterPartType.Prefab3D },
                new PartDef { Id="che_boucles", Label="Bouclés", Primitive=PrimitiveType.Sphere,   Color=new Color(0.85f,0.65f,0.20f), Scale=new Vector3(.26f,.20f,.26f), PartType=CharacterPartType.Prefab3D },
                new PartDef { Id="che_rase",    Label="Rasé",    Primitive=PrimitiveType.Cylinder, Color=new Color(0.30f,0.30f,0.30f), Scale=new Vector3(.24f,.02f,.24f), PartType=CharacterPartType.Prefab3D },
            },
            ["vetements"] = new List<PartDef>
            {
                new PartDef { Id="vet_tshirt",  Label="T-Shirt", Primitive=PrimitiveType.Cube,    Color=new Color(0.29f,0.56f,0.85f), Scale=new Vector3(.30f,.32f,.18f), PartType=CharacterPartType.Prefab3D },
                new PartDef { Id="vet_chemise", Label="Chemise", Primitive=PrimitiveType.Cube,    Color=new Color(0.95f,0.95f,0.95f), Scale=new Vector3(.30f,.32f,.18f), PartType=CharacterPartType.Prefab3D },
                new PartDef { Id="vet_veste",   Label="Veste",   Primitive=PrimitiveType.Cube,    Color=new Color(0.17f,0.17f,0.22f), Scale=new Vector3(.34f,.34f,.20f), PartType=CharacterPartType.Prefab3D },
                new PartDef { Id="vet_robe",    Label="Robe",    Primitive=PrimitiveType.Capsule, Color=new Color(0.75f,0.35f,0.55f), Scale=new Vector3(.26f,.40f,.26f), PartType=CharacterPartType.Prefab3D },
            },
            ["accessoires"] = new List<PartDef>
            {
                new PartDef { Id="acc_chapeau",  Label="Chapeau",  Primitive=PrimitiveType.Cylinder, Color=new Color(0.20f,0.20f,0.20f), Scale=new Vector3(.26f,.10f,.26f), PartType=CharacterPartType.Prefab3D },
                new PartDef { Id="acc_couronne", Label="Couronne", Primitive=PrimitiveType.Cylinder, Color=new Color(0.95f,0.75f,0.10f), Scale=new Vector3(.22f,.08f,.22f), PartType=CharacterPartType.Prefab3D },
                new PartDef { Id="acc_aucun",    Label="Aucun",    Primitive=PrimitiveType.Sphere,   Color=new Color(0.5f,0.5f,0.5f,0f), Scale=new Vector3(.01f,.01f,.01f), PartType=CharacterPartType.Prefab3D },
            },
            ["yeux"] = new List<PartDef>
            {
                new PartDef { Id="yeux_ronds",  Label="Ronds",  Color=new Color(0.10f,0.10f,0.10f), PartType=CharacterPartType.Sprite2D },
                new PartDef { Id="yeux_brides", Label="Bridés", Color=new Color(0.25f,0.15f,0.08f), PartType=CharacterPartType.Sprite2D },
                new PartDef { Id="yeux_bleus",  Label="Bleus",  Color=new Color(0.20f,0.45f,0.80f), PartType=CharacterPartType.Sprite2D },
                new PartDef { Id="yeux_verts",  Label="Verts",  Color=new Color(0.20f,0.55f,0.30f), PartType=CharacterPartType.Sprite2D },
            },
            ["bouche"] = new List<PartDef>
            {
                new PartDef { Id="bou_sourire", Label="Sourire", Color=new Color(0.75f,0.30f,0.35f), PartType=CharacterPartType.Sprite2D },
                new PartDef { Id="bou_large",   Label="Large",   Color=new Color(0.85f,0.20f,0.25f), PartType=CharacterPartType.Sprite2D },
                new PartDef { Id="bou_neutre",  Label="Neutre",  Color=new Color(0.60f,0.35f,0.35f), PartType=CharacterPartType.Sprite2D },
            },
        };

        #endregion

        #region Menu

        [MenuItem("Tools/GlimmerOfHope/Generate Test Character Data")]
        public static void Generate()
        {
            if (!EditorUtility.DisplayDialog(
                    "Générer les données de test",
                    "Ce script va créer des materials, prefabs et CharacterPartSO de test,\n" +
                    "puis les assigner aux catégories existantes.\n\n" +
                    "Les parts existantes ne seront PAS écrasées.\n\nContinuer ?",
                    "Générer", "Annuler"))
                return;

            var registry = AssetDatabase.LoadAssetAtPath<CharacterRegistrySO>(REGISTRY_PATH);
            if (registry == null)
            {
                EditorUtility.DisplayDialog("Erreur",
                    $"Registry introuvable :\n{REGISTRY_PATH}", "OK");
                return;
            }

            EnsureFolders();
            AssetDatabase.Refresh();

            int totalParts = 0;

            foreach (var kvp in TEST_DATA)
            {
                var categoryId = kvp.Key;
                var defs       = kvp.Value;

                var category = FindCategory(registry, categoryId);
                if (category == null)
                {
                    Debug.LogWarning($"[TestDataGenerator] Catégorie '{categoryId}' introuvable — skipped.");
                    continue;
                }

                foreach (var def in defs)
                {
                    if (CategoryHasPart(category, def.Id))
                    {
                        Debug.Log($"[TestDataGenerator] '{def.Id}' existe déjà — skipped.");
                        continue;
                    }

                    CharacterPartSO partSO = null;

                    if (def.PartType == CharacterPartType.Prefab3D)
                    {
                        var mat    = CreateOrLoadMaterial(def.Id, def.Color);
                        var prefab = CreatePrimitivePrefab(def.Id, def.Primitive, def.Scale, mat);
                        partSO     = CreatePartSO(categoryId, def, prefab, null);
                    }
                    else
                    {
                        var sprite = CreateColorSprite(def.Id, def.Color);
                        partSO     = CreatePartSO(categoryId, def, null, sprite);
                    }

                    if (partSO != null)
                    {
                        AddPartToCategory(category, partSO);
                        totalParts++;
                    }
                }

                EditorUtility.SetDirty(category);
            }

            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Génération terminée ✔",
                $"{totalParts} parts créées et assignées.\n\n" +
                "Lance le jeu pour tester la personnalisation.",
                "OK");
        }

        #endregion

        #region SO Creation

        private static CharacterPartSO CreatePartSO(string categoryId, PartDef def, GameObject prefab, Sprite sprite)
        {
            var folderPath = $"{PARTS_PATH}/{CapitalizeFirst(categoryId)}";
            var assetPath  = $"{folderPath}/{def.Id}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<CharacterPartSO>(assetPath);
            if (existing != null) return existing;

            var so = ScriptableObject.CreateInstance<CharacterPartSO>();

            SetPrivateField(so, "_partId",      def.Id);
            SetPrivateField(so, "_displayName", def.Label);
            SetPrivateField(so, "_partType",    def.PartType);

            if (prefab != null) SetPrivateField(so, "_prefab",  prefab);
            if (sprite != null) SetPrivateField(so, "_sprite",  sprite);

            AssetDatabase.CreateAsset(so, assetPath);
            EditorUtility.SetDirty(so);

            Debug.Log($"[TestDataGenerator] Créé : {assetPath}");
            return so;
        }

        #endregion

        #region Asset Factories

        private static Material CreateOrLoadMaterial(string id, Color color)
        {
            var path     = $"{MATERIALS_PATH}/Mat_{id}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit")
                      ?? Shader.Find("Standard");

            var mat = new Material(shader) { color = color };

            if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic",   0f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.3f);

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static GameObject CreatePrimitivePrefab(string id, PrimitiveType primitive, Vector3 scale, Material mat)
        {
            var path     = $"{PREFABS_PATH}/{id}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = GameObject.CreatePrimitive(primitive);
            go.name             = id;
            go.transform.localScale = scale;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = mat;

            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);

            if (prefab == null)
                Debug.LogError($"[TestDataGenerator] SaveAsPrefabAsset a échoué pour : {path}");

            return prefab;
        }

        private static Sprite CreateColorSprite(string id, Color color)
        {
            EnsureFolder($"{PARTS_PATH}/Sprites");

            var path     = $"{PARTS_PATH}/Sprites/{id}.png";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null) return existing;

            var tex    = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();

            var absPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
            File.WriteAllBytes(absPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType         = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        #endregion

        #region Category Helpers

        private static CharacterCategorySO FindCategory(CharacterRegistrySO registry, string categoryId)
        {
            foreach (var cat in registry.Categories)
                if (cat != null && cat.CategoryID == categoryId)
                    return cat;
            return null;
        }

        private static bool CategoryHasPart(CharacterCategorySO category, string partId)
        {
            foreach (var p in category.Parts)
                if (p != null && p.PartID == partId)
                    return true;
            return false;
        }

        private static void AddPartToCategory(CharacterCategorySO category, CharacterPartSO part)
        {
            var so   = new SerializedObject(category);
            var list = so.FindProperty("_parts");
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = part;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion

        #region Reflection Helper

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance);

            if (field == null)
            {
                Debug.LogError($"[TestDataGenerator] Champ '{fieldName}' introuvable sur {target.GetType().Name}.");
                return;
            }

            field.SetValue(target, value);
        }

        #endregion

        #region Folder Helpers

        private static void EnsureFolders()
        {
            EnsureFolder(PREFABS_PATH);
            EnsureFolder(MATERIALS_PATH);
            foreach (var key in TEST_DATA.Keys)
                EnsureFolder($"{PARTS_PATH}/{CapitalizeFirst(key)}");
        }

        private static void EnsureFolder(string path)
        {
            var parts   = path.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static string CapitalizeFirst(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1);

        #endregion
    }
}
