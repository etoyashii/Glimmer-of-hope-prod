#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using GlimmerOfHope.Core;
namespace GlimmerOfHope.Editor
{
    [CustomPropertyDrawer(typeof(PreviewPrefabAttribute))]
    public class PreviewPrefabDrawer : PropertyDrawer
    {
        /// <summary>
        /// This class is used to create a preview of the prefabs in the inspector.
        /// </summary>
        #region Unity LifeCycle

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label); // Display the label

            PreviewPrefabAttribute previewAttr = (PreviewPrefabAttribute)attribute; 
            float previewSize = previewAttr.width;// Get the attribute for the size

            Rect fieldRect = new Rect(position.x, position.y, position.width - previewSize - 5, position.height); // Display the ObjectField

            property.objectReferenceValue = EditorGUI.ObjectField(fieldRect, property.objectReferenceValue, typeof(GameObject), false);
            
            if (property.objectReferenceValue != null) // Display the preview if a Prefab is assigned
            {
                GameObject prefab = (GameObject)property.objectReferenceValue;
                // Check if the object is a Prefab or a Prefab instance
                bool isPrefab = PrefabUtility.IsPartOfPrefabInstance(prefab) || PrefabUtility.GetPrefabInstanceStatus(prefab) != PrefabInstanceStatus.NotAPrefab;
                // If it's not an instance, check if it's a Prefab asset
                if (!isPrefab && prefab != null)
                {
                    isPrefab = AssetDatabase.Contains(prefab) &&
                              PrefabUtility.GetPrefabAssetType(prefab) != PrefabAssetType.NotAPrefab;
                }
                if (isPrefab)
                {
                    Rect previewRect = new Rect(fieldRect.x + fieldRect.width + 5, position.y, previewSize, previewSize);
                    Texture2D preview = AssetPreview.GetAssetPreview(prefab);
                    if (preview != null)
                    {
                        GUI.DrawTexture(previewRect, preview);
                    }
                    else
                    {
                        // If no preview available, display an empty box
                        GUI.Box(previewRect, "");
                    }
                }
            }
            EditorGUI.EndProperty();
        }
        #endregion
    }
}
#endif