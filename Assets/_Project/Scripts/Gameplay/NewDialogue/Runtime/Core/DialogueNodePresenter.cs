using System.Collections.Generic;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// present a standard Dialogue node: where the text goes, where the choices go,
    /// depending on if the bubble follows the speaker or not. 
    /// </summary>
    public class DialogueNodePresenter
    {
        #region Private Fields

        private readonly DialogueBubblePresenter _bubble;
        private readonly DialogueInteractionPresenter _interaction;

        #endregion

        #region Constructor

        public DialogueNodePresenter(DialogueBubblePresenter bubble, DialogueInteractionPresenter interaction)
        {
            _bubble = bubble;
            _interaction = interaction;
        }

        #endregion

        #region Public Methods

        public void Present(DialogueNode node)
        {
            _bubble.EnsureInstance(node);
            _bubble.Position(node);

            bool hasRealChoices = node.choices.Count > 0 && !node.IsSimpleContinuation();
            var choiceLabels = hasRealChoices ? BuildChoiceLabels(node) : null;

            if (node.followSpeaker)
            {
                // The world-space bubble is never interactive: continue + choices both go through the fixed panel.
                _bubble.SetContent(node, null);

                if (hasRealChoices) _interaction.ShowChoices(choiceLabels);
                else _interaction.ShowContinue();
            }
            else
            {
                // Plain fixed-UI bubble handles everything itself, no need for the separate panel.
                _interaction.Hide();
                _bubble.SetContent(node, choiceLabels);
            }

            _bubble.Show();
        }

        #endregion

        #region Helpers

        private static List<string> BuildChoiceLabels(DialogueNode node)
        {
            var labels = new List<string>(node.choices.Count);
            foreach (var choice in node.choices) labels.Add(choice.choiceText);
            return labels;
        }

        #endregion
    }
}
