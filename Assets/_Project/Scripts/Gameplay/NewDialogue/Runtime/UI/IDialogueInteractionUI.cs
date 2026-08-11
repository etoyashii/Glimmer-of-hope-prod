using System;
using System.Collections.Generic;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// used for the interaction panel, always fixed on screen and when the paired text
    /// bubble follows the speaker
    /// </summary>
    public interface IDialogueInteractionUI
    {
        void Initialize(Action onContinue, Action<int> onChoiceSelected);

        //Shows just a "continue" button 
        void ShowContinue();

        //Shows real choices to the player.
        void ShowChoices(IReadOnlyList<string> labels);

        void Hide();
    }
}
