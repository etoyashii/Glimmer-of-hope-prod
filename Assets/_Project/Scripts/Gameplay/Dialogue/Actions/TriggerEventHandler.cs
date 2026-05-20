using UnityEngine;
using GlimmerOfHope.Core.Events;

namespace GlimmerOfHope.Gameplay.Dialogue.Actions
{
    public class TriggerEventHandler : IDialogueActionHandler
    {
        private readonly StringEventChannel _eventChannel;

        public DialogueActionType HandledType => DialogueActionType.TriggerEvent;

        public TriggerEventHandler(StringEventChannel eventChannel)
        {
            _eventChannel = eventChannel;
        }

        public void Execute(string parameter, float delay)
        {
            if (_eventChannel == null || string.IsNullOrEmpty(parameter)) return;

            _eventChannel.Raise(parameter);
            Debug.Log($"[TriggerEvent] Raised: {parameter}");
        }
    }
}
