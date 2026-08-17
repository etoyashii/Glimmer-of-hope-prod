namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// Pure logic for Condition (If) and Action nodes — no state, no instance,
    /// just functions that read/write flags or call custom registered functions.
    /// </summary>
    public static class DialogueLogicEvaluator
    {
        public static bool EvaluateCondition(ConditionNode node)
        {
            return node.conditionType switch
            {
                DialogueConditionType.Flag => DialogueFlags.Get(node.conditionFlagName) == node.conditionExpectedValue,
                DialogueConditionType.ScriptQuery => DialogueConditions.Evaluate(node.conditionScriptId),
                _ => false
            };
        }

        public static void ExecuteAction(ActionNode node)
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
    }
}
