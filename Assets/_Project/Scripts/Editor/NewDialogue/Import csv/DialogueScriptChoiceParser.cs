using System;
using System.Collections.Generic;

namespace GlimmerOfHope.Editor.NewDialogue
{
    /// <summary>
    /// Parses the Choices column: "Yes->take|No->end_refuse", or "->target" for a continuation, or "Text->" for a choice that ends the dialogue implicitly.
    /// </summary>
    public static class DialogueScriptChoiceParser
    {
        #region Public Methods
        public static List<(string text, string target)> Parse(string raw)
        {
            var result = new List<(string text, string target)>();
            if (string.IsNullOrWhiteSpace(raw)) return result;

            foreach (var segment in raw.Split('|'))
            {
                var trimmed = segment.Trim();
                if (trimmed.Length == 0) continue;

                int arrowIndex = trimmed.IndexOf("->", StringComparison.Ordinal);
                if (arrowIndex < 0)
                {
                    result.Add(("", trimmed)); // malformed but recoverable: treat as a target
                    continue;
                }

                string text = trimmed.Substring(0, arrowIndex).Trim();
                string target = trimmed.Substring(arrowIndex + 2).Trim();
                result.Add((text, target));
            }

            return result;
        }
        #endregion
    }
}
