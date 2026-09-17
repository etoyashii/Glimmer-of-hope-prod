using UnityEngine;
using UnityEditor;
using System.IO;

public class SpriteMaterialGenerator : EditorWindow
{
    private Shader shader;
    private DefaultAsset textureFolder;
    private DefaultAsset outputFolder;

    // IMPORTANT:
    // This must match the "Reference" name of the Texture2D property
    // in your Shader Graph Blackboard.
    private string textureProperty = "_Sprite";

    private bool overwriteExisting = false;

    [MenuItem("Tools/Sprite Material Generator")]
    public static void ShowWindow()
    {
        GetWindow<SpriteMaterialGenerator>("Sprite Material Generator");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Sprite Material Generator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Creates one Material per texture and assigns each texture to your custom Shader Graph.",
            MessageType.Info
        );

        EditorGUILayout.Space(8);

        shader = (Shader)EditorGUILayout.ObjectField(
            "Shader Graph",
            shader,
            typeof(Shader),
            false
        );

        textureFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Texture Folder",
            textureFolder,
            typeof(DefaultAsset),
            false
        );

        outputFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Material Folder",
            outputFolder,
            typeof(DefaultAsset),
            false
        );

        EditorGUILayout.Space(5);

        textureProperty = EditorGUILayout.TextField(
            "Texture Property",
            textureProperty
        );

        overwriteExisting = EditorGUILayout.Toggle(
            "Overwrite Existing",
            overwriteExisting
        );

        EditorGUILayout.Space(10);

        GUI.enabled = shader != null && textureFolder != null && outputFolder != null;

        if (GUILayout.Button("GENERATE MATERIALS", GUILayout.Height(35)))
        {
            GenerateMaterials();
        }

        GUI.enabled = true;

        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Example:\n" +
            "Tree_Base.png -> Tree_Base.mat\n" +
            "Rock.png -> Rock.mat\n\n" +
            "The texture is assigned to the Shader Graph property defined above.",
            MessageType.None
        );
    }

    private void GenerateMaterials()
    {
        string texturePath = AssetDatabase.GetAssetPath(textureFolder);
        string outputPath = AssetDatabase.GetAssetPath(outputFolder);

        if (!AssetDatabase.IsValidFolder(texturePath))
        {
            EditorUtility.DisplayDialog(
                "Error",
                "The selected Texture Folder is not a valid Unity folder.",
                "OK"
            );
            return;
        }

        if (!AssetDatabase.IsValidFolder(outputPath))
        {
            EditorUtility.DisplayDialog(
                "Error",
                "The selected Material Folder is not a valid Unity folder.",
                "OK"
            );
            return;
        }

        if (shader == null)
        {
            EditorUtility.DisplayDialog(
                "Error",
                "Please assign your Shader Graph.",
                "OK"
            );
            return;
        }

        if (!shader.HasTextureProperty(textureProperty))
        {
            EditorUtility.DisplayDialog(
                "Error",
                "The Shader does not contain a Texture2D property named:\n\n" +
                textureProperty +
                "\n\nCheck the Reference name in your Shader Graph Blackboard.",
                "OK"
            );
            return;
        }

        string[] textureGUIDs = AssetDatabase.FindAssets(
            "t:Texture2D",
            new[] { texturePath }
        );

        int created = 0;
        int skipped = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (string guid in textureGUIDs)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

                if (texture == null)
                    continue;

                string textureName = Path.GetFileNameWithoutExtension(assetPath);
                string materialPath = outputPath + "/" + textureName + ".mat";

                if (File.Exists(materialPath))
                {
                    if (!overwriteExisting)
                    {
                        skipped++;
                        continue;
                    }

                    AssetDatabase.DeleteAsset(materialPath);
                }

                Material material = new Material(shader);
                material.name = textureName;

                material.SetTexture(textureProperty, texture);

                AssetDatabase.CreateAsset(material, materialPath);

                created++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        EditorUtility.DisplayDialog(
            "Generation Complete",
            "Materials created: " + created +
            "\nMaterials skipped: " + skipped,
            "OK"
        );
    }
}
