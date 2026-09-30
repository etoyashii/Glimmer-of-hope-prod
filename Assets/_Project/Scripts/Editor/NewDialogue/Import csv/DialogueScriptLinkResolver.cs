using System.Collections.Generic;
using GlimmerOfHope.Gameplay.NewDialogue;

namespace GlimmerOfHope.Editor.NewDialogue
{
    /// <summary>
    /// Fills in the choices/links for every node
    /// Empty Choices = auto-continue to the row directly below
    /// </summary>
    public static class DialogueScriptLinkResolver
    {
        #region Public Methods
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
        #endregion

        #region Private Methods

        private static void ResolveCondition(DialogueNodeBase node, List<(string text, string target)> parsed)
        {
            if (parsed.Count < 2)
            {
                UnityEngine.Debug.LogWarning($"[DialogueScriptImporter] Condition row '{node.nodeId}' needs exactly two Choices (true|false); left unlinked.");
                AddSyncedChoice(node, "", "");
                AddSyncedChoice(node, "", "");
                return;
            }

            AddSyncedChoice(node, "", parsed[0].target);
            AddSyncedChoice(node, "", parsed[1].target);
        }

        private static void ResolveDefault(DialogueNodeBase node, List<(string text, string target)> parsed, string nextRowId)
        {
            if (parsed.Count == 0)
            {
                AddSyncedChoice(node, "", nextRowId);
                return;
            }

            bool anyRealChoiceText = false;
            foreach (var (text, target) in parsed)
            {
                AddSyncedChoice(node, text, target);
                if (!string.IsNullOrEmpty(text)) anyRealChoiceText = true;
            }

            if (node is DialogueLineNode lineNode)
                lineNode.hasChoices = anyRealChoiceText;
        }

        private static void AddSyncedChoice(DialogueNodeBase node, string text, string target)
        {
            var choice = new DialogueChoice { choiceText = text, nextNodeId = target };
            int index = node.choices.Count;
            DialogueLocalizationSync.CreateEntry(out choice.localizedChoiceText, $"choice_{node.nodeId}_{index}", text);
            node.choices.Add(choice);
        }

        #endregion
    }
}