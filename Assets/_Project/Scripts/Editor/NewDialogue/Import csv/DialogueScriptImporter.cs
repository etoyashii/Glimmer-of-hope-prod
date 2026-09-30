using System;
using System.Collections.Generic;
using System.IO;
using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor.NewDialogue
{
    /// <summary>
    /// Entry point for the script CSV import. One row per dialogue line, read top to bottom.
    /// </summary>
    public static class DialogueScriptImporter
    {
        #region Public Methods
        //Imports a single CSV into one graph
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

        //Parses one CSV and populates the graph's nodes. Does not save/refresh assets - callers do that themselves.
        public static bool BuildGraphContents(string csvPath, DialogueGraph graph)
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

        public static DialogueGraph CreateNewGraphAsset(string defaultName)
        {
            var savePath = EditorUtility.SaveFilePanelInProject("Save Imported Dialogue Graph", defaultName, "asset", "Choose where to save the imported graph");
            if (string.IsNullOrEmpty(savePath)) return null;

            var graph = ScriptableObject.CreateInstance<DialogueGraph>();
            AssetDatabase.CreateAsset(graph, savePath);
            return graph;
        }
        #endregion

        #region Private Methods
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

                if (node is DialogueLineNode lineNode)
                    DialogueLocalizationSync.CreateEntry(out lineNode.localizedText, $"line_{node.nodeId}", lineNode.text);

                nodes.Add(node);
                choicesRaw.Add(Col(row, 4));
            }

            return nodes;
        }

        private static string Col(string[] row, int index) => index < row.Length ? row[index] : "";

        private static string GenerateId() => Guid.NewGuid().ToString("N").Substring(0, 8);
        #endregion
    }
}