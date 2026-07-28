using UnityEditor;
using UnityEngine;
using GlimmerOfHope.Gameplay;
namespace GlimmerOfHope.Editor
{


    [CustomEditor(typeof(MassiveFoliageMeshPlacer))]
    public class MassiveFoliageMeshPlacerEditor : UnityEditor.Editor
    {
        #region Private Fields
        private SerializedProperty terrainProp;
        private SerializedProperty terrainLayersProp;
        private SerializedProperty eraseModificationProp;
        private SerializedProperty collisionCheckLayersProp;
        private bool[] layerFoldouts;
        #endregion

        #region Unity LifeCycle
        private void OnEnable()
        {
            terrainProp = serializedObject.FindProperty("terrain");
            terrainLayersProp = serializedObject.FindProperty("TerrainLayers");
            eraseModificationProp = serializedObject.FindProperty("EraseModification");
            collisionCheckLayersProp = serializedObject.FindProperty("collisionCheckLayers");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var placer = (MassiveFoliageMeshPlacer)target;

            EditorGUILayout.PropertyField(terrainProp);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(eraseModificationProp, new GUIContent("Effacer avant de générer"));
            EditorGUILayout.PropertyField(collisionCheckLayersProp, new GUIContent("Layers de collision testés"));
            EditorGUILayout.Space();

            if (layerFoldouts == null || layerFoldouts.Length != terrainLayersProp.arraySize)
                layerFoldouts = new bool[terrainLayersProp.arraySize];

            EditorGUILayout.LabelField("Layers de texture du terrain", EditorStyles.boldLabel);

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

                    EditorGUILayout.PropertyField(densityMultProp, new GUIContent("Multiplicateur de densité (layer)"));
                    EditorGUILayout.PropertyField(resolutionProp, new GUIContent("Résolution de la grille de placement"));

                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(SideSize, new GUIContent("SideSize"));
                    EditorGUILayout.Space();

                    EditorGUILayout.PropertyField(alphaDensity, new GUIContent("AlphaDensity"));
                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField($"Prefabs de foliage ({foliageProp.arraySize})", EditorStyles.miniBoldLabel);
                    DrawFoliageList(foliageProp);

                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
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
        private void DrawFoliageList(SerializedProperty foliageProp)
        {
            int removeIndex = -1;

            for (int j = 0; j < foliageProp.arraySize; j++)
            {
                SerializedProperty entry = foliageProp.GetArrayElementAtIndex(j);
                SerializedProperty prefabProp = entry.FindPropertyRelative("prefab");
                SerializedProperty AlignToNormal = entry.FindPropertyRelative("AlignToNormal");

                EditorGUILayout.BeginVertical("helpbox");

                string label = prefabProp.objectReferenceValue != null
                    ? prefabProp.objectReferenceValue.name
                    : $"Emplacement Prefab {j}";
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(prefabProp, new GUIContent("Prefab"));
                EditorGUILayout.Space(50);
                EditorGUILayout.PropertyField(AlignToNormal, new GUIContent("AlignToNormal"));
                EditorGUILayout.Space(5);


                EditorGUILayout.PropertyField(entry.FindPropertyRelative("fillType"), new GUIContent("Type de remplissage"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("density"), new GUIContent("Densité"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("fallOff"), new GUIContent("Seuil alpha"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("uniformScaleRange"), new GUIContent("Plage d'échelle"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("randomRotationY"), new GUIContent("Rotation Y aléatoire"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("collisionCheckRadius"), new GUIContent("Rayon collision (fallback)"));

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