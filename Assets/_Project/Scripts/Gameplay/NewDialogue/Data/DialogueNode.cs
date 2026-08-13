using System;
using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// One entry in a dialogue graph. All node types share this same class , easier to serialize and render in the custom Inspector,
    /// Fields are grouped by type below, each section says which node type it's for.
    /// </summary>
    [Serializable]
    public class DialogueNode
    {
        #region Identity

        [Tooltip("Unique node ID (auto-generated if left empty).")]
        public string nodeId;

        [Tooltip("Node type. Start is unique per graph and managed automatically.")]
        public DialogueNodeType nodeType = DialogueNodeType.Dialogue;

        #endregion

        #region Dialogue Content

        [Header("Content (Dialogue only)")]
        [Tooltip("Speaker ID (must match a DialogueSpeaker.speakerId present in the scene if followSpeaker = true).")]
        public string speakerId;

        [Tooltip("Text shown in the bubble.")]
        [TextArea(2, 5)]
        public string text;

        [Header("Bubble (Dialogue only)")]
        [Tooltip("Bubble prefab for this line. Needs a component implementing IDialogueBubble.")]
        public GameObject bubblePrefab;

        [Tooltip("TRUE = bubble floats above the speaker (world space). FALSE = fixed UI bubble on screen.")]
        public bool followSpeaker;

        [Tooltip("Offset from the speaker's Transform (used when followSpeaker = true).")]
        public Vector3 bubbleOffset = new Vector3(0f, 2f, 0f);

        [Header("Text Display (Dialogue only)")]
        [Tooltip("If checked, the text types out letter by letter instead of appearing instantly.")]
        public bool useTypewriter;

        [Tooltip("Typing speed in characters per second (used when useTypewriter = true).")]
        public float typewriterCharsPerSecond = 30f;

        [Tooltip("Unchecked = simple auto-continue (Continue button, no visible choice). Checked = show real choices to the player.")]
        public bool hasChoices;

        #endregion

        #region Start Trigger

        [Header("Trigger (Start only)")]
        [Tooltip("How this dialogue can be started.")]
        public DialogueTriggerType triggerType = DialogueTriggerType.ScriptCall;

        [Tooltip("Offset of the floating button above the NPC's Transform (used if Trigger = Floating Button).")]
        public Vector3 buttonOffset = new Vector3(0f, 2f, 0f);

        [Tooltip("Trigger zone radius (used if Trigger = Trigger Zone, acts as a default for the scene collider).")]
        public float triggerZoneRadius = 2f;

        #endregion

        #region End Outcome

        [Header("Ending (End only)")]
        [Tooltip("Optional ID for this specific ending (e.g. 'quest_accepted', 'declined'). Lets a script know which ending was reached.")]
        public string outcomeId;

        #endregion

        #region Gate Trigger

        [Header("Unlock Trigger (Gate only)")]
        public DialogueGateTriggerType gateTriggerType = DialogueGateTriggerType.ScriptEvent;

        [Tooltip("ID passed to DialogueManager.Instance.NotifyGateEvent(id) to unlock this node (Script Event mode).")]
        public string gateEventId;

        [Tooltip("Seconds to wait before auto-unlocking (Timer mode).")]
        public float gateTimerSeconds = 1f;

        [Tooltip("Flag to watch for (Flag mode).")]
        public string gateFlagName;

        [Tooltip("Value the flag needs to reach for this to unlock (Flag mode).")]
        public bool gateFlagExpectedValue = true;

        #endregion

        #region Condition

        [Header("Condition (If only)")]
        public DialogueConditionType conditionType = DialogueConditionType.Flag;

        [Tooltip("Flag to check (Flag mode).")]
        public string conditionFlagName;

        [Tooltip("Expected flag value (Flag mode).")]
        public bool conditionExpectedValue = true;

        [Tooltip("ID of the function registered with DialogueConditions.Register (Script Query mode).")]
        public string conditionScriptId;

        #endregion

        #region Action

        [Header("Action (Action only)")]
        public DialogueActionType actionType = DialogueActionType.SetFlag;

        [Tooltip("Flag to set (Set Flag mode).")]
        public string actionFlagName;

        [Tooltip("Value to assign to the flag (Set Flag mode).")]
        public bool actionFlagValue = true;

        [Tooltip("ID of the function registered with DialogueActions.Register (Script Action mode).")]
        public string actionScriptId;

        #endregion

        #region Links

        [Header("What Comes Next")]
        [Tooltip("Choices / links to the next node(s). Empty = end of dialogue. A single choice with no text = automatic continue.")]
        public List<DialogueChoice> choices = new List<DialogueChoice>();

        [HideInInspector]
        public Vector2 editorPosition = new Vector2(100f, 100f);

        #endregion

        #region Public Properties

        public bool IsStart => nodeType == DialogueNodeType.Start;
        public bool IsEnd => nodeType == DialogueNodeType.End;
        public bool IsGate => nodeType == DialogueNodeType.Gate;
        public bool IsCondition => nodeType == DialogueNodeType.Condition;
        public bool IsAction => nodeType == DialogueNodeType.Action;

        #endregion

        #region Public Methods

        public bool IsSimpleContinuation()
        {
            return nodeType == DialogueNodeType.Dialogue && !hasChoices;
        }

        public bool IsEndOfDialogue()
        {
            return nodeType == DialogueNodeType.End || choices == null || choices.Count == 0;
        }

        //Reads the nextNodeId of a given choice, safely handling out-of-range indices
        public string GetNextNodeId(int choiceIndex = 0)
        {
            return choiceIndex >= 0 && choiceIndex < choices.Count
                ? choices[choiceIndex].nextNodeId
                : null;
        }

        #endregion
    }
}
