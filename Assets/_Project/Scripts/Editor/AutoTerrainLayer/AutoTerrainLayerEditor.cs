using UnityEditor;
using UnityEngine;
using GlimmerOfHope.Gameplay.AutoTerrainLayer;

namespace GlimmerOfHope.Editor.AutoTerrainLayer
{
    /// <summary>
    /// Adds a Custom Editor for the AutoTerrainLayer component
    /// Adds 2 buttons for Apply and Undo
    /// </summary>
    [CustomEditor(typeof(AutoTerrainLayerByHeight))]
    public class AutoTerrainLayerByHeightEditor : UnityEditor.Editor
    {
        #region Public Methods
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            AutoTerrainLayerByHeight script = (AutoTerrainLayerByHeight)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

            // A Button that allow you to apply the layers by Heights
            if (GUILayout.Button("Appliquer les Layers par Hauteur", GUILayout.Height(30)))
            {
                script.ApplyLayersByHeight();
            }

            //  A Button that allow you to undo a layer paint (only 1 action is recorded for undo)

            if (GUILayout.Button("Undo (Annuler)", GUILayout.Height(30)))
            {
                script.UndoLayers();
            }
        }
        #endregion
    }
}