using GlimmerOfHope.Core.Audio;
using GlimmerOfHope.Core.Services;

namespace GlimmerOfHope.Gameplay.Dialogue.Actions
{
    public class PlaySoundHandler : IDialogueActionHandler
    {
        public DialogueActionType HandledType => DialogueActionType.PlaySound;

        public void Execute(string parameter, float delay)
        {
            if (string.IsNullOrEmpty(parameter)) return;

            if (ServiceLocator.TryGet<AudioManager>(out var audio))
            {
                audio.PlaySFX(parameter);
            }
        }
    }
}
