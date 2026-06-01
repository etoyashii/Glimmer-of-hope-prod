using UnityEditor;
using UnityEngine;
using GlimmerOfHope.Gameplay;

namespace GlimmerOfHope.Editor
{
    [CustomEditor(typeof(GlimmerOfHope.Gameplay.MultiTag))]
    public class MultiTagEditor : UnityEditor.Editor
    {
        #region Private Fields
        private SerializedProperty tagsProperty;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            if (serializedObject != null)
            {
                tagsProperty = serializedObject.FindProperty("_tags");

            }
        }
        #endregion

        #region Public Methods
        public override void OnInspectorGUI()
        {
            if (serializedObject == null || tagsProperty == null)
            {
                EditorGUILayout.HelpBox("Erreur : serializedObject ou tagsProperty est null. Vérifie que MultiTag a un champ 'tags' marqué avec [SerializeField].", MessageType.Error);
                return;
            }

            serializedObject.Update();

            if (GUILayout.Button("+ Add Tag"))
            {
                tagsProperty.arraySize++;
                tagsProperty.GetArrayElementAtIndex(tagsProperty.arraySize - 1).stringValue = "";
            }

            int i = 0;
            while (i < tagsProperty.arraySize)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Tag {i + 1}", GUILayout.Width(50));

                SerializedProperty element = tagsProperty.GetArrayElementAtIndex(i);
                element.stringValue = EditorGUILayout.TextField(element.stringValue);

                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    tagsProperty.DeleteArrayElementAtIndex(i);
                    continue;
                }

                EditorGUILayout.EndHorizontal();
                i++;
            }

            serializedObject.ApplyModifiedProperties();
        }
        #endregion
    }
}