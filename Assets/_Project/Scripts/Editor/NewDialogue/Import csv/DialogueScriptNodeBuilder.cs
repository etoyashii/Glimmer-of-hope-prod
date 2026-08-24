using GlimmerOfHope.Gameplay.NewDialogue;

namespace GlimmerOfHope.Editor.NewDialogue
{
    /// <summary>
    /// Turns one row's Type/Speaker/Text columns into the matching node, with all the type-specific settings on their default values 
    /// </summary>
    public static class DialogueScriptNodeBuilder
    {
        #region Public Methods
        public static DialogueNodeBase Build(string typeRaw, string speaker, string text)
        {
            string type = typeRaw.Trim();

            if (type.Length == 0)
                return new DialogueLineNode { speakerId = speaker, text = text };

            string keyword = type;
            string param = "";
            int colonIndex = type.IndexOf(':');
            if (colonIndex >= 0)
            {
                keyword = type.Substring(0, colonIndex).Trim();
                param = type.Substring(colonIndex + 1).Trim();
            }

            switch (keyword.ToLowerInvariant())
            {
                case "gate":
                    return new GateNode
                    {
                        gateTriggerType = DialogueGateTriggerType.Flag,
                        gateFlagName = param,
                        gateFlagExpectedValue = true
                    };

                case "condition":
                    return new ConditionNode
                    {
                        conditionType = DialogueConditionType.Flag,
                        conditionFlagName = param,
                        conditionExpectedValue = true
                    };

                case "action":
                    return new ActionNode
                    {
                        actionType = DialogueActionType.SetFlag,
                        actionFlagName = param,
                        actionFlagValue = true
                    };

                case "end":
                    return new EndNode { outcomeId = param };

                default:
                    UnityEngine.Debug.LogWarning($"[DialogueScriptImporter] Unknown Type '{type}', treated as a plain dialogue line.");
                    return new DialogueLineNode { speakerId = speaker, text = text };
            }
        }
        #endregion
    }
}
