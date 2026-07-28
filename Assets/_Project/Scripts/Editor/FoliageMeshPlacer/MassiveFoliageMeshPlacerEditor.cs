using UnityEditor;
using UnityEngine;
using GlimmerOfHope.Gameplay;
namespace GlimmerOfHope.Editor
{
    /// <summary>
    /// Custom inspector for "MassiveFoliageMeshPlacer"
    /// It draws the terrain texture layer with its density/placement settings and its list of foliage prefab entries
    /// </summary>
    [CustomEditor(typeof(MassiveFoliageMeshPlacer))]
    public class MassiveFoliageMeshPlacerEditor : UnityEditor.Editor
    {
        #region Private Fields
        // SerializedProperty handles for the placer's fields,
        private SerializedProperty terrainProp;
        private SerializedProperty terrainLayersProp;
        private SerializedProperty eraseModificationProp;
        private SerializedProperty collisionCheckLayersProp;

        // One state per terrain layer, kept in sync with terrainLayersProp.arraySize.
        private bool[] layerFoldouts;
        #endregion

        #region Unity LifeCycle
        // Resolves the SerializedProperty references used throughout the inspector.
        private void OnEnable()
        {
            terrainProp = serializedObject.FindProperty("terrain");
            terrainLayersProp = serializedObject.FindProperty("TerrainLayers");
            eraseModificationProp = serializedObject.FindProperty("EraseModification");
            collisionCheckLayersProp = serializedObject.FindProperty("collisionCheckLayers");
        }

        // Draws the whole custom inspector: global settings
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var placer = (MassiveFoliageMeshPlacer)target;

            EditorGUILayout.PropertyField(terrainProp);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(eraseModificationProp, new GUIContent("Effacer avant de générer"));
            EditorGUILayout.PropertyField(collisionCheckLayersProp, new GUIContent("Layers de collision testés"));
            EditorGUILayout.Space();

            // Resize the foldout state array if the number of terrain layers changed
            if (layerFoldouts == null || layerFoldouts.Length != terrainLayersProp.arraySize)
                layerFoldouts = new bool[terrainLayersProp.arraySize];

            EditorGUILayout.LabelField("Layers de texture du terrain", EditorStyles.boldLabel);

