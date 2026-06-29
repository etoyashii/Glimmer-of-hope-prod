using UnityEngine;
using UnityEditor;
using System.IO;

public class AssetContextMenuWithCollisions
{
    private const string PREFAB_OUTPUT_PATH = "Assets/_Project/Prefabs/BrushAssets";
    private const string TEMPLATE_OUTPUT_PATH = "Assets/_Project/Data/BrushAssets";

    [MenuItem("Assets/Brush Tool/With collisions with player", false, 0)]
    private static void CreateTemplate()
    {
        GameObject selected = Selection.activeObject as GameObject;
        if (selected == null)
        {
            Debug.LogWarning("Please select a prefab or fbx.");
            return;
        }

        string sourcePath = AssetDatabase.GetAssetPath(selected);
        string assetName = Path.GetFileNameWithoutExtension(sourcePath);
        bool isFbx = Path.GetExtension(sourcePath).ToLower() == ".fbx";

        // Ensure output directories exist
        if (!AssetDatabase.IsValidFolder(PREFAB_OUTPUT_PATH))
        {
            Directory.CreateDirectory(PREFAB_OUTPUT_PATH);
            AssetDatabase.Refresh();
        }
        if (!AssetDatabase.IsValidFolder(TEMPLATE_OUTPUT_PATH))
        {
            Directory.CreateDirectory(TEMPLATE_OUTPUT_PATH);
            AssetDatabase.Refresh();
        }

        // Instantiate the asset (works for both fbx and prefab)
        GameObject instance = PrefabUtility.InstantiatePrefab(selected) as GameObject;

        // Save prefab into BrushAssets folder
        string prefabPath = AssetDatabase.GenerateUniqueAssetPath(
            Path.Combine(PREFAB_OUTPUT_PATH, assetName + ".prefab")
        );

        bool success;
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath, out success);
        Object.DestroyImmediate(instance);

        if (!success)
        {
            Debug.LogError($"Failed to save prefab at: {prefabPath}");
            return;
        }

        // Create the AssetTemplate ScriptableObject into BrushAssets data folder
        AssetTemplate template = ScriptableObject.CreateInstance<AssetTemplate>();
        template._asset = savedPrefab;
        template._limiteSize = new Vector2(1f, 1f);
        template._weight = 1;
        template._rotation = 0f;

        string templatePath = AssetDatabase.GenerateUniqueAssetPath(
            Path.Combine(TEMPLATE_OUTPUT_PATH, assetName + "_Template.asset")
        );

        AssetDatabase.CreateAsset(template, templatePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = template;

        Debug.Log($"Prefab saved at \"{prefabPath}\" and template created at: {templatePath}");
    }

    [MenuItem("Assets/Brush Tool/Add Convex Collider + Create Template", true)]
    private static bool AddColliderAndCreateTemplateValidation()
    {
        return Selection.activeObject is GameObject;
    }
}