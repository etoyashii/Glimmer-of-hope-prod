namespace GlimmerOfHope.Gameplay.Dialogue.Actions
{
    public interface IDialogueActionHandler
    {
        DialogueActionType HandledType { get; }
        void Execute(string parameter, float delay);
    }
}
