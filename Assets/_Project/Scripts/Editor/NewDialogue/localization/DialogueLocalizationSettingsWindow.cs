using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor.NewDialogue
{
    public class DialogueLocalizationSettingsWindow : EditorWindow
    {
        #region Public Methods
        [MenuItem("Window/Dialogue System/Localization Settings...")]
        public static void Open()
        {
            var window = GetWindow<DialogueLocalizationSettingsWindow>();
            window.titleContent = new GUIContent("Dialogue Localization Settings");
            window.minSize = new Vector2(380, 120);
        }
        #endregion

        #region Unity Lifecycle
        private void OnGUI()
        {
            EditorGUILayout.LabelField("Dialogue Localization Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Used by the migration tool and by automatic sync (new/deleted nodes and choices).", MessageType.Info);
            EditorGUILayout.Space();

            DialogueLocalizationSettings.CollectionName = EditorGUILayout.TextField("String Table Collection", DialogueLocalizationSettings.CollectionName);
            DialogueLocalizationSettings.SourceLocaleCode = EditorGUILayout.TextField("Source Locale Code", DialogueLocalizationSettings.SourceLocaleCode);
        }
        #endregion
    }
}
