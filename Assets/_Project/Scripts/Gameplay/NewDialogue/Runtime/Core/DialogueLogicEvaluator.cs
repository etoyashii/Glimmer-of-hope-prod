
namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// Pure logic for Condition (If) and Action nodes, just functions that read/write flags or call registered functions.
    /// </summary>
    public static class DialogueLogicEvaluator
    {
        #region Public Methods

        public static bool EvaluateCondition(DialogueNode node)
        {
            return node.conditionType switch
            {
                DialogueConditionType.Flag => DialogueFlags.Get(node.conditionFlagName) == node.conditionExpectedValue,
                DialogueConditionType.ScriptQuery => DialogueConditions.Evaluate(node.conditionScriptId),
                _ => false
            };
        }

        public static void ExecuteAction(DialogueNode node)
        {
            switch (node.actionType)
            {
                case DialogueActionType.SetFlag:
                    DialogueFlags.Set(node.actionFlagName, node.actionFlagValue);
                    break;

                case DialogueActionType.ScriptAction:
                    DialogueActions.Invoke(node.actionScriptId);
                    break;
            }
        }

        #endregion
    }
}
