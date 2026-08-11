using System;
using System.Collections.Generic;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// Contract the component on a dialogue bubble prefab , DialogueManager only
    /// ever talks to this interface, so any bubble design works as long as it implements it.
    /// </summary>
    public interface IDialogueBubble
    {
        void Initialize(Action onContinue, Action<int> onChoiceSelected);

        void SetText(string text, bool typewriter, float charsPerSecond);

        bool IsRevealingText { get; }

        //Skips straight to the full text, without waiting for the typing animation
        void CompleteTextReveal();

        //Shows real choices to the player. Null/empty means "no choice" 
        void SetChoices(IReadOnlyList<string> labels);

        void Show();
        void Hide();
    }
}
