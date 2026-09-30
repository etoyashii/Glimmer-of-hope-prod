using GlimmerOfHope.Gameplay.NewDialogue;

namespace GlimmerOfHope.Editor.NewDialogue
{
    /// <summary>
    /// Adds the default choices a freshly-created node needs (Start/Gate/Action = 1,
    /// Condition = 2, Dialogue = 1 or 2 depending on hasChoices), backed by its own synced String Table entry
    /// </summary>
    public static class DialogueNodeChoiceInitializer
    {
        #region Public Methods
        public static void InitializeDefaultChoices(DialogueNodeBase node, bool hasChoices)
        {
            switch (node)
            {
                case DialogueLineNode:
                    int count = hasChoices ? 2 : 1;
                    for (int i = 0; i < count; i++)
                        AddChoice(node, i);
                    break;

                case GateNode:
                case ActionNode:
                    AddChoice(node, 0);
                    break;

                case ConditionNode:
                    AddChoice(node, 0);
                    AddChoice(node, 1);
                    break;
            }
        }
        #endregion

        #region Private Methods

        private static void AddChoice(DialogueNodeBase node, int index)
        {
            var choice = new DialogueChoice { choiceText = "", nextNodeId = "" };
            DialogueLocalizationSync.CreateEntry(out choice.localizedChoiceText, $"choice_{node.nodeId}_{index}");
            node.choices.Add(choice);
        }
        #endregion
    }
}
