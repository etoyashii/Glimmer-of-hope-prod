using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public class ChoiceLocalizationSection : VisualElement
    {
        #region Private Fields

        private readonly DialogueGraphView _graphView;
        private readonly List<LocalizationFieldGroup> _groups = new();
        private DialogueLineNode _node;
        private int _shownCount = -1;

        #endregion

        #region Constructor

        public ChoiceLocalizationSection(DialogueGraphView graphView)
        {
            _graphView = graphView;
        }

        #endregion

        #region Public Methods

        public bool NeedsRebuild(DialogueLineNode node)
        {
            return node != _node || ChoiceCount(node) != _shownCount;
        }

        public void Populate(DialogueLineNode node)
        {
            _node = node;
            _groups.Clear();
            Clear();

            if (node == null || node.LineSO == null)
            {
                _shownCount = -1;
                return;
            }

            _shownCount = ChoiceCount(node);

            if (_shownCount == 0)
            {
                Add(BuildEmptyLabel());
                return;
            }

            for (int i = 0; i < _shownCount; i++)
                BuildGroup(i);
        }

        public void RefreshTexts()
        {
            if (_node == null || _node.Choices == null)
                return;

            for (int i = 0; i < _groups.Count; i++)
                _groups[i].SetTexts(ReadTexts(i));
        }

        #endregion

        #region Private Methods

        private static int ChoiceCount(DialogueLineNode node)
        {
            var choices = node != null && node.LineSO != null ? node.LineSO.Choices : null;
            return choices != null ? choices.Length : 0;
        }

        private void BuildGroup(int index)
        {
            var group = new LocalizationFieldGroup($"Choix {index + 1}", (lang, text) =>
            {
                _node.Choices.SetChoiceText(index, lang, text);
                _node.UpdateVisuals(_graphView.ActiveLanguage);
            });

            group.SetTexts(ReadTexts(index));

            _groups.Add(group);
            Add(group);
        }

        private Dictionary<string, string> ReadTexts(int index)
        {
            var texts = new Dictionary<string, string>();

            foreach (var lang in DialogueCSVFormat.LANGUAGES)
                texts[lang] = _node.Choices.GetChoiceText(index, lang);

            return texts;
        }

        private static Label BuildEmptyLabel()
        {
            return new Label("Aucun choix")
            {
                style = { color = new Color(0.5f, 0.5f, 0.5f) }
            };
        }

        #endregion
    }
}