            // One collapsible "box" per terrain layer.
            for (int i = 0; i < terrainLayersProp.arraySize; i++)
            {
                SerializedProperty layerProp = terrainLayersProp.GetArrayElementAtIndex(i);
                SerializedProperty nameProp = layerProp.FindPropertyRelative("name");
                SerializedProperty densityMultProp = layerProp.FindPropertyRelative("DensityMultiplier");
                SerializedProperty resolutionProp = layerProp.FindPropertyRelative("placementResolution");
                SerializedProperty SideSize = layerProp.FindPropertyRelative("SideSize");
                SerializedProperty alphaDensity = layerProp.FindPropertyRelative("AlphaDensity");

                SerializedProperty foliageProp = layerProp.FindPropertyRelative("FoliagePrefabs");

                EditorGUILayout.BeginVertical("box");

                string label = string.IsNullOrEmpty(nameProp.stringValue) ? $"Layer {i}" : nameProp.stringValue;
                layerFoldouts[i] = EditorGUILayout.Foldout(layerFoldouts[i], label, true);

                if (layerFoldouts[i])
                {
                    EditorGUI.indentLevel++;

                    // Per-layer placement settings.
                    EditorGUILayout.PropertyField(densityMultProp, new GUIContent("Multiplicateur de densité (layer)"));
                    EditorGUILayout.PropertyField(resolutionProp, new GUIContent("Résolution de la grille de placement"));

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(SideSize, new GUIContent("SideSize"));
                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(alphaDensity, new GUIContent("AlphaDensity"));
                    EditorGUILayout.Space();

                    // List of foliage prefab entries configured for this layer.
                    EditorGUILayout.LabelField($"Prefabs de foliage ({foliageProp.arraySize})", EditorStyles.miniBoldLabel);
                    DrawFoliageList(foliageProp);

                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    // apply pending edits first so the placer reads up-to-date values,
                    // then record an undo snapshot of the whole hierarchy before mutating it.
                    if (GUILayout.Button("Générer ce layer"))
                    {
                        serializedObject.ApplyModifiedProperties();
                        Undo.RegisterFullObjectHierarchyUndo(placer.gameObject, "Generate Foliage Layer");
                        placer.GenerateFoliageMeshesForLayer(i);
                        EditorUtility.SetDirty(placer);
                    }
                    if (GUILayout.Button("Nettoyer ce layer"))
                    {
                        Undo.RegisterFullObjectHierarchyUndo(placer.gameObject, "Clean Foliage Layer");
                        placer.CleanFoliageMeshes(i);
                        EditorUtility.SetDirty(placer);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Actions globales", EditorStyles.boldLabel);

            // Global generate/clean buttons, acting on every layer at once (layerIndex -1 in CleanFoliageMeshes).
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Générer tout le foliage"))
            {
                Undo.RegisterFullObjectHierarchyUndo(placer.gameObject, "Generate Foliage Meshes");
                placer.GenerateFoliageMeshes();
                EditorUtility.SetDirty(placer);
            }
            if (GUILayout.Button("Nettoyer tout le foliage"))
            {
                Undo.RegisterFullObjectHierarchyUndo(placer.gameObject, "Clean Foliage Meshes");
                placer.CleanFoliageMeshes(-1);
                EditorUtility.SetDirty(placer);
            }
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Draws the editable list of foliage prefab entries for one terrain layer
        /// </summary>
        private void DrawFoliageList(SerializedProperty foliageProp)
        {
            // deleting mid-loop would shift indices and break iteration,so we just remember which index to remove and do it after the loop.
            int removeIndex = -1;

            for (int j = 0; j < foliageProp.arraySize; j++)
            {
                SerializedProperty entry = foliageProp.GetArrayElementAtIndex(j);
                SerializedProperty prefabProp = entry.FindPropertyRelative("prefab");
                SerializedProperty AlignToNormal = entry.FindPropertyRelative("AlignToNormal");

                EditorGUILayout.BeginVertical("helpbox");

                // Header uses the assigned prefab's name once one is set, otherwise a placeholder.
                string label = prefabProp.objectReferenceValue != null
                    ? prefabProp.objectReferenceValue.name
                    : $"Emplacement Prefab {j}";
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(prefabProp, new GUIContent("Prefab"));
                EditorGUILayout.Space(50);
                EditorGUILayout.PropertyField(AlignToNormal, new GUIContent("AlignToNormal"));
                EditorGUILayout.Space(5);

                // per-prefab spawn rules (fill type, density, alpha threshold, scale/rotation, collision radius).
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("fillType"), new GUIContent("Type de remplissage"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("density"), new GUIContent("Densité"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("fallOff"), new GUIContent("Seuil alpha"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("uniformScaleRange"), new GUIContent("Plage d'échelle"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("randomRotationY"), new GUIContent("Rotation Y aléatoire"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("collisionCheckRadius"), new GUIContent("Rayon collision (fallback)"));

                // Slope range drawn as a min-max slider (0-90°) rather than two raw float fields.
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("Pente autorisée", EditorStyles.miniLabel);
                SerializedProperty minSlopeProp = entry.FindPropertyRelative("minSlope");
                SerializedProperty maxSlopeProp = entry.FindPropertyRelative("maxSlope");
                float minSlope = minSlopeProp.floatValue;
                float maxSlope = maxSlopeProp.floatValue;
                EditorGUILayout.MinMaxSlider(new GUIContent($"{minSlope:0}° - {maxSlope:0}°"), ref minSlope, ref maxSlope, 0f, 90f);
                minSlopeProp.floatValue = minSlope;
                maxSlopeProp.floatValue = maxSlope;

                if (GUILayout.Button("Supprimer", GUILayout.Width(90)))
                    removeIndex = j;

                EditorGUILayout.EndVertical();
            }

            if (removeIndex >= 0)
                foliageProp.DeleteArrayElementAtIndex(removeIndex);

            // Appends a new entry with sensible default values so the user doesn't start from empty/zeroed fields.
            if (GUILayout.Button("+ Ajouter un Prefab de foliage"))
            {
                foliageProp.InsertArrayElementAtIndex(foliageProp.arraySize);
                SerializedProperty newEntry = foliageProp.GetArrayElementAtIndex(foliageProp.arraySize - 1);
                newEntry.FindPropertyRelative("prefab").objectReferenceValue = null;
                newEntry.FindPropertyRelative("density").intValue = 50;
                newEntry.FindPropertyRelative("fallOff").floatValue = 0.8f;
                newEntry.FindPropertyRelative("fillType").enumValueIndex = 0;
                newEntry.FindPropertyRelative("uniformScaleRange").vector2Value = new Vector2(0.85f, 1.15f);
                newEntry.FindPropertyRelative("randomRotationY").boolValue = true;
                newEntry.FindPropertyRelative("collisionCheckRadius").floatValue = 0.5f;
                newEntry.FindPropertyRelative("minSlope").floatValue = 0f;
                newEntry.FindPropertyRelative("maxSlope").floatValue = 45f;
            }
        }
        #endregion
    }
}