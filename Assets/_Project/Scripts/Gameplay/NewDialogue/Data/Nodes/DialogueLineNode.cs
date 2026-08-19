using System;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    [Serializable]
    public class DialogueLineNode : DialogueNodeBase
    {
        #region Public Properties
        public override DialogueNodeType NodeType => DialogueNodeType.Dialogue;

        [Header("Content")]
        [Tooltip("Speaker ID (must match a DialogueSpeaker.speakerId present in the scene if followSpeaker = true).")]
        public string speakerId;

        [Tooltip("Text shown in the bubble.")]
        [TextArea(2, 5)]
        public string text;

        [Header("Bubble")]
        [Tooltip("Bubble prefab for this line. Needs a component implementing IDialogueBubble.")]
        public GameObject bubblePrefab;

        [Tooltip("TRUE = bubble floats above the speaker (world space). FALSE = fixed UI bubble on screen.")]
        public bool followSpeaker;

        [Tooltip("Offset from the speaker's Transform (used when followSpeaker = true).")]
        public Vector3 bubbleOffset = new Vector3(0f, 2f, 0f);

        [Header("Text Display")]
        [Tooltip("If checked, the text types out letter by letter instead of appearing instantly.")]
        public bool useTypewriter;

        [Tooltip("Typing speed in characters per second (used when useTypewriter = true).")]
        public float typewriterCharsPerSecond = 30f;

        [Tooltip("Unchecked = simple auto-continue (Continue button, no visible choice). Checked = show real choices to the player.")]
        public bool hasChoices;
        #endregion

        #region Public Methods
        public override bool IsSimpleContinuation() => !hasChoices;
        #endregion
    }
}
