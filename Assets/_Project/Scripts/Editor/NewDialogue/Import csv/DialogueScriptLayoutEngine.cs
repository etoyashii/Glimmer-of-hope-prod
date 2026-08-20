using System.Collections.Generic;
using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEngine;

namespace GlimmerOfHope.Editor.NewDialogue
{
    /// <summary>
    /// Lays out imported nodes by walking the graph from Start (breadth-first), instead of
    /// just spacing them out in CSV row order. Column = distance from Start. Row: a plain
    /// continuation stays on the same line as its parent; a Condition's True branch goes up
    /// and False goes down; any other multi-choice node spreads its targets vertically,
    /// centered on the parent's row.
    /// </summary>
    public static class DialogueScriptLayoutEngine
    {
        private const float ColumnSpacing = 300f;
        private const float RowSpacing = 140f;

        public static void Layout(StartNode start, List<DialogueNodeBase> nodes)
        {
            var byId = new Dictionary<string, DialogueNodeBase>();
            foreach (var node in nodes) byId[node.nodeId] = node;
            byId[start.nodeId] = start;

            var visited = new HashSet<string> { start.nodeId };
            start.editorPosition = Vector2.zero;

            var queue = new Queue<(DialogueNodeBase node, int depth, float y)>();
            queue.Enqueue((start, 0, 0f));

            while (queue.Count > 0)
            {
                var (node, depth, y) = queue.Dequeue();
                PlaceChildren(node, depth, y, byId, visited, queue);
            }

            PlaceOrphans(nodes, visited);
        }

        private static void PlaceChildren(
            DialogueNodeBase node, int depth, float y,
            Dictionary<string, DialogueNodeBase> byId, HashSet<string> visited,
            Queue<(DialogueNodeBase, int, float)> queue)
        {
            bool isCondition = node is ConditionNode;
            int count = node.choices.Count;

            for (int i = 0; i < count; i++)
            {
                string targetId = node.choices[i].nextNodeId;
                if (string.IsNullOrEmpty(targetId)) continue;
                if (!byId.TryGetValue(targetId, out var child)) continue;
                if (!visited.Add(targetId)) continue;

                float childY = ComputeChildY(y, i, count, isCondition);
                child.editorPosition = new Vector2((depth + 1) * ColumnSpacing, childY);
                queue.Enqueue((child, depth + 1, childY));
            }
        }

        private static float ComputeChildY(float parentY, int choiceIndex, int choiceCount, bool isCondition)
        {
            if (isCondition)
                return choiceIndex == 0 ? parentY - RowSpacing : parentY + RowSpacing;

            if (choiceCount <= 1)
                return parentY; // plain continuation: same line as the parent

            float middle = (choiceCount - 1) / 2f;
            return parentY + (choiceIndex - middle) * RowSpacing;
        }

        /// <summary>Nodes never reached from Start (typo'd links, unused branches) get a fallback row below everything else.</summary>
        private static void PlaceOrphans(List<DialogueNodeBase> nodes, HashSet<string> visited)
        {
            float maxY = 0f;
            foreach (var node in nodes)
                if (node.editorPosition.y > maxY) maxY = node.editorPosition.y;

            float orphanY = maxY + RowSpacing * 2f;
            float orphanX = 0f;

            foreach (var node in nodes)
            {
                if (visited.Contains(node.nodeId)) continue;
                node.editorPosition = new Vector2(orphanX, orphanY);
                orphanX += ColumnSpacing;
            }
        }
    }
}
