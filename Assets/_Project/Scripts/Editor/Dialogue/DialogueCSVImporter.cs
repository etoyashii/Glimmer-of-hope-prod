using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;

namespace GlimmerOfHope.Editor.Dialogue
{
    /// <summary>
    /// Entry point for CSV dialogue import. Orchestrates the parse -> write assets -> resolve
    /// references -> write localization -> write conversations pipeline. The heavy lifting lives
    /// in DialogueCSVParser, DialogueAssetWriter and DialogueLocalizationWriter.
    /// </summary>
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

            public void AddError(int row, string message) => Errors.Add($"Row {row}: {message}");
            public void AddWarning(int row, string message) => Warnings.Add($"Row {row}: {message}");
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
                var rows = File.ReadAllLines(csvPath, Encoding.UTF8);
                if (rows.Length < 2)
                {
                    result.Errors.Add("CSV must have at least a header row and one data row");
                    return result;
                }

                var parsedLines = new DialogueCSVParser().Parse(rows, result);
                if (result.Errors.Count > 0)
                    return result;

                var writer = new DialogueAssetWriter();
                var lineAssets = writer.WriteLines(parsedLines, result);
                writer.ResolveReferences(lineAssets, parsedLines, result);

                new DialogueLocalizationWriter().Write(parsedLines, result);
                writer.WriteConversations(lineAssets, parsedLines, result);

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
    }
}
