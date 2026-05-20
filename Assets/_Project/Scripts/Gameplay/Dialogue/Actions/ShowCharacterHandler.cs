using UnityEngine;
using GlimmerOfHope.Core.Events;

namespace GlimmerOfHope.Gameplay.Dialogue.Actions
{
    public class ShowCharacterHandler : IDialogueActionHandler
    {
        private readonly StringEventChannel _eventChannel;

        public DialogueActionType HandledType => DialogueActionType.ShowCharacter;

        public ShowCharacterHandler(StringEventChannel eventChannel)
        {
            _eventChannel = eventChannel;
        }

        public void Execute(string parameter, float delay)
        {
            if (_eventChannel == null || string.IsNullOrEmpty(parameter)) return;

            _eventChannel.Raise(parameter);
            Debug.Log($"[ShowCharacter] Showing: {parameter}");
        }
    }
}
