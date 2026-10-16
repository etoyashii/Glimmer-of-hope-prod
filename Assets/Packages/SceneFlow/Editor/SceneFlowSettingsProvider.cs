using System.Collections.Generic;
using Kebab.SceneFlow.Settings;
using Kebab.SceneFlow.Settings.Editors;
using UnityEditor;
using UnityEngine;


namespace Kebab.SceneFlow.Editors
{
    public static class SceneFlowSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider("Project/SceneFlow", SettingsScope.Project)
            {
                label = "SceneFlow",
                guiHandler = (searchContext) =>
                {
                    var settings = SceneFlowSettings.GetOrCreate();
                    
                    // Create and draw the custom editor
                    var editor = Editor.CreateEditor(settings);
                    if (editor != null && editor is SceneFlowSettingsCustomEditor customEditor)
                    {
                        editor.OnInspectorGUI();
                    }
                    
                    // Apply changes
                    if (GUI.changed)
                    {
                        EditorUtility.SetDirty(settings);
                        AssetDatabase.SaveAssets();
                    }
                },
                keywords = new HashSet<string> { "SceneFlow", "Scene" }
            };
            return provider;
        }

    }
}
