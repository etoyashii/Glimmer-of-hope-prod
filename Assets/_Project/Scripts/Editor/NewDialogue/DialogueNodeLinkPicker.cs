using System;
using System.Collections.Generic;
using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor.NewDialogue
{
    /// <summary>
    /// Builds readable node labels and draws the "next node"dropdown
    /// Holds a reference to the graph's node list so it doesn't need to be passed every call.
    /// </summary>
    public class DialogueNodeLinkPicker
    {
        #region Private Fields

        private readonly DialogueGraph _graph;

        #endregion

        #region Constructor

        public DialogueNodeLinkPicker(DialogueGraph graph)
        {
            _graph = graph;
        }

        #endregion

        #region Public Methods

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

        //Readable preview of a node 
        public string BuildNodeLabel(DialogueNode node)
        {
            switch (node.nodeType)
            {
                case DialogueNodeType.Dialogue:
                    string preview = string.IsNullOrEmpty(node.text) ? "(empty)" : Truncate(node.text, 30);
                    return $"[Dialogue] {(string.IsNullOrEmpty(node.speakerId) ? "?" : node.speakerId)} : {preview} ({node.nodeId})";
                case DialogueNodeType.Start:
                    return "[START]";
                case DialogueNodeType.End:
                    return string.IsNullOrEmpty(node.outcomeId) ? $"[END] ({node.nodeId})" : $"[END] {node.outcomeId} ({node.nodeId})";
                case DialogueNodeType.Gate:
                    return $"[GATE] ({node.nodeId})";
                case DialogueNodeType.Condition:
                    return $"[IF] ({node.nodeId})";
                case DialogueNodeType.Action:
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

            foreach (var node in _graph.nodes)
            {
                if (node.nodeType == DialogueNodeType.Start) continue; // nothing should link back into Start
                labels.Add(BuildNodeLabel(node));
                ids.Add(node.nodeId);
            }

            return (labels.ToArray(), ids.ToArray());
        }

        #endregion

        #region Helpers

        private static string Truncate(string text, int max) => text.Length <= max ? text : text.Substring(0, max) + "…";

        #endregion
    }
}
