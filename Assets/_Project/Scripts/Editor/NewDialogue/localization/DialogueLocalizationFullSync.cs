using System.Collections.Generic;
using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace GlimmerOfHope.Editor.NewDialogue
{
    /// <summary>
    /// Safety on top of the per-action sync hooks already in the GraphView, the list
    /// Inspector, and the CSV importer: scans every DialogueGraph in the project, creates any
    /// missing String Table entry, and removes entries no longer referenced by any node or choice.
    /// </summary>
    public static class DialogueLocalizationFullSync
    {
        #region Public Methods
        [MenuItem("Window/Dialogue System/Sync All Graphs With Localization Table")]
        public static void SyncAll()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(DialogueLocalizationSettings.CollectionName);
            if (collection == null)
            {
                Debug.LogWarning($"[DialogueLocalizationFullSync] No String Table Collection named '{DialogueLocalizationSettings.CollectionName}'. Set it via Window > Dialogue System > Localization Settings.");
                return;
            }

            var sourceTable = collection.GetTable(DialogueLocalizationSettings.SourceLocaleCode) as StringTable;
            if (sourceTable == null)
            {
                Debug.LogWarning($"[DialogueLocalizationFullSync] Collection '{DialogueLocalizationSettings.CollectionName}' has no table for locale '{DialogueLocalizationSettings.SourceLocaleCode}'.");
                return;
            }

            var validKeys = new HashSet<string>();
            int created = 0;

            var guids = AssetDatabase.FindAssets("t:DialogueGraph");
            foreach (var guid in guids)
            {
                var graph = AssetDatabase.LoadAssetAtPath<DialogueGraph>(AssetDatabase.GUIDToAssetPath(guid));
                if (graph == null) continue;

                bool graphChanged = SyncGraph(graph, collection, sourceTable, validKeys, ref created);
                if (graphChanged) EditorUtility.SetDirty(graph);
            }

            int removed = RemoveOrphanedEntries(collection, validKeys);
            AssetDatabase.SaveAssets();

            Debug.Log($"[DialogueLocalizationFullSync] Synced {guids.Length} graph(s): {created} entry(ies) created, {removed} orphaned entry(ies) removed.");
        }

        //Same as SyncAll, but also opens the Localization Tables window right after 
        [MenuItem("Window/Dialogue System/Open Localization Table (Synced)")]
        public static void OpenSynced()
        {
            SyncAll();
            EditorApplication.ExecuteMenuItem("Window/Asset Management/Localization Tables");
        }
        #endregion

        #region Private Methods
        private static bool SyncGraph(DialogueGraph graph, StringTableCollection collection, StringTable sourceTable, HashSet<string> validKeys, ref int created)
        {
            bool changed = false;

            foreach (var node in graph.TypedNodes)
            {
                if (node is DialogueLineNode lineNode)
                {
                    string key = $"line_{node.nodeId}";
                    validKeys.Add(key);
                    if (EnsureEntry(sourceTable, key, lineNode.text, ref lineNode.localizedText)) { created++; changed = true; }
                }

                for (int i = 0; i < node.choices.Count; i++)
                {
                    var choice = node.choices[i];
                    string key = $"choice_{node.nodeId}_{i}";
                    validKeys.Add(key);
                    if (EnsureEntry(sourceTable, key, choice.choiceText, ref choice.localizedChoiceText)) { created++; changed = true; }
                }
            }

            return changed;
        }

        /// <summary>
        /// Only skips if the entry LocalizedString points genuinely exists in the current table 
        /// </summary>
        private static bool EnsureEntry(StringTable sourceTable, string key, string initialValue, ref LocalizedString target)
        {
            bool entryReallyExists = false;
            if (target != null && !target.IsEmpty)
                entryReallyExists = sourceTable.GetEntry(target.TableEntryReference.Key) != null;

            if (entryReallyExists) return false; // genuinely linked to something real, leave it 
            DialogueLocalizationSync.CreateEntry(out target, key, initialValue);
            return true;
        }

        private static int RemoveOrphanedEntries(StringTableCollection collection, HashSet<string> validKeys)
        {
            var toRemove = new List<string>();
            foreach (var entry in collection.SharedData.Entries)
                if (!validKeys.Contains(entry.Key))
                    toRemove.Add(entry.Key);

            foreach (var key in toRemove)
                collection.RemoveEntry(key);

            if (toRemove.Count > 0)
            {
                EditorUtility.SetDirty(collection.SharedData);
                foreach (var table in collection.StringTables)
                    EditorUtility.SetDirty(table);
            }

            return toRemove.Count;
        }
        #endregion
    }
}