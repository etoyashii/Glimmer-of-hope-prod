using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace GlimmerOfHope.Editor.NewDialogue
{
    /// <summary>
    /// Keeps the String Table Collection in sync with the dialogue graphs: creates an entry
    /// when a line/choice is added, removes it when deleted, and updates the source-language
    /// value when the author edits the text in the GraphView or the list Inspector.
    /// </summary>
    public static class DialogueLocalizationSync
    {
        #region Public Methods
        public static void CreateEntry(out LocalizedString target, string key, string initialValue = "")
        {
            var collection = GetCollection();
            if (collection == null)
            {
                target = new LocalizedString();
                return;
            }

            var sourceTable = collection.GetTable(DialogueLocalizationSettings.SourceLocaleCode) as StringTable;

            foreach (var table in collection.StringTables)
                table.AddEntry(key, table == sourceTable ? initialValue : "");

            MarkDirty(collection);

            target = new LocalizedString
            {
                TableReference = collection.TableCollectionNameReference,
                TableEntryReference = key
            };
        }

        /// <summary>Reads the current source-language value from the table, for populating a field on display.</summary>
        public static string GetSourceValue(LocalizedString localized, string fallback)
        {
            if (localized == null || localized.IsEmpty) return fallback;

            var collection = GetCollection();
            if (collection == null) return fallback;

            var sourceTable = collection.GetTable(DialogueLocalizationSettings.SourceLocaleCode) as StringTable;
            var entry = sourceTable?.GetEntry(localized.TableEntryReference.Key);
            return entry != null ? entry.Value : fallback;
        }

        //Call when the author edits the plain text field, to keep the source-language value in sync
        public static void UpdateSourceValue(LocalizedString localized, string newValue)
        {
            if (localized == null || localized.IsEmpty) return;

            var collection = GetCollection();
            if (collection == null) return;

            var sourceTable = collection.GetTable(DialogueLocalizationSettings.SourceLocaleCode) as StringTable;
            if (sourceTable == null) return;

            var entry = sourceTable.GetEntry(localized.TableEntryReference.Key);
            if (entry == null)
                entry = sourceTable.AddEntry(localized.TableEntryReference.Key, newValue);
            else
                entry.Value = newValue;

            EditorUtility.SetDirty(sourceTable);
        }

        public static void RemoveEntry(LocalizedString localized)
        {
            if (localized == null || localized.IsEmpty) return;

            var collection = GetCollection();
            if (collection == null) return;

            collection.RemoveEntry(localized.TableEntryReference);
            MarkDirty(collection);
        }
        #endregion

        #region Private Methods
        private static StringTableCollection GetCollection()
        {
            string name = DialogueLocalizationSettings.CollectionName;
            var collection = LocalizationEditorSettings.GetStringTableCollection(name);

            if (collection == null)
                Debug.LogWarning($"[DialogueLocalizationSync] No String Table Collection named '{name}'. Set it via Window > Dialogue System > Localization Settings.");

            return collection;
        }

        private static void MarkDirty(StringTableCollection collection)
        {
            EditorUtility.SetDirty(collection.SharedData);
            foreach (var table in collection.StringTables)
                EditorUtility.SetDirty(table);
        }
        #endregion
    }
}