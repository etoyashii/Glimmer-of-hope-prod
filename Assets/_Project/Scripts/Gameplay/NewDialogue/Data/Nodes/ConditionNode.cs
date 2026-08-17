using System;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    [Serializable]
    public class ConditionNode : DialogueNodeBase
    {
        public override DialogueNodeType NodeType => DialogueNodeType.Condition;

        public DialogueConditionType conditionType = DialogueConditionType.Flag;

        [UnityEngine.Tooltip("Flag to check (Flag mode).")]
        public string conditionFlagName;

        [UnityEngine.Tooltip("Expected flag value (Flag mode).")]
        public bool conditionExpectedValue = true;

        [UnityEngine.Tooltip("ID of the function registered with DialogueConditions.Register (Script Query mode).")]
        public string conditionScriptId;
    }
}
