using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AutoTerrainLayerByHeight))]
public class AutoTerrainLayerByHeightEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AutoTerrainLayerByHeight script = (AutoTerrainLayerByHeight)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Appliquer les Layers par Hauteur", GUILayout.Height(30)))
        {
            script.ApplyLayersByHeight();
        }

        if (GUILayout.Button("Undo (Annuler)", GUILayout.Height(30)))
        {
            script.UndoLayers();
        }
    }
}