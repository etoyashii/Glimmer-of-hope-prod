namespace GlimmerOfHope.Gameplay.Dialogue
{
    public enum EmotionType
    {
        Neutral,
        Happy,
        Sad,
        Angry,
        Surprised,
        Thoughtful,
        Scared
    }

    public enum ConversationType
    {
        Standard,
        Tutorial,
        Cutscene,
        Optional
    }

    public enum DialogueActionType
    {
        None,
        PlaySound,
        SetFlag,
        TriggerEvent,
        ShowCharacter,
        HideCharacter,
        Spawn,
        Despawn
    }
}
