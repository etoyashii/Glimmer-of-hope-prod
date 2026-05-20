using System;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.Dialogue
{
    [Serializable]
    public class DialogueAction
    {
        public DialogueActionType type = DialogueActionType.None;
        public string parameter;
        public float delay;
    }

    [Serializable]
    public class DialogueChoice
    {
        [TextArea(1, 2)]
        public string choiceText;
        public DialogueLineSO targetLine;
        public string conditionFlag;
        public string setFlag;
    }

    [Serializable]
    public class ConditionalNext
    {
        [Tooltip("Format: HAS:flag, NOT:flag, HAS:a AND HAS:b")]
        public string condition;
        public DialogueLineSO gotoLine;
    }
}
