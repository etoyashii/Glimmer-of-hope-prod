using System;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    [Serializable]
    public class StartNode : DialogueNodeBase
    {
        public override DialogueNodeType NodeType => DialogueNodeType.Start;

        [Tooltip("How this dialogue can be started.")]
        public DialogueTriggerType triggerType = DialogueTriggerType.ScriptCall;

        [Tooltip("Offset of the floating button above the NPC's Transform (used if Trigger = Floating Button).")]
        public Vector3 buttonOffset = new Vector3(0f, 2f, 0f);

        [Tooltip("Trigger zone radius (used if Trigger = Trigger Zone, acts as a default for the scene collider).")]
        public float triggerZoneRadius = 2f;
    }
}
