#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using GlimmerOfHope.Core;

namespace GlimmerOfHope.Editor
{

    [CustomPropertyDrawer(typeof(PreviewPrefabAttribute))]
    public class PreviewPrefabDrawer : PropertyDrawer
    {
        #region Unity LifeCycle
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Affiche le label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            // Récupère l'attribut pour la taille
            PreviewPrefabAttribute previewAttr = (PreviewPrefabAttribute)attribute;
            float previewSize = previewAttr.width;

            // Affiche le champ ObjectField
            Rect fieldRect = new Rect(position.x, position.y, position.width - previewSize - 5, position.height);
            property.objectReferenceValue = EditorGUI.ObjectField(fieldRect, property.objectReferenceValue, typeof(GameObject), false);

            // Affiche la prévisualisation si un Prefab est assigné
            if (property.objectReferenceValue != null)
            {
                GameObject prefab = (GameObject)property.objectReferenceValue;

                // Vérifie si l'objet est un Prefab ou une instance de Prefab
                bool isPrefab = PrefabUtility.IsPartOfPrefabInstance(prefab) ||
                               PrefabUtility.GetPrefabInstanceStatus(prefab) != PrefabInstanceStatus.NotAPrefab;

                // Si ce n'est pas une instance, vérifie si c'est un asset Prefab
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
                        // Si pas de prévisualisation, affiche une case vide
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
