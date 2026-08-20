using System.Collections.Generic;
using GlimmerOfHope.Gameplay.NewDialogue;

namespace GlimmerOfHope.Editor.NewDialogue
{
    /// <summary>
    /// Fills in the choices/links for every node, in one pass over the sheet's rows.
    /// Empty Choices = auto-continue to the row directly below (or an implicit end if it's
    /// the last row). Condition nodes always need exactly two explicit targets.
    /// </summary>
    public static class DialogueScriptLinkResolver
    {
        public static void ResolveAll(List<DialogueNodeBase> nodesInOrder, List<string> choicesRaw)
        {
            for (int i = 0; i < nodesInOrder.Count; i++)
            {
                var node = nodesInOrder[i];
                if (node is EndNode) continue;

                var parsed = DialogueScriptChoiceParser.Parse(choicesRaw[i]);
                string nextRowId = i + 1 < nodesInOrder.Count ? nodesInOrder[i + 1].nodeId : "";

                if (node is ConditionNode)
                {
                    ResolveCondition(node, parsed);
                    continue;
                }

                ResolveDefault(node, parsed, nextRowId);
            }
        }

        private static void ResolveCondition(DialogueNodeBase node, List<(string text, string target)> parsed)
        {
            if (parsed.Count < 2)
            {
                UnityEngine.Debug.LogWarning($"[DialogueScriptImporter] Condition row '{node.nodeId}' needs exactly two Choices (true|false); left unlinked.");
                node.choices.Add(new DialogueChoice { choiceText = "", nextNodeId = "" });
                node.choices.Add(new DialogueChoice { choiceText = "", nextNodeId = "" });
                return;
            }

            node.choices.Add(new DialogueChoice { choiceText = "", nextNodeId = parsed[0].target });
            node.choices.Add(new DialogueChoice { choiceText = "", nextNodeId = parsed[1].target });
        }

        private static void ResolveDefault(DialogueNodeBase node, List<(string text, string target)> parsed, string nextRowId)
        {
            if (parsed.Count == 0)
            {
                node.choices.Add(new DialogueChoice { choiceText = "", nextNodeId = nextRowId });
                return;
            }

            bool anyRealChoiceText = false;
            foreach (var (text, target) in parsed)
            {
                node.choices.Add(new DialogueChoice { choiceText = text, nextNodeId = target });
                if (!string.IsNullOrEmpty(text)) anyRealChoiceText = true;
            }

            if (node is DialogueLineNode lineNode)
                lineNode.hasChoices = anyRealChoiceText;
        }
    }
}
