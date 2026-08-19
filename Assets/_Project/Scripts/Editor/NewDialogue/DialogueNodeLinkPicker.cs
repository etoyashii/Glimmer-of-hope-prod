using System;
using System.Collections.Generic;
using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor.NewDialogue
{
    /// <summary>
    /// Builds readable node labels (for foldouts and dropdowns) and draws the "next node" dropdown. 
    /// </summary>
    public class DialogueNodeLinkPicker
    {
        #region private fields
        private readonly DialogueGraph _graph;
        #endregion

        #region Public Methods

        public DialogueNodeLinkPicker(DialogueGraph graph)
        {
            _graph = graph;
        }

        //Draws a dropdown listing every eligible node for a "next" link
        public void DrawNextDropdown(SerializedProperty choiceProperty, string label, string selfId, params GUILayoutOption[] options)
        {
            var nextProperty = choiceProperty.FindPropertyRelative("nextNodeId");
            var (labels, ids) = BuildOptions();

            int currentIndex = Array.IndexOf(ids, nextProperty.stringValue);
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = EditorGUILayout.Popup(label, currentIndex, labels, options);
            nextProperty.stringValue = ids[newIndex];
        }

        //Readable preview of a node (e.g. "[Dialogue] blacksmith: Hello there... (a3f92e1c)").
        public string BuildNodeLabel(DialogueNodeBase node)
        {
            switch (node)
            {
                case DialogueLineNode lineNode:
                    string preview = string.IsNullOrEmpty(lineNode.text) ? "(empty)" : Truncate(lineNode.text, 30);
                    string speaker = string.IsNullOrEmpty(lineNode.speakerId) ? "?" : lineNode.speakerId;
                    return $"[Dialogue] {speaker} : {preview} ({node.nodeId})";
                case StartNode:
                    return "[START]";
                case EndNode endNode:
                    return string.IsNullOrEmpty(endNode.outcomeId) ? $"[END] ({node.nodeId})" : $"[END] {endNode.outcomeId} ({node.nodeId})";
                case GateNode:
                    return $"[GATE] ({node.nodeId})";
                case ConditionNode:
                    return $"[IF] ({node.nodeId})";
                case ActionNode:
                    return $"[ACTION] ({node.nodeId})";
                default:
                    return node.nodeId;
            }
        }
        #endregion

        #region Private Methods

        private (string[] labels, string[] ids) BuildOptions()
        {
            var labels = new List<string> { "(none / implicit end)" };
            var ids = new List<string> { "" };

            foreach (var node in _graph.TypedNodes)
            {
                if (node is StartNode) continue; // nothing should link back into Start
                labels.Add(BuildNodeLabel(node));
                ids.Add(node.nodeId);
            }

            return (labels.ToArray(), ids.ToArray());
        }

        private static string Truncate(string text, int max) => text.Length <= max ? text : text.Substring(0, max) + "…";

        #endregion

    }
}
