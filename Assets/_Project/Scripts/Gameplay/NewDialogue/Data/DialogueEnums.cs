using UnityEngine;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    #region Public Enums
    public enum DialogueNodeType
    {
        Dialogue,
        Start,
        End,
        Gate,
        Condition,
        Action
    }

    public enum DialogueActionType
    {
        [Tooltip("Sets a flag via DialogueFlags.Set(name, value).")]
        SetFlag,

        [Tooltip("Calls a function registered with DialogueActions.Register(id, () => { ... }).")]
        ScriptAction
    }

    public enum DialogueConditionType
    {
        [Tooltip("True if DialogueFlags.Get(name) == expected value.")]
        Flag,

        [Tooltip("True/false decided by a function registered with DialogueConditions.Register(id, () => bool).")]
        ScriptQuery
    }

    public enum DialogueGateTriggerType
    {
        [Tooltip("Unlocked by DialogueManager.Instance.NotifyGateEvent(id), called from a script, an Animation Event, or a UnityEvent.")]
        ScriptEvent,

        [Tooltip("Unlocks automatically after a fixed delay.")]
        Timer,

        [Tooltip("Unlocked when DialogueFlags.Set(name, value) is called somewhere in the code.")]
        Flag
    }

    public enum DialogueTriggerType
    {
        [Tooltip("A scene collider (DialogueTriggerZone) starts the dialogue when the player walks in.")]
        TriggerZone,

        [Tooltip("A world-space UI button floats above a Transform (DialogueTriggerButton) and starts the dialogue on click.")]
        FloatingButton,

        [Tooltip("No automatic trigger: you call DialogueManager.Instance.StartDialogue(graph) yourself from a script.")]
        ScriptCall
    }
    #endregion
}
