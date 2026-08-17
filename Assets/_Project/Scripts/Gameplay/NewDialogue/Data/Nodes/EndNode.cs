using System;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    [Serializable]
    public class EndNode : DialogueNodeBase
    {
        public override DialogueNodeType NodeType => DialogueNodeType.End;

        [UnityEngine.Tooltip("Optional ID for this specific ending (e.g. 'quest_accepted', 'declined'). Lets a script know which ending was reached.")]
        public string outcomeId;
    }
}
