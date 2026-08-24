using UnityEditor;

namespace GlimmerOfHope.Editor.NewDialogue
{
    /// <summary>
    /// Which String Table Collection and source language to use, Stored in EditorPrefs so it's set once per machine, not per graph.
    /// </summary>
    public static class DialogueLocalizationSettings
    {
        #region Private Fields
        private const string CollectionKey = "DialogueSystem.StringTableCollection";
        private const string SourceLocaleKey = "DialogueSystem.SourceLocaleCode";

        #endregion

        #region Public Properties
        public static string CollectionName
        {
            get => EditorPrefs.GetString(CollectionKey, "DialogueText");
            set => EditorPrefs.SetString(CollectionKey, value);
        }

        public static string SourceLocaleCode
        {
            get => EditorPrefs.GetString(SourceLocaleKey, "en");
            set => EditorPrefs.SetString(SourceLocaleKey, value);
        }
        #endregion
    }
}
