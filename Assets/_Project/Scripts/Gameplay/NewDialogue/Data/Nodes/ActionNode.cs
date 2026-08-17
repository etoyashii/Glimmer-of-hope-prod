using System;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    [Serializable]
    public class ActionNode : DialogueNodeBase
    {
        public override DialogueNodeType NodeType => DialogueNodeType.Action;

        public DialogueActionType actionType = DialogueActionType.SetFlag;

        [UnityEngine.Tooltip("Flag to set (Set Flag mode).")]
        public string actionFlagName;

        [UnityEngine.Tooltip("Value to assign to the flag (Set Flag mode).")]
        public bool actionFlagValue = true;

        [UnityEngine.Tooltip("ID of the function registered with DialogueActions.Register (Script Action mode).")]
        public string actionScriptId;
    }
}
