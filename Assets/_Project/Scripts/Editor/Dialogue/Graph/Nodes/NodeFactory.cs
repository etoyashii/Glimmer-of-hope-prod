using System.Collections.Generic;
using UnityEngine;
using GlimmerOfHope.Gameplay.Dialogue;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public static class NodeFactory
    {
        #region Constants

        private const float NODE_WIDTH = 300f;
        private const float NODE_SPACING_X = 350f;
        private const float NODE_SPACING_Y = 150f;
        private static readonly Vector2 START_NODE_POS = new(100f, 300f);
        private static readonly Vector2 END_NODE_POS = new(1200f, 300f);

        #endregion

        #region Public Methods

        public static ConversationStartNode CreateStartNode(ConversationSO conversation)
        {
            var node = new ConversationStartNode();
            node.Initialize(conversation);
            node.SetPosition(new Rect(START_NODE_POS, Vector2.zero));
            return node;
        }

        public static ConversationEndNode CreateEndNode()
        {
            var node = new ConversationEndNode();
            node.Initialize();
            node.SetPosition(new Rect(END_NODE_POS, Vector2.zero));
            return node;
        }

        public static DialogueLineNode CreateFromSO(
            DialogueLineSO lineSO,
            Dictionary<string, string> localizedTexts,
            Dictionary<string, Dictionary<string, string>> allLangData = null)
        {
            var node = new DialogueLineNode();
            node.Initialize(lineSO, localizedTexts, allLangData);
            return node;
        }

        public static DialogueLineNode CreateEmpty(string lineId, Vector2 position)
        {
            var node = new DialogueLineNode();
            node.InitializeEmpty(lineId);
            node.SetPosition(new Rect(position, Vector2.zero));
            return node;
        }

        public static string GenerateLineId(string conversationId, int index)
        {
            return $"{conversationId}_{index:D2}";
        }

        #endregion

        #region Auto-Layout

        public static void AutoLayout(
            ConversationStartNode startNode,
            List<DialogueLineNode> lineNodes,
            ConversationEndNode endNode)
        {
            if (lineNodes == null || lineNodes.Count == 0)
                return;

            SortBySequenceOrder(lineNodes);
            PositionNodesGrid(startNode, lineNodes, endNode);
        }

        #endregion

        #region Private Methods

        private static void SortBySequenceOrder(List<DialogueLineNode> nodes)
        {
            nodes.Sort((a, b) =>
            {
                int orderA = a.LineSO != null ? a.LineSO.SequenceOrder : 0;
                int orderB = b.LineSO != null ? b.LineSO.SequenceOrder : 0;
                return orderA.CompareTo(orderB);
            });
        }

        private static void PositionNodesGrid(
            ConversationStartNode startNode,
            List<DialogueLineNode> lineNodes,
            ConversationEndNode endNode)
        {
            float startX = START_NODE_POS.x + NODE_SPACING_X;
            float startY = START_NODE_POS.y;

            for (int i = 0; i < lineNodes.Count; i++)
            {
                float x = startX + (i * NODE_SPACING_X);
                float y = startY;

                lineNodes[i].SetPosition(new Rect(x, y, NODE_WIDTH, 0));
            }

            if (endNode != null)
            {
                float endX = startX + (lineNodes.Count * NODE_SPACING_X);
                endNode.SetPosition(new Rect(endX, startY, NODE_WIDTH, 0));
            }
        }

        #endregion
    }
}
