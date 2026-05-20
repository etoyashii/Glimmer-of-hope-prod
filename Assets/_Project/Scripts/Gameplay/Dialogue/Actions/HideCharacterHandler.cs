using UnityEngine;
using GlimmerOfHope.Core.Events;

namespace GlimmerOfHope.Gameplay.Dialogue.Actions
{
    public class HideCharacterHandler : IDialogueActionHandler
    {
        private readonly StringEventChannel _eventChannel;

        public DialogueActionType HandledType => DialogueActionType.HideCharacter;

        public HideCharacterHandler(StringEventChannel eventChannel)
        {
            _eventChannel = eventChannel;
        }

        public void Execute(string parameter, float delay)
        {
            if (_eventChannel == null || string.IsNullOrEmpty(parameter)) return;

            _eventChannel.Raise(parameter);
            Debug.Log($"[HideCharacter] Hiding: {parameter}");
        }
    }
}
