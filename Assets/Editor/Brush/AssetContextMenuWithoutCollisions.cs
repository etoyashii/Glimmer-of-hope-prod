using UnityEngine;
using UnityEditor;
using System.IO;

public class AssetContextMenuWithoutCollisions
{
    private const string PREFAB_OUTPUT_PATH = "Assets/_Project/Prefabs/BrushAssets";
    private const string TEMPLATE_OUTPUT_PATH = "Assets/_Project/Data/BrushAssets";

    [MenuItem("Assets/Brush Tool/Without collisions with player", false, 0)]
    private static void AddColliderAndCreateTemplate()
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
        instance.layer = LayerMask.NameToLayer("IgnoreCollisions");

        // Add convex MeshCollider on every MeshFilter (children included)
        MeshFilter[] meshFilters = instance.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter mf in meshFilters)
        {
            MeshCollider mc = mf.gameObject.GetComponent<MeshCollider>();
            if (mc == null)
                mc = mf.gameObject.AddComponent<MeshCollider>();

            mc.sharedMesh = mf.sharedMesh;
            mc.convex = true;
        }

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
        template._rotation = Vector3.zero;

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