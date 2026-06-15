using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using GlimmerOfHope.Core.Localization;
using GlimmerOfHope.Editor.Dialogue;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public static class LocalizationBridge
    {
        #region Public Methods — Load

        public static Dictionary<string, Dictionary<string, string>> LoadAllLanguages(string conversationId)
        {
            var result = new Dictionary<string, Dictionary<string, string>>();

            foreach (var lang in DialogueCSVFormat.LANGUAGES)
            {
                result[lang] = LoadLanguage(conversationId, lang);
            }

            return result;
        }

        public static Dictionary<string, string> LoadLanguage(string conversationId, string language)
        {
            var entries = new Dictionary<string, string>();
            var path = GetAbsolutePath(language, conversationId);

            if (!File.Exists(path))
                return entries;

            var json = File.ReadAllText(path, Encoding.UTF8);
            var table = JsonUtility.FromJson<LocalizationTable>(json);

            if (table?.entries == null)
                return entries;

            foreach (var entry in table.entries)
            {
                if (!string.IsNullOrEmpty(entry.key))
                    entries[entry.key] = entry.value;
            }

            return entries;
        }

        public static Dictionary<string, string> GetTextsForLine(
            string lineId,
            Dictionary<string, Dictionary<string, string>> allLangData)
        {
            var result = new Dictionary<string, string>();

            foreach (var lang in DialogueCSVFormat.LANGUAGES)
            {
                if (allLangData.TryGetValue(lang, out var langData) &&
                    langData.TryGetValue(lineId, out var text))
                {
                    result[lang] = text;
                }
                else
                {
                    result[lang] = "";
                }
            }

            return result;
        }

        public static string GetChoiceKey(string lineId, int choiceIndex)
        {
            return $"{lineId}_choice{choiceIndex + 1}";
        }

        #endregion

        #region Public Methods — Save

        public static void SaveAllLanguages(
            string conversationId,
            Dictionary<string, Dictionary<string, string>> dataPerLang)
        {
            foreach (var lang in DialogueCSVFormat.LANGUAGES)
            {
                if (dataPerLang.TryGetValue(lang, out var entries))
                {
                    SaveLanguage(conversationId, lang, entries);
                }
            }
        }

        public static void SaveLanguage(
            string conversationId,
            string language,
            Dictionary<string, string> entries)
        {
            var table = new LocalizationTable
            {
                tableName = $"dialogue_{conversationId}",
                languageCode = language,
                entries = new List<LocalizationEntry>()
            };

            foreach (var kvp in entries)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    table.entries.Add(new LocalizationEntry
                    {
                        key = kvp.Key,
                        value = kvp.Value
                    });
                }
            }

            SortEntries(table.entries);

            var path = GetAbsolutePath(language, conversationId);
            var directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var json = JsonUtility.ToJson(table, true);
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        #endregion

        #region Private Methods

        private static string GetAbsolutePath(string language, string conversationId)
        {
            var relativePath = DialogueCSVFormat.GetLocalizationPath(language, conversationId);
            return Path.Combine(Application.dataPath, "..", relativePath);
        }

        private static void SortEntries(List<LocalizationEntry> entries)
        {
            entries.Sort((a, b) => string.Compare(a.key, b.key, System.StringComparison.Ordinal));
        }

        #endregion
    }
}
