using System;
using System.Collections.Generic;
using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor.NewDialogue 
{
    /// <summary>
    /// Custom Inspector for Dialogue edits nodes as a list, with dropdowns to pick the "next" links. 
    /// This file only handles the main loop and adding/removing nodes
    /// the field drawing is delegated to DialogueNodeFieldDrawer and DialogueNodeLinkPicker
    /// </summary>
    [CustomEditor(typeof(DialogueGraph))]
    public class DialogueGraphInspector : UnityEditor.Editor
    {
        #region Private Fields

        private DialogueGraph _graph;
        private SerializedProperty _nodesProperty;
        private readonly Dictionary<string, bool> _expandedNodes = new Dictionary<string, bool>();

        private DialogueNodeLinkPicker _linkPicker;
        private DialogueNodeFieldDrawer _fieldDrawer;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            _graph = (DialogueGraph)target;
            _nodesProperty = serializedObject.FindProperty("nodes");

            _linkPicker = new DialogueNodeLinkPicker(_graph);
            _fieldDrawer = new DialogueNodeFieldDrawer(_linkPicker);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Graph ID", _graph.graphId);
            EditorGUILayout.HelpBox(
                "List-based editing. The 'Next / True / False' dropdowns pick the target node among the ones that already exist.",
                MessageType.None);
            EditorGUILayout.Space();

            for (int i = 0; i < _nodesProperty.arraySize; i++)
                DrawNode(i);

            EditorGUILayout.Space();
            DrawAddButtons();

            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Private Methods

        private void DrawNode(int index)
        {
            var nodeProperty = _nodesProperty.GetArrayElementAtIndex(index);
            var nodeIdProperty = nodeProperty.FindPropertyRelative("nodeId");
            var nodeTypeProperty = nodeProperty.FindPropertyRelative("nodeType");
            var type = (DialogueNodeType)nodeTypeProperty.enumValueIndex;

            string nodeId = nodeIdProperty.stringValue;
            if (!_expandedNodes.ContainsKey(nodeId)) _expandedNodes[nodeId] = type == DialogueNodeType.Start;

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();

            _expandedNodes[nodeId] = EditorGUILayout.Foldout(_expandedNodes[nodeId], _linkPicker.BuildNodeLabel(_graph.nodes[index]), true);

            GUI.enabled = type != DialogueNodeType.Start; // Start is unique and required, never deletable
            if (GUILayout.Button("Delete", GUILayout.Width(80)))
            {
                _nodesProperty.DeleteArrayElementAtIndex(index);
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            if (_expandedNodes[nodeId])
            {
                EditorGUI.indentLevel++;
                _fieldDrawer.Draw(nodeProperty, type, nodeId);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAddButtons()
        {
            EditorGUILayout.LabelField("Add Node", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Dialogue (simple)")) AddNode(DialogueNodeType.Dialogue, hasChoices: false);
            if (GUILayout.Button("+ Dialogue (choices)")) AddNode(DialogueNodeType.Dialogue, hasChoices: true);
            if (GUILayout.Button("+ Gate")) AddNode(DialogueNodeType.Gate, hasChoices: false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Condition (If)")) AddNode(DialogueNodeType.Condition, hasChoices: false);
            if (GUILayout.Button("+ Action")) AddNode(DialogueNodeType.Action, hasChoices: false);
            if (GUILayout.Button("+ End")) AddNode(DialogueNodeType.End, hasChoices: false);
            EditorGUILayout.EndHorizontal();
        }

        private void AddNode(DialogueNodeType type, bool hasChoices)
        {
            var newNode = new DialogueNode
            {
                nodeId = Guid.NewGuid().ToString("N").Substring(0, 8),
                nodeType = type,
                hasChoices = hasChoices,
                text = type == DialogueNodeType.Dialogue ? "New line" : ""
            };

            Undo.RecordObject(_graph, "Add Dialogue Node");
            _graph.nodes.Add(newNode);
            EditorUtility.SetDirty(_graph);

            // Resyncs the SerializedObject with the change made.
            serializedObject.Update();
        }

        #endregion
    }
}
