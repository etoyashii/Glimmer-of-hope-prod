using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using GlimmerOfHope.Core.Localization;
using ParsedLine = GlimmerOfHope.Editor.Dialogue.DialogueCSVParser.ParsedLine;

namespace GlimmerOfHope.Editor.Dialogue
{
    /// <summary>
    /// Writes one localization JSON table per conversation per language into StreamingAssets.
    /// Keyed by line id (and "_choiceN" for choices), so file location of the SO assets is irrelevant here.
    /// </summary>
    public class DialogueLocalizationWriter
    {
        /// <summary>Writes one JSON table per conversation per language (fr/en/es) into StreamingAssets/Localization.</summary>
        public void Write(List<ParsedLine> parsedLines, DialogueCSVImporter.ImportResult result)
        {
            foreach (var group in parsedLines.GroupBy(l => l.ConversationId))
            {
                foreach (var lang in DialogueCSVFormat.LANGUAGES)
                    WriteFile(group.Key, lang, group.ToList(), result);
            }
        }

        private void WriteFile(string conversationId, string language, List<ParsedLine> lines, DialogueCSVImporter.ImportResult result)
        {
            var table = new LocalizationTable
            {
                tableName = $"dialogue_{conversationId}",
                languageCode = language,
                entries = new List<LocalizationEntry>()
            };

            foreach (var line in lines.OrderBy(l => l.Order))
            {
                if (line.Texts.TryGetValue(language, out var text) && !string.IsNullOrEmpty(text))
                    table.entries.Add(new LocalizationEntry { key = line.LineId, value = text });

                for (int i = 0; i < line.Choices.Count; i++)
                {
                    if (line.Choices[i].Texts.TryGetValue(language, out var choiceText) && !string.IsNullOrEmpty(choiceText))
                        table.entries.Add(new LocalizationEntry { key = $"{line.LineId}_choice{i + 1}", value = choiceText });
                }
            }

            var path = DialogueCSVFormat.GetLocalizationPath(language, conversationId);
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, JsonUtility.ToJson(table, true), Encoding.UTF8);
            result.LocalizationFilesCreated++;
        }
    }
}
