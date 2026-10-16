using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kebab.SceneFlow.Components.Editor
{
    [CustomPropertyDrawer(typeof(SceneAttribute))]
    public class SceneAttributePropertyDrawer : PropertyDrawer
    {
        private static readonly GUIContent refreshButtonContent = EditorGUIUtility.IconContent("Refresh");
        private const float REFRESH_BUTTON_WIDTH = 25;
        private static string[] sceneNames = null;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            if (sceneNames == null) RefreshSceneNames();
            DrawDropdown(position, property, label);
        }

        private void DrawDropdown(Rect position, SerializedProperty property, GUIContent label)
        {
            // Create a label field
            Rect labelRect = EditorGUI.PrefixLabel(position, label);
            Rect refreshButtonRect = new Rect(labelRect.x + labelRect.width - REFRESH_BUTTON_WIDTH, labelRect.y, REFRESH_BUTTON_WIDTH, labelRect.height);
            Rect popupRect = new Rect(labelRect.x, labelRect.y, labelRect.width - REFRESH_BUTTON_WIDTH, labelRect.height);

            // Draw the popup in remaining space
            int currentIndex = sceneNames.ToList().IndexOf(property.stringValue);
            int newIndex = EditorGUI.Popup(popupRect, currentIndex, sceneNames);

            if (newIndex == -1 && sceneNames.Length > 0)
                newIndex = 0;
            if (newIndex != currentIndex)
                property.stringValue = sceneNames[newIndex];

            if (GUI.Button(refreshButtonRect, refreshButtonContent))
                RefreshSceneNames();

        }

        public static void RefreshSceneNames()

        {
            List<string> tmpSceneNames = new List<string>();
            string sceneName = null;

            EditorBuildSettings.scenes.ToList().ForEach(scene =>
            {
                if (scene.enabled)
                {
                    sceneName = Path.GetFileNameWithoutExtension(scene.path);
                    tmpSceneNames.Add(sceneName);
                }

            });

            sceneNames = tmpSceneNames.ToArray();
        }
    }
}
