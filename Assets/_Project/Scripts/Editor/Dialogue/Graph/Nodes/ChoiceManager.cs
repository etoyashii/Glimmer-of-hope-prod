using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using GlimmerOfHope.Gameplay.Dialogue;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public class ChoiceManager
    {
        #region Private Fields

        private readonly DialogueLineNode _node;
        private readonly List<Port> _ports;
        private readonly VisualElement _container;
        private readonly Dictionary<int, Dictionary<string, string>> _choiceTexts = new();
        private Action<string> _onChanged;
        private string _activeLanguage = "fr";

        #endregion

        #region Properties

        public IReadOnlyDictionary<int, Dictionary<string, string>> ChoiceTexts => _choiceTexts;

        #endregion

        #region Constructor

        public ChoiceManager(
            DialogueLineNode node,
            List<Port> ports,
            VisualElement container,
            Action<string> onChanged)
        {
            _node = node;
            _ports = ports;
            _container = container;
            _onChanged = onChanged;
        }

        #endregion

        #region Public Methods

        public void LoadChoiceTexts(
            DialogueLineSO lineSO,
            Dictionary<string, Dictionary<string, string>> allLangData)
        {
            _choiceTexts.Clear();

            if (lineSO == null || !lineSO.HasChoices)
                return;

            for (int i = 0; i < lineSO.Choices.Length; i++)
            {
                var texts = new Dictionary<string, string>();
                var key = LocalizationBridge.GetChoiceKey(lineSO.LineId, i);

                foreach (var lang in DialogueCSVFormat.LANGUAGES)
                {
                    if (allLangData != null &&
                        allLangData.TryGetValue(lang, out var langData) &&
                        langData.TryGetValue(key, out var text))
                    {
                        texts[lang] = text;
                    }
                    else
                    {
                        texts[lang] = lineSO.Choices[i].choiceText;
                    }
                }

                _choiceTexts[i] = texts;
            }
        }

        public void RebuildUI(string language)
        {
            _activeLanguage = language;
            ClearPorts();
            _container.Clear();

            if (_node.LineSO != null && _node.LineSO.HasChoices)
            {
                for (int i = 0; i < _node.LineSO.Choices.Length; i++)
                    BuildChoiceRow(i, language);
            }

            BuildAddButton();
            _node.RefreshPorts();
        }

        public void AddChoice()
        {
            var lineSO = _node.LineSO;
            if (lineSO == null) return;

            var so = new SerializedObject(lineSO);
            var choicesProp = so.FindProperty("_choices");
            choicesProp.arraySize++;

            var newChoice = choicesProp.GetArrayElementAtIndex(choicesProp.arraySize - 1);
            newChoice.FindPropertyRelative("choiceText").stringValue = "";
            newChoice.FindPropertyRelative("conditionFlag").stringValue = "";
            newChoice.FindPropertyRelative("setFlag").stringValue = "";
            newChoice.FindPropertyRelative("targetLine").objectReferenceValue = null;
            so.ApplyModifiedProperties();

            int idx = choicesProp.arraySize - 1;
            _choiceTexts[idx] = new Dictionary<string, string>
            {
                { "fr", "" }, { "en", "" }, { "es", "" }
            };

            RebuildUI(_activeLanguage);
        }

        public void RemoveChoice(int index)
        {
            var lineSO = _node.LineSO;
            if (lineSO == null) return;

            var so = new SerializedObject(lineSO);
            var choicesProp = so.FindProperty("_choices");

            if (index >= choicesProp.arraySize) return;

            choicesProp.DeleteArrayElementAtIndex(index);
            so.ApplyModifiedProperties();

            RebuildChoiceTextsAfterRemove(index);
            RebuildUI(_activeLanguage);
        }

        public void SetChoiceText(int index, string language, string text)
        {
            if (!_choiceTexts.ContainsKey(index))
                _choiceTexts[index] = new Dictionary<string, string>();

            _choiceTexts[index][language] = text;

            if (_node.LineSO != null)
                UpdateChoiceFallbackText(index, text, language);
        }

        public string GetChoiceText(int index, string language)
        {
            if (_choiceTexts.TryGetValue(index, out var texts) &&
                texts.TryGetValue(language, out var text))
                return text;

            return "";
        }

        public void CollectLocalizationData(
            string lineId,
            Dictionary<string, Dictionary<string, string>> dataPerLang)
        {
            foreach (var kvp in _choiceTexts)
            {
                var key = LocalizationBridge.GetChoiceKey(lineId, kvp.Key);

                foreach (var lang in DialogueCSVFormat.LANGUAGES)
                {
                    if (kvp.Value.TryGetValue(lang, out var text) && !string.IsNullOrEmpty(text))
                        dataPerLang[lang][key] = text;
                }
            }
        }

        #endregion

        #region Private Methods

        private void BuildChoiceRow(int index, string language)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 2;

            var textField = new TextField();
            textField.value = GetChoiceText(index, language);
            textField.style.flexGrow = 1;
            textField.style.minWidth = 120;
            textField.multiline = false;

            int capturedIndex = index;
            string capturedLang = language;
            textField.RegisterValueChangedCallback(evt =>
            {
                SetChoiceText(capturedIndex, capturedLang, evt.newValue);
                _onChanged?.Invoke(capturedLang);
            });
            row.Add(textField);

            var port = DialoguePortFactory.CreateChoiceOutput(_node, "", index);
            _ports.Add(port);
            row.Add(port);

            var removeBtn = new Button(() => RemoveChoice(capturedIndex)) { text = "X" };
            removeBtn.style.width = 20;
            removeBtn.style.height = 18;
            removeBtn.style.color = new Color(1f, 0.4f, 0.4f);
            removeBtn.style.fontSize = 10;
            row.Add(removeBtn);

            _container.Add(row);
        }

        private void BuildAddButton()
        {
            var addBtn = new Button(AddChoice) { text = "+ Choix" };
            addBtn.style.marginTop = 4;
            addBtn.style.alignSelf = Align.FlexStart;
            addBtn.style.backgroundColor = new Color(0.2f, 0.35f, 0.5f);
            addBtn.style.height = 20;
            addBtn.style.fontSize = 10;
            _container.Add(addBtn);
        }

        private void ClearPorts()
        {
            foreach (var port in _ports)
            {
                foreach (var edge in new List<Edge>(port.connections))
                    edge.RemoveFromHierarchy();
            }
            _ports.Clear();
        }

        private void RebuildChoiceTextsAfterRemove(int removedIndex)
        {
            var newTexts = new Dictionary<int, Dictionary<string, string>>();
            foreach (var kvp in _choiceTexts)
            {
                if (kvp.Key < removedIndex)
                    newTexts[kvp.Key] = kvp.Value;
                else if (kvp.Key > removedIndex)
                    newTexts[kvp.Key - 1] = kvp.Value;
            }
            _choiceTexts.Clear();
            foreach (var kvp in newTexts)
                _choiceTexts[kvp.Key] = kvp.Value;
        }

        private void UpdateChoiceFallbackText(int index, string text, string language)
        {
            if (language != "fr") return;

            var so = new SerializedObject(_node.LineSO);
            var choices = so.FindProperty("_choices");
            if (index < choices.arraySize)
            {
                choices.GetArrayElementAtIndex(index)
                    .FindPropertyRelative("choiceText").stringValue = text;
                so.ApplyModifiedProperties();
            }
        }

        #endregion
    }
}
