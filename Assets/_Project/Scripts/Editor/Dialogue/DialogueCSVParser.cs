using System;
using System.Collections.Generic;
using System.Text;
using GlimmerOfHope.Gameplay.Dialogue;

namespace GlimmerOfHope.Editor.Dialogue
{
    /// <summary>
    /// Turns raw CSV rows into ParsedLine records. Pure parsing only: no asset or file writing.
    /// Validation problems are pushed onto the shared ImportResult as errors/warnings.
    /// </summary>
    public class DialogueCSVParser
    {
        #region Nested Types

        /// <summary>One dialogue line as read from a CSV row, before it becomes a DialogueLineSO.</summary>
        public class ParsedLine
        {
            public string LineId;
            public string ConversationId;
            public int Order;
            public string SpeakerId;
            public EmotionType Emotion;
            public Dictionary<string, string> Texts = new();
            public string NextLineId;
            public List<ParsedChoice> Choices = new();
            public string Folder;
            public int SourceRow;
        }

        /// <summary>One branching choice attached to a line: localized texts, target line id and an optional flag.</summary>
        public class ParsedChoice
        {
            public Dictionary<string, string> Texts = new();
            public string TargetLineId;
            public string SetFlag;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Parses every data row (skipping the header at index 0) into ParsedLine records.
        /// Rows that are blank or fail validation are skipped and reported on the result.
        /// </summary>
        public List<ParsedLine> Parse(string[] lines, DialogueCSVImporter.ImportResult result)
        {
            var parsed = new List<ParsedLine>();

            for (int i = 1; i < lines.Length; i++)
            {
                var row = lines[i].Trim();
                if (string.IsNullOrEmpty(row))
                    continue;

                var columns = ParseRow(row);
                if (columns.Length < DialogueCSVFormat.MIN_COLUMNS)
                {
                    result.AddError(i + 1, $"Not enough columns ({columns.Length} < {DialogueCSVFormat.MIN_COLUMNS})");
                    continue;
                }

                var line = BuildLine(columns, i + 1, result);
                if (line != null)
                    parsed.Add(line);
            }

            return parsed;
        }

        #endregion

        #region Row Parsing

        /// <summary>Maps one already-split row to a ParsedLine. Returns null (with an error logged) if line_id or conversation_id is missing.</summary>
        private ParsedLine BuildLine(string[] columns, int row, DialogueCSVImporter.ImportResult result)
        {
            var line = new ParsedLine
            {
                SourceRow = row,
                LineId = columns[DialogueCSVFormat.COL_LINE_ID].Trim(),
                ConversationId = columns[DialogueCSVFormat.COL_CONV_ID].Trim(),
                SpeakerId = columns[DialogueCSVFormat.COL_SPEAKER].Trim(),
                NextLineId = GetColumn(columns, DialogueCSVFormat.COL_NEXT_LINE),
                Folder = GetColumn(columns, DialogueCSVFormat.COL_FOLDER)
            };

            if (string.IsNullOrEmpty(line.LineId))
            {
                result.AddError(row, "line_id is required");
                return null;
            }

            if (string.IsNullOrEmpty(line.ConversationId))
            {
                result.AddError(row, "conversation_id is required");
                return null;
            }

            if (!int.TryParse(columns[DialogueCSVFormat.COL_ORDER], out line.Order))
            {
                result.AddWarning(row, "Invalid order, defaulting to 0");
                line.Order = 0;
            }

            line.Emotion = ParseEmotion(GetColumn(columns, DialogueCSVFormat.COL_EMOTION));

            line.Texts["fr"] = GetColumn(columns, DialogueCSVFormat.COL_TEXT_FR);
            line.Texts["en"] = GetColumn(columns, DialogueCSVFormat.COL_TEXT_EN);
            line.Texts["es"] = GetColumn(columns, DialogueCSVFormat.COL_TEXT_ES);

            for (int c = 0; c < DialogueCSVFormat.MAX_CHOICES; c++)
            {
                var choice = ParseChoice(columns, c);
                if (choice != null)
                    line.Choices.Add(choice);
            }

            return line;
        }

        /// <summary>Reads the 4-column block for choice number <paramref name="index"/>. Returns null if that choice slot is empty.</summary>
        private ParsedChoice ParseChoice(string[] columns, int index)
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
                Texts = new Dictionary<string, string> { { "fr", textFr }, { "en", textEn } },
                TargetLineId = target,
                SetFlag = flag
            };
        }

        // Splits one CSV row, honouring quoted fields and "" escapes inside them.
        private string[] ParseRow(string row)
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

            return Enum.TryParse<EmotionType>(value, true, out var emotion) ? emotion : EmotionType.Neutral;
        }

        #endregion
    }
}
