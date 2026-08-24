using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using GlimmerOfHope.Gameplay.NewDialogue;


namespace GlimmerOfHope.Editor.NewDialogue
{
    /// <summary>
    /// Batch version of DialogueScriptImporter: imports every .csv file directly inside a
    /// source folder, each as its own DialogueGraph asset in the output folder.
    /// </summary>
    public static class DialogueScriptFolderImporter
    {
        #region Public Methods
        public static void ImportFolder(string sourceFolder, string outputFolder)
        {
            if (string.IsNullOrEmpty(sourceFolder) || !Directory.Exists(sourceFolder))
            {
                Debug.LogError("[DialogueScriptFolderImporter] Source folder is invalid.");
                return;
            }

            string relativeOutputFolder = ToProjectRelativePath(outputFolder);
            if (relativeOutputFolder == null)
            {
                Debug.LogError("[DialogueScriptFolderImporter] Output folder must be inside this project's Assets folder.");
                return;
            }

            var csvFiles = Directory.GetFiles(sourceFolder, "*.csv", SearchOption.TopDirectoryOnly);
            if (csvFiles.Length == 0)
            {
                Debug.LogError($"[DialogueScriptFolderImporter] No .csv files found directly inside '{sourceFolder}'.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(relativeOutputFolder))
                CreateFolderRecursive(relativeOutputFolder);

            int imported = 0;
            foreach (var csvPath in csvFiles)
            {
                string sheetName = Path.GetFileNameWithoutExtension(csvPath);
                string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{relativeOutputFolder}/{SanitizeAssetName(sheetName)}.asset");

                var graph = ScriptableObject.CreateInstance<DialogueGraph>();
                AssetDatabase.CreateAsset(graph, assetPath);

                if (!DialogueScriptImporter.BuildGraphContents(csvPath, graph))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                    continue;
                }

                EditorUtility.SetDirty(graph);
                imported++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[DialogueScriptFolderImporter] Imported {imported} of {csvFiles.Length} feuille(s) from '{sourceFolder}' into '{relativeOutputFolder}'.");
        }
        #endregion

        #region Private Methods
        //Converts an absolute path into an "Assets/..." path, or null if it's outside this project
        private static string ToProjectRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return null;
            string normalized = absolutePath.Replace('\\', '/').TrimEnd('/');
            string dataPath = Application.dataPath.Replace('\\', '/');

            if (normalized.Equals(dataPath, StringComparison.OrdinalIgnoreCase))
                return "Assets";
            if (normalized.StartsWith(dataPath + "/", StringComparison.OrdinalIgnoreCase))
                return "Assets" + normalized.Substring(dataPath.Length);
            return null;
        }

        private static void CreateFolderRecursive(string relativeFolder)
        {
            var parts = relativeFolder.Split('/');
            string current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static string SanitizeAssetName(string name)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars())
                name = name.Replace(invalid, '_');
            return name;
        }
        #endregion
    }
}
