using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using GlimmerOfHope.Gameplay.Dialogue;
using GlimmerOfHope.Core.Localization;

namespace GlimmerOfHope.Editor.Dialogue
{
    public class DialogueCSVImporter
    {
        #region Nested Types

        public class ImportResult
        {
            public bool Success;
            public int LinesCreated;
            public int LinesUpdated;
            public int ConversationsCreated;
            public int LocalizationFilesCreated;
            public List<string> Errors = new();
            public List<string> Warnings = new();

            public void AddError(int row, string message)
            {
                Errors.Add($"Row {row}: {message}");
            }

            public void AddWarning(int row, string message)
            {
                Warnings.Add($"Row {row}: {message}");
            }
        }

        private class ParsedLine
        {
            public string LineId;
            public string ConversationId;
            public int Order;
            public string SpeakerId;
            public EmotionType Emotion;
            public Dictionary<string, string> Texts = new();
            public string NextLineId;
            public List<ParsedChoice> Choices = new();
            public int SourceRow;
        }

        private class ParsedChoice
        {
            public Dictionary<string, string> Texts = new();
            public string TargetLineId;
            public string SetFlag;
        }

        #endregion

        #region Public Methods

        public ImportResult Import(string csvPath)
        {
            var result = new ImportResult();

            if (!File.Exists(csvPath))
            {
                result.Errors.Add($"File not found: {csvPath}");
                return result;
            }

            try
            {
                var lines = File.ReadAllLines(csvPath, Encoding.UTF8);
                if (lines.Length < 2)
                {
                    result.Errors.Add("CSV must have at least a header row and one data row");
                    return result;
                }

                var parsedLines = ParseCSV(lines, result);
                if (result.Errors.Count > 0)
                    return result;

                EnsureFoldersExist();

                var lineAssets = CreateOrUpdateLines(parsedLines, result);
                ResolveReferences(lineAssets, parsedLines, result);
                GenerateLocalizationFiles(parsedLines, result);
                GenerateConversations(lineAssets, parsedLines, result);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                result.Success = result.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Import failed: {ex.Message}");
            }

            return result;
        }

        #endregion

        #region CSV Parsing

        private List<ParsedLine> ParseCSV(string[] lines, ImportResult result)
        {
            var parsed = new List<ParsedLine>();

            for (int i = 1; i < lines.Length; i++)
            {
                var row = lines[i].Trim();
                if (string.IsNullOrEmpty(row))
                    continue;

                var columns = ParseCSVRow(row);
                if (columns.Length < DialogueCSVFormat.MIN_COLUMNS)
                {
                    result.AddError(i + 1, $"Not enough columns ({columns.Length} < {DialogueCSVFormat.MIN_COLUMNS})");
                    continue;
                }

                var line = new ParsedLine
                {
                    SourceRow = i + 1,
                    LineId = columns[DialogueCSVFormat.COL_LINE_ID].Trim(),
                    ConversationId = columns[DialogueCSVFormat.COL_CONV_ID].Trim(),
                    SpeakerId = columns[DialogueCSVFormat.COL_SPEAKER].Trim(),
                    NextLineId = GetColumn(columns, DialogueCSVFormat.COL_NEXT_LINE)
                };

                if (string.IsNullOrEmpty(line.LineId))
                {
                    result.AddError(i + 1, "line_id is required");
                    continue;
                }

                if (string.IsNullOrEmpty(line.ConversationId))
                {
                    result.AddError(i + 1, "conversation_id is required");
                    continue;
                }

                if (!int.TryParse(columns[DialogueCSVFormat.COL_ORDER], out line.Order))
                {
                    result.AddWarning(i + 1, "Invalid order, defaulting to 0");
                    line.Order = 0;
                }

                line.Emotion = ParseEmotion(GetColumn(columns, DialogueCSVFormat.COL_EMOTION));

                line.Texts["fr"] = GetColumn(columns, DialogueCSVFormat.COL_TEXT_FR);
                line.Texts["en"] = GetColumn(columns, DialogueCSVFormat.COL_TEXT_EN);
                line.Texts["es"] = GetColumn(columns, DialogueCSVFormat.COL_TEXT_ES);

                for (int c = 0; c < DialogueCSVFormat.MAX_CHOICES; c++)
                {
                    var choice = ParseChoice(columns, c, i + 1, result);
                    if (choice != null)
                        line.Choices.Add(choice);
                }

                parsed.Add(line);
            }

            return parsed;
        }

