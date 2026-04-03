using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GlimmerOfHope.Gameplay.Dialogue;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public class GraphSerializer
    {
        #region Private Fields

        private readonly DialogueGraphView _graphView;
        private readonly GraphSaveHelper _saveHelper;

        #endregion

        #region Constructor

        public GraphSerializer(DialogueGraphView graphView)
        {
            _graphView = graphView;
            _saveHelper = new GraphSaveHelper(graphView);
        }

        #endregion

        #region Public Methods — Load

        public void LoadConversation(ConversationSO conversation)
        {
            _graphView.ClearGraph();

            if (conversation == null)
                return;

            var allLangData = LocalizationBridge.LoadAllLanguages(conversation.ConversationId);
            var startNode = NodeFactory.CreateStartNode(conversation);
            _graphView.AddElement(startNode);

            var lineNodes = CreateLineNodes(conversation, allLangData);
            var endNode = NodeFactory.CreateEndNode();
            _graphView.AddElement(endNode);

            var layout = LoadLayout(conversation.ConversationId);
            ApplyPositions(startNode, lineNodes, endNode, layout);
            CreateEdges(conversation, startNode, lineNodes, endNode);
        }

        #endregion

        #region Public Methods — Save

        public bool SaveConversation(ConversationSO conversation)
        {
            return _saveHelper.Save(conversation);
        }

        #endregion

        #region Private Methods — Load Helpers

        private Dictionary<string, DialogueLineNode> CreateLineNodes(
            ConversationSO conversation,
            Dictionary<string, Dictionary<string, string>> allLangData)
        {
            var nodeMap = new Dictionary<string, DialogueLineNode>();

            if (conversation.AllLines == null)
                return nodeMap;

            foreach (var lineSO in conversation.AllLines)
            {
                if (lineSO == null) continue;

                var texts = LocalizationBridge.GetTextsForLine(lineSO.LineId, allLangData);
                var node = NodeFactory.CreateFromSO(lineSO, texts, allLangData);
                _graphView.AddElement(node);
                nodeMap[lineSO.LineId] = node;
            }

            return nodeMap;
        }

        private void CreateEdges(
            ConversationSO conversation,
            ConversationStartNode startNode,
            Dictionary<string, DialogueLineNode> nodeMap,
            ConversationEndNode endNode)
        {
            ConnectStartNode(conversation, startNode, nodeMap);
            ConnectLineNodes(nodeMap, endNode);
        }

        private void ConnectStartNode(
            ConversationSO conversation,
            ConversationStartNode startNode,
            Dictionary<string, DialogueLineNode> nodeMap)
        {
            if (conversation.StartLine == null) return;

            if (nodeMap.TryGetValue(conversation.StartLine.LineId, out var firstNode))
                _graphView.CreateEdge(startNode.OutputPort, firstNode.InputPort);
        }

        private void ConnectLineNodes(
            Dictionary<string, DialogueLineNode> nodeMap,
            ConversationEndNode endNode)
        {
            foreach (var kvp in nodeMap)
            {
                var node = kvp.Value;
                var lineSO = node.LineSO;
                if (lineSO == null) continue;

                ConnectDefaultNext(lineSO, node, nodeMap, endNode);
                ConnectChoices(lineSO, node, nodeMap);
                ConnectConditionals(lineSO, node, nodeMap);
            }
        }

        private void ConnectDefaultNext(
            DialogueLineSO lineSO, DialogueLineNode node,
            Dictionary<string, DialogueLineNode> nodeMap, ConversationEndNode endNode)
        {
            if (lineSO.NextLine != null &&
                nodeMap.TryGetValue(lineSO.NextLine.LineId, out var nextNode))
            {
                _graphView.CreateEdge(node.DefaultOutputPort, nextNode.InputPort);
            }
            else if (lineSO.NextLine == null && !lineSO.HasChoices && !lineSO.HasConditionals)
            {
                _graphView.CreateEdge(node.DefaultOutputPort, endNode.InputPort);
            }
        }

        private void ConnectChoices(
            DialogueLineSO lineSO, DialogueLineNode node,
            Dictionary<string, DialogueLineNode> nodeMap)
        {
            if (!lineSO.HasChoices) return;

            for (int i = 0; i < node.ChoicePorts.Count && i < lineSO.Choices.Length; i++)
            {
                var choice = lineSO.Choices[i];
                if (choice.targetLine != null &&
                    nodeMap.TryGetValue(choice.targetLine.LineId, out var targetNode))
                {
                    _graphView.CreateEdge(node.ChoicePorts[i], targetNode.InputPort);
                }
            }
        }

        private void ConnectConditionals(
            DialogueLineSO lineSO, DialogueLineNode node,
            Dictionary<string, DialogueLineNode> nodeMap)
        {
            if (!lineSO.HasConditionals) return;

            for (int i = 0; i < node.ConditionPorts.Count && i < lineSO.ConditionalNexts.Length; i++)
            {
                var cond = lineSO.ConditionalNexts[i];
                if (cond.gotoLine != null &&
                    nodeMap.TryGetValue(cond.gotoLine.LineId, out var targetNode))
                {
                    _graphView.CreateEdge(node.ConditionPorts[i], targetNode.InputPort);
                }
            }
        }

        #endregion

        #region Private Methods — Layout

        private GraphLayoutData LoadLayout(string conversationId)
        {
            var path = $"{DialogueCSVFormat.CONVERSATIONS_FOLDER}/{conversationId}_layout.asset";
            return AssetDatabase.LoadAssetAtPath<GraphLayoutData>(path);
        }

        private void ApplyPositions(
            ConversationStartNode startNode,
            Dictionary<string, DialogueLineNode> lineNodes,
            ConversationEndNode endNode,
            GraphLayoutData layout)
        {
            if (layout == null || layout.NodeLayouts == null)
            {
                NodeFactory.AutoLayout(startNode, lineNodes.Values.ToList(), endNode);
                return;
            }

            foreach (var entry in layout.NodeLayouts)
            {
                if (entry.nodeId == "__START__" && startNode != null)
                    startNode.SetPosition(new Rect(entry.position, Vector2.zero));
                else if (entry.nodeId == "__END__" && endNode != null)
                    endNode.SetPosition(new Rect(entry.position, Vector2.zero));
                else if (lineNodes.TryGetValue(entry.nodeId, out var node))
                    node.SetPosition(new Rect(entry.position, Vector2.zero));
            }
        }

        #endregion
    }
}
