using System;
using UnityEngine;
using UnityEngine.Localization;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// A possible branch out of a DialogueNode. If choiceText is empty, it's not a real choice shown to the player 
    /// </summary>
    [Serializable]
    public class DialogueChoice
    {
        #region Public Properties
        [Tooltip("Text shown as a choice option. Leave empty for a plain 'continue' link (no visible button).")]
        public string choiceText;

        [Tooltip("Localized version of choiceText. Once migration is complete, this replaces the plain field above.")]
        public LocalizedString localizedChoiceText;

        [Tooltip("ID of the next DialogueNode if this choice is picked. Leave empty to end the dialogue here.")]
        public string nextNodeId;

        #endregion
    }
}
