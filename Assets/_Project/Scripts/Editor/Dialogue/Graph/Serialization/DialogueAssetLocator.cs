using System.IO;
using UnityEditor;
using UnityEngine;
using GlimmerOfHope.Editor.Common;
using GlimmerOfHope.Gameplay.Dialogue;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    /// <summary>
    /// Locates dialogue assets by id ANYWHERE under the dialogue root, instead of assuming a flat
    /// folder. This keeps the graph tool working after Line/Conversation/Layout SOs have been sorted
    /// into subfolders — the same folder-agnostic rule the CSV importer follows.
    /// </summary>
    public static class DialogueAssetLocator
    {
        private static readonly string[] SearchRoots = { DialogueCSVFormat.DIALOGUE_ROOT };

        /// <summary>Finds the layout for a conversation by its stored ConversationId, wherever the asset lives.</summary>
        public static GraphLayoutData FindLayout(string conversationId)
        {
            foreach (var guid in AssetSearch.FindInFolders("t:GraphLayoutData", SearchRoots))
            {
                var layout = AssetDatabase.LoadAssetAtPath<GraphLayoutData>(AssetDatabase.GUIDToAssetPath(guid));
                if (layout != null && layout.ConversationId == conversationId)
                    return layout;
            }
            return null;
        }

        /// <summary>Finds an existing line by its LineId, wherever the asset lives (avoids creating flat duplicates).</summary>
        public static DialogueLineSO FindLine(string lineId)
        {
            foreach (var guid in AssetSearch.FindInFolders("t:DialogueLineSO", SearchRoots))
            {
                var line = AssetDatabase.LoadAssetAtPath<DialogueLineSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (line != null && line.LineId == lineId)
                    return line;
            }
            return null;
        }

        /// <summary>Folder an existing asset sits in, or null if it is not saved on disk yet.</summary>
        public static string FolderOf(Object asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrEmpty(path) ? null : Path.GetDirectoryName(path).Replace("\\", "/");
        }

        /// <summary>
        /// Folder where new lines of a conversation should be created: next to the conversation's
        /// existing lines if any, otherwise the base Lines folder. Keeps a conversation's lines together.
        /// </summary>
        public static string LinesFolderFor(ConversationSO conversation)
        {
            if (conversation != null && conversation.AllLines != null)
            {
                foreach (var line in conversation.AllLines)
                {
                    var folder = FolderOf(line);
                    if (folder != null)
                        return folder;
                }
            }
            return DialogueCSVFormat.LINES_FOLDER;
        }
    }
}
