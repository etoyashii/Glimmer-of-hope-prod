using GlimmerOfHope.Editor.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using GlimmerOfHope.Gameplay.Dialogue;
using ParsedLine = GlimmerOfHope.Editor.Dialogue.DialogueCSVParser.ParsedLine;

namespace GlimmerOfHope.Editor.Dialogue
{
    /// <summary>
    /// Creates and updates the Line and Conversation ScriptableObjects from parsed CSV rows.
    ///
    /// Location-agnostic by design: an existing asset is found by its id ANYWHERE under
    /// DIALOGUE_ROOT (recursive search), then updated in place. This is what lets the team
    /// freely sort assets into subfolders — a re-import respects wherever a file was moved
    /// instead of recreating a duplicate at a hard-coded flat path. Only brand-new assets are
    /// created, and they land in the subfolder named by the CSV "folder" column.
    /// </summary>
    public class DialogueAssetWriter
    {
        #region Lines

        /// <summary>
        /// Creates or updates a DialogueLineSO per parsed row and returns them keyed by line id.
        /// Existing assets are matched by id wherever they live; new ones land in the CSV "folder" subfolder.
        /// Reference fields (next line, choice targets) are wired separately by ResolveReferences.
        /// </summary>
        public Dictionary<string, DialogueLineSO> WriteLines(List<ParsedLine> parsedLines, DialogueCSVImporter.ImportResult result)
        {
            var lineAssets = new Dictionary<string, DialogueLineSO>();
            var existing = BuildIndex<DialogueLineSO>("_lineId", result);
            var characters = LoadCharacters();

            foreach (var parsed in parsedLines)
            {
                var line = ResolveOrCreate(existing, parsed.LineId, () =>
                {
                    var folder = DialogueCSVFormat.GetLinesFolder(parsed.Folder);
                    return $"{folder}/{parsed.LineId}.asset";
                }, result, isNew => { if (isNew) result.LinesCreated++; else result.LinesUpdated++; });

                ApplyLineFields(line, parsed, characters, result);
                EditorUtility.SetDirty(line);
                lineAssets[parsed.LineId] = line;
            }

            return lineAssets;
        }

        private void ApplyLineFields(DialogueLineSO line, ParsedLine parsed, Dictionary<string, CharacterSO> characters, DialogueCSVImporter.ImportResult result)
        {
            SetField(line, "_lineId", parsed.LineId);
            SetField(line, "_conversationId", parsed.ConversationId);
            SetField(line, "_sequenceOrder", parsed.Order);
            SetField(line, "_emotion", parsed.Emotion);

            if (!string.IsNullOrEmpty(parsed.SpeakerId) && characters.TryGetValue(parsed.SpeakerId, out var speaker))
                SetField(line, "_speaker", speaker);
            else if (!string.IsNullOrEmpty(parsed.SpeakerId))
                result.AddWarning(parsed.SourceRow, $"Speaker '{parsed.SpeakerId}' not found");

            var choices = parsed.Choices
                .Select(pc => new DialogueChoice
                {
                    choiceText = pc.Texts.TryGetValue("fr", out var fr) ? fr : "",
                    setFlag = pc.SetFlag
                })
                .ToArray();
            SetField(line, "_choices", choices);
        }

        #endregion

        #region Reference Resolution

        /// <summary>
        /// Second pass over the lines: now that every line asset exists, links each line to its
        /// next line and its choice targets by id. Unknown targets are reported as warnings.
        /// </summary>
        public void ResolveReferences(Dictionary<string, DialogueLineSO> lineAssets, List<ParsedLine> parsedLines, DialogueCSVImporter.ImportResult result)
        {
            foreach (var parsed in parsedLines)
            {
                if (!lineAssets.TryGetValue(parsed.LineId, out var line))
                    continue;

                if (!string.IsNullOrEmpty(parsed.NextLineId))
                {
                    if (lineAssets.TryGetValue(parsed.NextLineId, out var nextLine))
                        SetField(line, "_nextLine", nextLine);
                    else
                        result.AddWarning(parsed.SourceRow, $"NextLine '{parsed.NextLineId}' not found");
                }

                var choices = GetField<DialogueChoice[]>(line, "_choices");
                if (choices != null)
                {
                    for (int i = 0; i < choices.Length && i < parsed.Choices.Count; i++)
                    {
                        var targetId = parsed.Choices[i].TargetLineId;
                        if (!string.IsNullOrEmpty(targetId) && lineAssets.TryGetValue(targetId, out var target))
                            choices[i].targetLine = target;
                    }
                    SetField(line, "_choices", choices);
                }

                EditorUtility.SetDirty(line);
            }
        }

