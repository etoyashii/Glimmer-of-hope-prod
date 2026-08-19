using System;
using System.Collections.Generic;
using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor.NewDialogue
{
    /// <summary>
    /// Custom Inspector for DialogueGraph edits nodes as a foldable list, with dropdowns to
    /// pick the "next" links. Reads field values through SerializedProperty 
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

        #region Private Methods
        private void OnEnable()
        {
            _graph = (DialogueGraph)target;
            _nodesProperty = serializedObject.FindProperty("_typedNodes");

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

            for (int i = 0; i < _graph.TypedNodes.Count; i++)
                DrawNode(i);

            EditorGUILayout.Space();
            DrawAddButtons();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawNode(int index)
        {
            var dataNode = _graph.TypedNodes[index];
            var nodeProperty = _nodesProperty.GetArrayElementAtIndex(index);

            string nodeId = dataNode.nodeId;
            if (!_expandedNodes.ContainsKey(nodeId)) _expandedNodes[nodeId] = dataNode is StartNode;

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.BeginHorizontal();

            _expandedNodes[nodeId] = EditorGUILayout.Foldout(_expandedNodes[nodeId], _linkPicker.BuildNodeLabel(dataNode), true);

            GUI.enabled = !(dataNode is StartNode); // Start is unique and required, never deletable
            if (GUILayout.Button("Delete", GUILayout.Width(80)))
            {
                Undo.RecordObject(_graph, "Delete Dialogue Node");
                _graph.TypedNodes.RemoveAt(index);
                EditorUtility.SetDirty(_graph);
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                serializedObject.Update();
                return;
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            if (_expandedNodes[nodeId])
            {
                EditorGUI.indentLevel++;
                _fieldDrawer.Draw(nodeProperty, dataNode, nodeId);
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
            DialogueNodeBase newNode = type switch
            {
                DialogueNodeType.Dialogue => new DialogueLineNode { hasChoices = hasChoices, text = "New line" },
                DialogueNodeType.Gate => new GateNode(),
                DialogueNodeType.Condition => new ConditionNode(),
                DialogueNodeType.Action => new ActionNode(),
                DialogueNodeType.End => new EndNode(),
                _ => null
            };
            if (newNode == null) return;

            newNode.nodeId = Guid.NewGuid().ToString("N").Substring(0, 8);

            Undo.RecordObject(_graph, "Add Dialogue Node");
            _graph.TypedNodes.Add(newNode);
            EditorUtility.SetDirty(_graph);

            serializedObject.Update();
        }
        #endregion
    }
}