        private ParsedChoice ParseChoice(string[] columns, int index, int row, ImportResult result)
        {
            int offset = DialogueCSVFormat.GetChoiceColumnOffset(index);

            string textFr = GetColumn(columns, offset);
            string textEn = GetColumn(columns, offset + 1);
            string target = GetColumn(columns, offset + 2);
            string flag = GetColumn(columns, offset + 3);

            if (string.IsNullOrEmpty(textFr) && string.IsNullOrEmpty(textEn))
                return null;

            return new ParsedChoice
            {
                Texts = new Dictionary<string, string>
                {
                    { "fr", textFr },
                    { "en", textEn }
                },
                TargetLineId = target,
                SetFlag = flag
            };
        }

        private string[] ParseCSVRow(string row)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();

            for (int i = 0; i < row.Length; i++)
            {
                char c = row[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < row.Length && row[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString().Trim());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString().Trim());
            return result.ToArray();
        }

        private string GetColumn(string[] columns, int index)
        {
            return index < columns.Length ? columns[index].Trim() : "";
        }

        private EmotionType ParseEmotion(string value)
        {
            if (string.IsNullOrEmpty(value))
                return EmotionType.Neutral;

            if (Enum.TryParse<EmotionType>(value, true, out var emotion))
                return emotion;

            return EmotionType.Neutral;
        }

        #endregion

        #region Asset Creation

        private void EnsureFoldersExist()
        {
            EnsureFolder(DialogueCSVFormat.LINES_FOLDER);
            EnsureFolder(DialogueCSVFormat.CONVERSATIONS_FOLDER);

            foreach (var lang in DialogueCSVFormat.LANGUAGES)
            {
                EnsureFolder($"{DialogueCSVFormat.LOCALIZATION_FOLDER}/{lang}");
            }
        }

        private void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                var parts = path.Split('/');
                var current = parts[0];

