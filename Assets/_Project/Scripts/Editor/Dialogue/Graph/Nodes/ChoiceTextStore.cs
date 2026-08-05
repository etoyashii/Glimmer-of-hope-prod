using System.Collections.Generic;
using UnityEditor;
using GlimmerOfHope.Gameplay.Dialogue;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public class ChoiceTextStore
    {
        #region Private Fields

        private readonly Dictionary<int, Dictionary<string, string>> _texts = new();

        #endregion

        #region Public Methods

        public void Load(
            DialogueLineSO lineSO,
            Dictionary<string, Dictionary<string, string>> allLangData)
        {
            _texts.Clear();

            if (lineSO == null || !lineSO.HasChoices)
                return;

            for (int i = 0; i < lineSO.Choices.Length; i++)
                _texts[i] = LoadOne(lineSO, allLangData, i);
        }

        public void AddEmpty(int index)
        {
            var texts = new Dictionary<string, string>();

            foreach (var lang in DialogueCSVFormat.LANGUAGES)
                texts[lang] = "";

            _texts[index] = texts;
        }

        public void Set(DialogueLineSO lineSO, int index, string language, string text)
        {
            if (!_texts.ContainsKey(index))
                _texts[index] = new Dictionary<string, string>();

            _texts[index][language] = text;

            if (lineSO != null)
                UpdateFallback(lineSO, index, language, text);
        }

        public string Get(int index, string language)
        {
            if (_texts.TryGetValue(index, out var texts) &&
                texts.TryGetValue(language, out var text))
                return text;

            return "";
        }

        public void ShiftAfterRemove(int removedIndex)
        {
            var shifted = new Dictionary<int, Dictionary<string, string>>();

            foreach (var kvp in _texts)
            {
                if (kvp.Key < removedIndex)
                    shifted[kvp.Key] = kvp.Value;
                else if (kvp.Key > removedIndex)
                    shifted[kvp.Key - 1] = kvp.Value;
            }

            _texts.Clear();

            foreach (var kvp in shifted)
                _texts[kvp.Key] = kvp.Value;
        }

        public void Collect(
            string lineId,
            Dictionary<string, Dictionary<string, string>> dataPerLang)
        {
            foreach (var kvp in _texts)
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

        private static Dictionary<string, string> LoadOne(
            DialogueLineSO lineSO,
            Dictionary<string, Dictionary<string, string>> allLangData,
            int index)
        {
            var texts = new Dictionary<string, string>();
            var key = LocalizationBridge.GetChoiceKey(lineSO.LineId, index);

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
                    texts[lang] = lineSO.Choices[index].choiceText;
                }
            }

            return texts;
        }

        private static void UpdateFallback(
            DialogueLineSO lineSO,
            int index,
            string language,
            string text)
        {
            if (language != DialogueCSVFormat.DEFAULT_LANGUAGE)
                return;

            var so = new SerializedObject(lineSO);
            var choices = so.FindProperty("_choices");

            if (index >= choices.arraySize)
                return;

            choices.GetArrayElementAtIndex(index)
                .FindPropertyRelative("choiceText").stringValue = text;

            so.ApplyModifiedProperties();
        }

        #endregion
    }
}