        #endregion

        #region Conversations

        /// <summary>
        /// Creates or updates one ConversationSO per conversation id, with its start line and ordered line list.
        /// Matched by id wherever it lives; new ones use the CSV "folder" subfolder. The display name is only
        /// seeded on creation so a manual rename survives re-imports.
        /// </summary>
        public void WriteConversations(Dictionary<string, DialogueLineSO> lineAssets, List<ParsedLine> parsedLines, DialogueCSVImporter.ImportResult result)
        {
            var existing = BuildIndex<ConversationSO>("_conversationId", result);

            foreach (var group in parsedLines.GroupBy(l => l.ConversationId))
            {
                var convId = group.Key;
                var ordered = group.OrderBy(l => l.Order).ToList();

                bool created = false;
                var conv = ResolveOrCreate(existing, convId, () =>
                {
                    var folder = DialogueCSVFormat.GetConversationsFolder(ordered[0].Folder);
                    return $"{folder}/{convId}.asset";
                }, result, isNew => { created = isNew; if (isNew) result.ConversationsCreated++; });

                SetField(conv, "_conversationId", convId);
                // Only seed the display name on creation; a re-import must not clobber a manual rename.
                if (created)
                    SetField(conv, "_displayName", convId);

                if (lineAssets.TryGetValue(ordered[0].LineId, out var startLine))
                    SetField(conv, "_startLine", startLine);

                var allLines = ordered
                    .Select(l => lineAssets.TryGetValue(l.LineId, out var asset) ? asset : null)
                    .Where(l => l != null)
                    .ToArray();
                SetField(conv, "_allLines", allLines);

                EditorUtility.SetDirty(conv);
            }
        }

        #endregion

        #region Asset Lookup

        // Indexes every asset of type T under DIALOGUE_ROOT by its id field, wherever it lives.
        // A duplicate id (a leftover from the old flat-path duplication) is reported, not silently hidden.
        private Dictionary<string, T> BuildIndex<T>(string idField, DialogueCSVImporter.ImportResult result) where T : ScriptableObject
        {
            var map = new Dictionary<string, T>();
            if (!AssetDatabase.IsValidFolder(DialogueCSVFormat.DIALOGUE_ROOT))
                return map;

            var guids = AssetSearch.FindInFolder($"t:{typeof(T).Name}", DialogueCSVFormat.DIALOGUE_ROOT);

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset == null)
                    continue;

                var id = GetField<string>(asset, idField);
                if (string.IsNullOrEmpty(id))
                    continue;

                if (map.TryGetValue(id, out var first))
                    result.Warnings.Add($"Duplicate {typeof(T).Name} id '{id}': '{path}' ignored, using '{AssetDatabase.GetAssetPath(first)}'. Delete one to clean up.");
                else
                    map[id] = asset;
            }

            return map;
        }

        /// <summary>Returns the existing asset for <paramref name="id"/>, or creates one at newPath(). onResolved(isNew) reports which happened.</summary>
        private T ResolveOrCreate<T>(Dictionary<string, T> index, string id, Func<string> newPath, DialogueCSVImporter.ImportResult result, Action<bool> onResolved) where T : ScriptableObject
        {
            if (index.TryGetValue(id, out var existing))
            {
                onResolved(false);
                return existing;
            }

            var path = newPath();
            EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace("\\", "/"));

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            index[id] = asset;
            onResolved(true);
            return asset;
        }

        /// <summary>Indexes the CharacterSO assets by their character id (case-insensitive) so lines can resolve their speaker.</summary>
        private Dictionary<string, CharacterSO> LoadCharacters()
        {
            var result = new Dictionary<string, CharacterSO>(StringComparer.OrdinalIgnoreCase);
            if (!AssetDatabase.IsValidFolder(DialogueCSVFormat.CHARACTERS_FOLDER))
                return result;

            var guids = AssetSearch.FindInFolder("t:CharacterSO", DialogueCSVFormat.CHARACTERS_FOLDER);

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var character = AssetDatabase.LoadAssetAtPath<CharacterSO>(path);
                if (character == null)
                    continue;

                var id = GetField<string>(character, "_characterId");
                if (!string.IsNullOrEmpty(id))
                    result[id] = character;
            }

            return result;
        }

        private void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parts = path.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        #endregion

        #region Reflection Helpers

        private void SetField<T>(object obj, string fieldName, T value)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        private T GetField<T>(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return field != null ? (T)field.GetValue(obj) : default;
        }

        #endregion
    }
}
