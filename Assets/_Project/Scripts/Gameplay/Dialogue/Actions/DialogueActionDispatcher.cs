using System.Collections.Generic;
using UnityEngine;
using GlimmerOfHope.Core.Events;

namespace GlimmerOfHope.Gameplay.Dialogue.Actions
{
    public class DialogueActionDispatcher
    {
        private readonly Dictionary<DialogueActionType, IDialogueActionHandler> _handlers = new();

        public void RegisterHandler(IDialogueActionHandler handler)
        {
            if (handler == null) return;

            if (_handlers.ContainsKey(handler.HandledType))
            {
                Debug.LogWarning($"[ActionDispatcher] Handler for {handler.HandledType} already registered. Replacing.");
            }

            _handlers[handler.HandledType] = handler;
        }

        public void RegisterDefaults(
            FlagManager flagManager,
            StringEventChannel onDialogueEvent,
            StringEventChannel onCharacterShow,
            StringEventChannel onCharacterHide)
        {
            if (flagManager != null)
                RegisterHandler(new SetFlagHandler(flagManager));

            RegisterHandler(new PlaySoundHandler());

            if (onDialogueEvent != null)
                RegisterHandler(new TriggerEventHandler(onDialogueEvent));

            if (onCharacterShow != null)
                RegisterHandler(new ShowCharacterHandler(onCharacterShow));

            if (onCharacterHide != null)
                RegisterHandler(new HideCharacterHandler(onCharacterHide));
        }

        public void ExecuteAll(DialogueAction[] actions)
        {
            if (actions == null) return;

            foreach (var action in actions)
            {
                if (action.type == DialogueActionType.None) continue;

                if (_handlers.TryGetValue(action.type, out var handler))
                {
                    handler.Execute(action.parameter, action.delay);
                }
                else
                {
                    Debug.LogWarning($"[ActionDispatcher] No handler for action type: {action.type}");
                }
            }
        }

        public bool HasHandler(DialogueActionType type)
        {
            return _handlers.ContainsKey(type);
        }
    }
}