                for (int i = 1; i < parts.Length; i++)
                {
                    var next = current + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(next))
                    {
                        AssetDatabase.CreateFolder(current, parts[i]);
                    }
                    current = next;
                }
            }
        }

        private Dictionary<string, DialogueLineSO> CreateOrUpdateLines(List<ParsedLine> parsedLines, ImportResult result)
        {
            var lineAssets = new Dictionary<string, DialogueLineSO>();
            var characters = LoadCharacters();

            foreach (var parsed in parsedLines)
            {
                var assetPath = $"{DialogueCSVFormat.LINES_FOLDER}/{parsed.LineId}.asset";
                var line = AssetDatabase.LoadAssetAtPath<DialogueLineSO>(assetPath);
                bool isNew = line == null;

                if (isNew)
                {
                    line = ScriptableObject.CreateInstance<DialogueLineSO>();
                    AssetDatabase.CreateAsset(line, assetPath);
                    result.LinesCreated++;
                }
                else
                {
                    result.LinesUpdated++;
                }

                SetPrivateField(line, "_lineId", parsed.LineId);
                SetPrivateField(line, "_conversationId", parsed.ConversationId);
                SetPrivateField(line, "_sequenceOrder", parsed.Order);
                SetPrivateField(line, "_emotion", parsed.Emotion);

                if (!string.IsNullOrEmpty(parsed.SpeakerId) && characters.TryGetValue(parsed.SpeakerId, out var speaker))
                {
                    SetPrivateField(line, "_speaker", speaker);
                }
                else if (!string.IsNullOrEmpty(parsed.SpeakerId))
                {
                    result.AddWarning(parsed.SourceRow, $"Speaker '{parsed.SpeakerId}' not found");
                }

                var choices = new List<DialogueChoice>();
                for (int i = 0; i < parsed.Choices.Count; i++)
                {
                    var pc = parsed.Choices[i];
                    var fallbackText = pc.Texts.ContainsKey("fr") ? pc.Texts["fr"] : "";
                    choices.Add(new DialogueChoice
                    {
                        choiceText = fallbackText,
                        setFlag = pc.SetFlag
                    });
                }
                SetPrivateField(line, "_choices", choices.ToArray());

                EditorUtility.SetDirty(line);
                lineAssets[parsed.LineId] = line;
            }

            return lineAssets;
        }

        private Dictionary<string, CharacterSO> LoadCharacters()
        {
            var result = new Dictionary<string, CharacterSO>(StringComparer.OrdinalIgnoreCase);
            var guids = AssetDatabase.FindAssets("t:CharacterSO", new[] { DialogueCSVFormat.CHARACTERS_FOLDER });

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var character = AssetDatabase.LoadAssetAtPath<CharacterSO>(path);
                if (character != null)
                {
                    var id = GetPrivateField<string>(character, "_characterId");
                    if (!string.IsNullOrEmpty(id))
                        result[id] = character;
                }
            }

            return result;
        }

        #endregion

        #region Reference Resolution

        private void ResolveReferences(Dictionary<string, DialogueLineSO> lineAssets, List<ParsedLine> parsedLines, ImportResult result)
        {
            foreach (var parsed in parsedLines)
            {
                if (!lineAssets.TryGetValue(parsed.LineId, out var line))
                    continue;

                if (!string.IsNullOrEmpty(parsed.NextLineId))
                {
                    if (lineAssets.TryGetValue(parsed.NextLineId, out var nextLine))
                    {
                        SetPrivateField(line, "_nextLine", nextLine);
                    }
                    else
                    {
                        result.AddWarning(parsed.SourceRow, $"NextLine '{parsed.NextLineId}' not found");
                    }
                }

                var choices = GetPrivateField<DialogueChoice[]>(line, "_choices");
                if (choices != null)
                {
                    for (int i = 0; i < choices.Length && i < parsed.Choices.Count; i++)
                    {
                        var targetId = parsed.Choices[i].TargetLineId;
                        if (!string.IsNullOrEmpty(targetId) && lineAssets.TryGetValue(targetId, out var target))
                        {
                            choices[i].targetLine = target;
                        }
                    }
                    SetPrivateField(line, "_choices", choices);
                }

                EditorUtility.SetDirty(line);
            }
        }

        #endregion

        #region Localization Generation

        private void GenerateLocalizationFiles(List<ParsedLine> parsedLines, ImportResult result)
        {
            var byConversation = parsedLines.GroupBy(l => l.ConversationId);

            foreach (var group in byConversation)
            {
                foreach (var lang in DialogueCSVFormat.LANGUAGES)
                {
                    GenerateLocalizationFile(group.Key, lang, group.ToList(), result);
                }
            }
        }

        private void GenerateLocalizationFile(string conversationId, string language, List<ParsedLine> lines, ImportResult result)
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
                {
                    table.entries.Add(new LocalizationEntry { key = line.LineId, value = text });
                }

                for (int i = 0; i < line.Choices.Count; i++)
                {
                    if (line.Choices[i].Texts.TryGetValue(language, out var choiceText) && !string.IsNullOrEmpty(choiceText))
                    {
                        var key = $"{line.LineId}_choice{i + 1}";
                        table.entries.Add(new LocalizationEntry { key = key, value = choiceText });
                    }
                }
            }

            var path = DialogueCSVFormat.GetLocalizationPath(language, conversationId);
            var directory = Path.GetDirectoryName(path);

            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var json = JsonUtility.ToJson(table, true);
            File.WriteAllText(path, json, Encoding.UTF8);
            result.LocalizationFilesCreated++;
        }

        #endregion

        #region Conversation Generation

        private void GenerateConversations(Dictionary<string, DialogueLineSO> lineAssets, List<ParsedLine> parsedLines, ImportResult result)
        {
            var byConversation = parsedLines.GroupBy(l => l.ConversationId);

            foreach (var group in byConversation)
            {
                var convId = group.Key;
                var assetPath = $"{DialogueCSVFormat.CONVERSATIONS_FOLDER}/{convId}.asset";
                var conv = AssetDatabase.LoadAssetAtPath<ConversationSO>(assetPath);
                bool isNew = conv == null;

                if (isNew)
                {
                    conv = ScriptableObject.CreateInstance<ConversationSO>();
                    AssetDatabase.CreateAsset(conv, assetPath);
                    result.ConversationsCreated++;
                }

                var orderedLines = group.OrderBy(l => l.Order).ToList();
                var startLineId = orderedLines.First().LineId;

                SetPrivateField(conv, "_conversationId", convId);
                SetPrivateField(conv, "_displayName", convId);

                if (lineAssets.TryGetValue(startLineId, out var startLine))
                {
                    SetPrivateField(conv, "_startLine", startLine);
                }

                var allLines = orderedLines
                    .Select(l => lineAssets.TryGetValue(l.LineId, out var asset) ? asset : null)
                    .Where(l => l != null)
                    .ToArray();
                SetPrivateField(conv, "_allLines", allLines);

                EditorUtility.SetDirty(conv);
            }
        }

        #endregion

        #region Reflection Helpers

        private void SetPrivateField<T>(object obj, string fieldName, T value)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                field.SetValue(obj, value);
        }

        private T GetPrivateField<T>(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
                return (T)field.GetValue(obj);
            return default;
        }

        #endregion
    }
}
