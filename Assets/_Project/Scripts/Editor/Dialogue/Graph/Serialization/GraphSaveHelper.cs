using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using GlimmerOfHope.Gameplay.Dialogue;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public class GraphSaveHelper
    {
        #region Private Fields

        private readonly DialogueGraphView _graphView;

        #endregion

        #region Constructor

        public GraphSaveHelper(DialogueGraphView graphView)
        {
            _graphView = graphView;
        }

        #endregion

        #region Public Methods

        public bool Save(ConversationSO conversation)
        {
            if (conversation == null)
                return false;

            var validator = new GraphValidator(_graphView);
            var validation = validator.Validate();

            if (!validation.IsValid)
            {
                var errorMsg = string.Join("\n", validation.Errors);
                Debug.LogError($"[DialogueGraph] Validation failed:\n{errorMsg}");
                EditorUtility.DisplayDialog("Erreurs de Validation", errorMsg, "OK");
                return false;
            }

            foreach (var warning in validation.Warnings)
                Debug.LogWarning($"[DialogueGraph] {warning}");

            var lineNodes = _graphView.GetLineNodes();
            var startNode = _graphView.GetStartNode();

            UpdateConversationSO(conversation, startNode, lineNodes);
            SaveLineAssets(lineNodes, conversation.ConversationId);
            SaveLocalizationData(lineNodes, conversation.ConversationId);
            SaveLayout(conversation.ConversationId);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[DialogueGraph] Saved: {lineNodes.Count} lines for '{conversation.ConversationId}'");
            return true;
        }

        #endregion

        #region Private Methods — Conversation

        private void UpdateConversationSO(
            ConversationSO conversation,
            ConversationStartNode startNode,
            List<DialogueLineNode> lineNodes)
        {
            var so = new SerializedObject(conversation);

            var startLineNode = GetConnectedLineNode(startNode?.OutputPort);
            if (startLineNode != null)
                so.FindProperty("_startLine").objectReferenceValue = startLineNode.LineSO;

            var allLines = lineNodes.Where(n => n.LineSO != null).Select(n => n.LineSO).ToArray();
            var allLinesProp = so.FindProperty("_allLines");
            allLinesProp.arraySize = allLines.Length;

            for (int i = 0; i < allLines.Length; i++)
                allLinesProp.GetArrayElementAtIndex(i).objectReferenceValue = allLines[i];

            if (startNode != null)
            {
                so.FindProperty("_conversationId").stringValue = startNode.ConversationId;
                so.FindProperty("_displayName").stringValue = startNode.DisplayName;
                so.FindProperty("_type").enumValueIndex = (int)startNode.ConversationType;
            }

            so.ApplyModifiedProperties();
        }

        #endregion

        #region Private Methods — Lines

        private void SaveLineAssets(List<DialogueLineNode> lineNodes, string conversationId)
        {
            foreach (var node in lineNodes)
            {
                if (node.LineSO == null)
                    CreateNewLineSO(node, conversationId);
                else
                    UpdateLineSO(node);
            }
        }

        private void CreateNewLineSO(DialogueLineNode node, string conversationId)
        {
            var path = $"{DialogueCSVFormat.LINES_FOLDER}/{node.LineId}.asset";
            var lineSO = ScriptableObject.CreateInstance<DialogueLineSO>();
            AssetDatabase.CreateAsset(lineSO, path);

            SetPrivateField(lineSO, "_lineId", node.LineId);
            SetPrivateField(lineSO, "_conversationId", conversationId);
            EditorUtility.SetDirty(lineSO);

            node.LinkSO(lineSO);
        }

        private void UpdateLineSO(DialogueLineNode node)
        {
            var so = new SerializedObject(node.LineSO);

            var nextNode = GetConnectedLineNode(node.DefaultOutputPort);
            so.FindProperty("_nextLine").objectReferenceValue = nextNode?.LineSO;

            UpdateChoiceReferences(so, node);
            UpdateConditionalReferences(so, node);

            so.ApplyModifiedProperties();
        }

        private void UpdateChoiceReferences(SerializedObject so, DialogueLineNode node)
        {
            var choices = so.FindProperty("_choices");
            if (choices == null) return;

            for (int i = 0; i < choices.arraySize && i < node.ChoicePorts.Count; i++)
            {
                var target = GetConnectedLineNode(node.ChoicePorts[i]);
                choices.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("targetLine")
                    .objectReferenceValue = target?.LineSO;
            }
        }

        private void UpdateConditionalReferences(SerializedObject so, DialogueLineNode node)
        {
            var conds = so.FindProperty("_conditionalNexts");
            if (conds == null) return;

            for (int i = 0; i < conds.arraySize && i < node.ConditionPorts.Count; i++)
            {
                var target = GetConnectedLineNode(node.ConditionPorts[i]);
                conds.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("gotoLine")
                    .objectReferenceValue = target?.LineSO;
            }
        }

        #endregion

        #region Private Methods — Localization & Layout

        private void SaveLocalizationData(List<DialogueLineNode> lineNodes, string conversationId)
        {
            var dataPerLang = new Dictionary<string, Dictionary<string, string>>();

            foreach (var lang in DialogueCSVFormat.LANGUAGES)
                dataPerLang[lang] = new Dictionary<string, string>();

            foreach (var node in lineNodes)
            {
                foreach (var lang in DialogueCSVFormat.LANGUAGES)
                {
                    var text = node.GetLocalizedText(lang);
                    if (!string.IsNullOrEmpty(text))
                        dataPerLang[lang][node.LineId] = text;
                }

                node.CollectChoiceLocData(dataPerLang);
            }

            LocalizationBridge.SaveAllLanguages(conversationId, dataPerLang);
        }

        private void SaveLayout(string conversationId)
        {
            var path = $"{DialogueCSVFormat.CONVERSATIONS_FOLDER}/{conversationId}_layout.asset";
            var layout = AssetDatabase.LoadAssetAtPath<GraphLayoutData>(path);

            if (layout == null)
            {
                layout = ScriptableObject.CreateInstance<GraphLayoutData>();
                AssetDatabase.CreateAsset(layout, path);
            }

            var entries = BuildLayoutEntries();
            var viewTransform = _graphView.viewTransform;
            layout.Save(conversationId, viewTransform.position, viewTransform.scale, entries);
            EditorUtility.SetDirty(layout);
        }

        private NodeLayoutEntry[] BuildLayoutEntries()
        {
            var entries = new List<NodeLayoutEntry>();

            AddNodeLayout(entries, _graphView.GetStartNode(), "__START__");

            foreach (var node in _graphView.GetLineNodes())
                AddNodeLayout(entries, node, node.LineId);

            AddNodeLayout(entries, _graphView.GetEndNode(), "__END__");

            return entries.ToArray();
        }

        private void AddNodeLayout(List<NodeLayoutEntry> entries, Node node, string id)
        {
            if (node == null) return;

            entries.Add(new NodeLayoutEntry
            {
                nodeId = id,
                position = node.GetPosition().position
            });
        }

        #endregion

        #region Private Methods — Utilities

        private DialogueLineNode GetConnectedLineNode(Port outputPort)
        {
            if (outputPort == null || !outputPort.connected)
                return null;

            var edge = outputPort.connections.FirstOrDefault();
            return edge?.input?.node as DialogueLineNode;
        }

        private void SetPrivateField<T>(object obj, string fieldName, T value)
        {
            var field = obj.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        #endregion
    }
}
