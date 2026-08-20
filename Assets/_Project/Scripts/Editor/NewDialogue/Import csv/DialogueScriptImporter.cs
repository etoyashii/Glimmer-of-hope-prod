using System;
using System.Collections.Generic;
using System.IO;
using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor.NewDialogue
{
    /// <summary>
    /// Entry point for the script-style CSV import. One row per dialogue line, read top to
    /// bottom — see the Legend tab of the template sheet for the exact syntax.
    ///
    /// A CSV can only ever hold one sheet/tab (that's a limitation of the format itself, not
    /// this tool), so "one graph per feuille" means: export each tab as its own CSV, put them
    /// in a folder, and use <see cref="ImportFolder"/> to turn every file in that folder into
    /// its own DialogueGraph named after the file.
    /// </summary>
    public static class DialogueScriptImporter
    {
        /// <summary>Imports a single CSV into one graph.</summary>
        public static void Import(string csvPath, DialogueGraph targetGraph)
        {
            var graph = targetGraph != null ? targetGraph : CreateNewGraphAsset(Path.GetFileNameWithoutExtension(csvPath));
            if (graph == null) return; // user cancelled the save dialog

            if (!BuildGraphContents(csvPath, graph))
                return;

            EditorUtility.SetDirty(graph);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[DialogueScriptImporter] Imported '{Path.GetFileName(csvPath)}' into '{graph.name}'.");
        }

        /// <summary>
        /// Imports every .csv file directly inside <paramref name="sourceFolder"/> as its own
        /// DialogueGraph (one feuille exported per file), saving the new assets into
        /// <paramref name="outputFolder"/>. Each graph's asset name is taken from its CSV's
        /// file name, so "Tavern.csv" becomes "Tavern.asset".
        /// </summary>
        public static void ImportFolder(string sourceFolder, string outputFolder)
        {
            if (string.IsNullOrEmpty(sourceFolder) || !Directory.Exists(sourceFolder))
            {
                Debug.LogError("[DialogueScriptImporter] Source folder is invalid.");
                return;
            }

            string relativeOutputFolder = ToProjectRelativePath(outputFolder);
            if (relativeOutputFolder == null)
            {
                Debug.LogError("[DialogueScriptImporter] Output folder must be inside this project's Assets folder.");
                return;
            }

            var csvFiles = Directory.GetFiles(sourceFolder, "*.csv", SearchOption.TopDirectoryOnly);
            if (csvFiles.Length == 0)
            {
                Debug.LogError($"[DialogueScriptImporter] No .csv files found directly inside '{sourceFolder}'.");
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

                if (!BuildGraphContents(csvPath, graph))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                    continue;
                }

                EditorUtility.SetDirty(graph);
                imported++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[DialogueScriptImporter] Imported {imported} of {csvFiles.Length} feuille(s) from '{sourceFolder}' into '{relativeOutputFolder}'.");
        }

        /// <summary>Parses one CSV and populates <paramref name="graph"/>'s nodes. Does not save/refresh assets.</summary>
        private static bool BuildGraphContents(string csvPath, DialogueGraph graph)
        {
            var rawRows = CsvUtility.Parse(csvPath);
            if (rawRows.Count < 2)
            {
                Debug.LogError($"[DialogueScriptImporter] '{Path.GetFileName(csvPath)}' has no data rows.");
                return false;
            }

            var dataRows = new List<string[]>();
            for (int i = 1; i < rawRows.Count; i++) // skip header row
            {
                var row = rawRows[i];
                bool isBlank = true;
                foreach (var cell in row)
                    if (!string.IsNullOrWhiteSpace(cell)) { isBlank = false; break; }
                if (!isBlank) dataRows.Add(row);
            }

            if (dataRows.Count == 0)
            {
                Debug.LogError($"[DialogueScriptImporter] '{Path.GetFileName(csvPath)}' — every row is empty, nothing to import.");
                return false;
            }

            var nodes = BuildNodesWithIds(dataRows, out var choicesRaw);
            DialogueScriptLinkResolver.ResolveAll(nodes, choicesRaw);

            var startNode = new StartNode
            {
                nodeId = GenerateId(),
                choices = { new DialogueChoice { choiceText = "", nextNodeId = nodes[0].nodeId } }
            };

            DialogueScriptLayoutEngine.Layout(startNode, nodes);

            graph.TypedNodes.Clear();
            graph.TypedNodes.Add(startNode);
            foreach (var node in nodes)
                graph.TypedNodes.Add(node);
            graph.startNodeId = startNode.nodeId;

            return true;
        }

        private static List<DialogueNodeBase> BuildNodesWithIds(List<string[]> rows, out List<string> choicesRaw)
        {
            var nodes = new List<DialogueNodeBase>();
            choicesRaw = new List<string>();

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                string id = Col(row, 0).Trim();
                string type = Col(row, 1);
                string speaker = Col(row, 2);
                string text = Col(row, 3);

                var node = DialogueScriptNodeBuilder.Build(type, speaker, text);
                node.nodeId = string.IsNullOrEmpty(id) ? $"row{i}_{GenerateId()}" : id;

                nodes.Add(node);
                choicesRaw.Add(Col(row, 4));
            }

            return nodes;
        }

        private static string Col(string[] row, int index) => index < row.Length ? row[index] : "";

        private static string GenerateId() => Guid.NewGuid().ToString("N").Substring(0, 8);

        private static DialogueGraph CreateNewGraphAsset(string defaultName)
        {
            var savePath = EditorUtility.SaveFilePanelInProject("Save Imported Dialogue Graph", defaultName, "asset", "Choose where to save the imported graph");
            if (string.IsNullOrEmpty(savePath)) return null;

            var graph = ScriptableObject.CreateInstance<DialogueGraph>();
            AssetDatabase.CreateAsset(graph, savePath);
            return graph;
        }

        /// <summary>Converts an absolute OS path into an "Assets/..." path, or null if it's outside this project.</summary>
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
    }
}