using System;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    [Serializable]
    public class GateNode : DialogueNodeBase
    {
        #region Public Properties
        public override DialogueNodeType NodeType => DialogueNodeType.Gate;

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
    }
}
