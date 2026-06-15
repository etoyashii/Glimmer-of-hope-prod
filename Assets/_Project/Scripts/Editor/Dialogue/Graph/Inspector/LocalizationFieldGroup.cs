using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using GlimmerOfHope.Editor.Dialogue;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public class LocalizationFieldGroup : VisualElement
    {
        #region Constants

        private static readonly Dictionary<string, string> LANG_LABELS = new()
        {
            { "fr", "FR" },
            { "en", "EN" },
            { "es", "ES" }
        };

        #endregion

        #region Private Fields

        private readonly Dictionary<string, TextField> _textFields = new();
        private readonly Dictionary<string, Label> _statusLabels = new();
        private Action<string, string> _onTextChanged;

        #endregion

        #region Constructor

        public LocalizationFieldGroup(string label, Action<string, string> onTextChanged)
        {
            _onTextChanged = onTextChanged;

            AddToClassList("loc-field-group");

            var header = new Label(label);
            header.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            header.style.marginBottom = 4;
            Add(header);

            foreach (var lang in DialogueCSVFormat.LANGUAGES)
            {
                BuildLanguageRow(lang);
            }
        }

        #endregion

        #region Public Methods

        public void SetTexts(Dictionary<string, string> texts)
        {
            foreach (var lang in DialogueCSVFormat.LANGUAGES)
            {
                if (_textFields.TryGetValue(lang, out var field))
                {
                    field.SetValueWithoutNotify(texts.TryGetValue(lang, out var t) ? t : "");
                }

                UpdateStatus(lang);
            }
        }

        public Dictionary<string, string> GetTexts()
        {
            var result = new Dictionary<string, string>();

            foreach (var lang in DialogueCSVFormat.LANGUAGES)
            {
                if (_textFields.TryGetValue(lang, out var field))
                    result[lang] = field.value;
            }

            return result;
        }

        public void SetEnabled(bool enabled)
        {
            foreach (var field in _textFields.Values)
                field.SetEnabled(enabled);
        }

        #endregion

        #region Private Methods

        private void BuildLanguageRow(string lang)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginBottom = 4;
            row.style.alignItems = Align.FlexStart;

            var langLabel = new Label(LANG_LABELS.GetValueOrDefault(lang, lang));
            langLabel.style.width = 30;
            langLabel.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
            langLabel.style.color = new UnityEngine.Color(0.6f, 0.8f, 1f);
            row.Add(langLabel);

            var textField = new TextField();
            textField.multiline = true;
            textField.style.flexGrow = 1;
            textField.style.minHeight = 40;
            textField.RegisterValueChangedCallback(evt => OnFieldChanged(lang, evt.newValue));
            _textFields[lang] = textField;
            row.Add(textField);

            var status = new Label();
            status.style.width = 20;
            status.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
            _statusLabels[lang] = status;
            row.Add(status);

            Add(row);
        }

        private void OnFieldChanged(string lang, string newValue)
        {
            UpdateStatus(lang);
            _onTextChanged?.Invoke(lang, newValue);
        }

        private void UpdateStatus(string lang)
        {
            if (!_statusLabels.TryGetValue(lang, out var label))
                return;

            if (!_textFields.TryGetValue(lang, out var field))
                return;

            bool hasText = !string.IsNullOrWhiteSpace(field.value);
            label.text = hasText ? "OK" : "!!";
            label.style.color = hasText
                ? new UnityEngine.Color(0.4f, 0.8f, 0.4f)
                : new UnityEngine.Color(1f, 0.5f, 0.3f);
        }

        #endregion
    }
}
