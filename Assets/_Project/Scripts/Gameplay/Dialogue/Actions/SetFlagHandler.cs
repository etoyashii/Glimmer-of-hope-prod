namespace GlimmerOfHope.Gameplay.Dialogue.Actions
{
    public class SetFlagHandler : IDialogueActionHandler
    {
        private readonly FlagManager _flagManager;

        public DialogueActionType HandledType => DialogueActionType.SetFlag;

        public SetFlagHandler(FlagManager flagManager)
        {
            _flagManager = flagManager;
        }

        public void Execute(string parameter, float delay)
        {
            if (_flagManager == null || string.IsNullOrEmpty(parameter)) return;

            _flagManager.SetFlag(parameter);
        }
    }
}
