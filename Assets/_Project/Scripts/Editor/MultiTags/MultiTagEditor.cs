using UnityEditor;
using UnityEngine;
using GlimmerOfHope.Gameplay;

namespace GlimmerOfHope.Editor
{
    /// <summary>
    /// A custom editor for the MultiTag component.
    /// It provides a more user-friendly interface for the MultiTag component,
    /// for example by replacing the default element names (e.g., "Element 0") with "Tag 1", "Tag 2", etc.
    /// </summary>

    [CustomEditor(typeof(GlimmerOfHope.Gameplay.MultiTag))]
    public class MultiTagEditor : UnityEditor.Editor
    {
        #region Private Fields
        private SerializedProperty tagsProperty;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            // Initialize the reference to the "_tags" property when the editor becomes enabled.
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
                EditorGUILayout.HelpBox(
                    "Erreur : serializedObject ou tagsProperty est null. Vérifie que MultiTag a un champ '_tags' marqué avec [SerializeField].",
                    MessageType.Error);
                return;
            }

            // Apply modifications to the serialized object before editing.
            serializedObject.Update();

            // Button to add a new tag to the list.
            if (GUILayout.Button("+ Add Tag"))
            {
                tagsProperty.arraySize++;
                tagsProperty.GetArrayElementAtIndex(tagsProperty.arraySize - 1).stringValue = "";
            }

            int i = 0;
            while (i < tagsProperty.arraySize)
            {
                EditorGUILayout.BeginHorizontal();

                // Display the tag label with its index (e.g., "Tag 1", "Tag 2").
                EditorGUILayout.LabelField($"Tag {i + 1}", GUILayout.Width(50));

                SerializedProperty element = tagsProperty.GetArrayElementAtIndex(i);
                element.stringValue = EditorGUILayout.TextField(element.stringValue);

                // Button to remove the current tag.
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