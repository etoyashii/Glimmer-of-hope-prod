using System;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// A possible branch out of a DialogueNode. If choiceText is empty, it's not a real choice shown to the player 
    /// </summary>
    [Serializable]
    public class DialogueChoice
    {
        [Tooltip("Text shown as a choice option. Leave empty for a plain 'continue' link (no visible button).")]
        public string choiceText;

        [Tooltip("ID of the next DialogueNode if this choice is picked. Leave empty to end the dialogue here.")]
        public string nextNodeId;
    }
}
