using UnityEngine;
using UnityEditor;
using GlimmerOfHope.Gameplay;

namespace GlimmerOfHope.Editor
{
    [CustomEditor(typeof(BlenderMeshFixer))]
    public class BlenderMeshFixerEditor : UnityEditor.Editor
    {
        #region Private Fields
        // Target folder where baked meshes and updated prefabs will be stored
        private const string SAVE_FOLDER = "Assets/_Project/Prefabs/BlenderSavedPrefabs";
        #endregion

        #region Unity LifeCycle
        public override void OnInspectorGUI()
        {
            BlenderMeshFixer fixer = (BlenderMeshFixer)target;

            // Draw default inspector fields (importMesh, etc.)
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            // BUTTON 1: Rotate vertices -90 degrees on X axis to fix Blender import orientation
            GUI.backgroundColor = new Color(0.4f, 0.7f, 1f);
            if (GUILayout.Button("[Axes] Bake -90° X Axis Correction", GUILayout.Height(32)))
            {
                BakeAll(fixer, bakeAxes: true, bakeScale: false);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.HelpBox(
                "Applies -90 degrees on X.\n" +
                "Based on the current mesh (importMesh) and saves the result.",
                MessageType.Info);

            EditorGUILayout.Space(12);

            // BUTTON 2: Freeze current scene Transform (Rotation/Scale) into the geometry, then reset Transform to (0,0,0) / (1,1,1)
            GUI.backgroundColor = new Color(0.9f, 0.5f, 0.1f); // Orange
            if (GUILayout.Button("Make current Transform scale equals to 1/1/1 local scale", GUILayout.Height(32)))
            {
                BakeCurrentSceneTransform(fixer);
            }
            GUI.backgroundColor = Color.white;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Captures the current local rotation and scale from the scene instance, bakes them into a new mesh asset, and resets the transform.
        /// </summary>
        private void BakeCurrentSceneTransform(BlenderMeshFixer fixer)
        {
            MeshFilter mf = fixer.GetComponent<MeshFilter>();
            if (!ValidateMeshFilter(mf)) return;

            Mesh currentMesh = mf.sharedMesh;
            if (currentMesh == null) return;

            // Extract current transformation data from the scene instance
            Quaternion rotation = fixer.transform.localRotation;
            Vector3 scale = fixer.transform.localScale;

            if (scale == Vector3.zero)
            {
                EditorUtility.DisplayDialog("BlenderMeshFixer", "Transform scale is (0,0,0), operation canceled.", "OK");
                return;
            }

            string meshName = fixer.gameObject.name + "_baked";
            Mesh bakedMesh = BakeMeshWithTransform(currentMesh, rotation, scale, meshName);

            // Save the generated asset, update component references, and reset the scene transform
            SaveMeshAndPrefab(mf, fixer, bakedMesh, resetRot: true, resetScale: true);

            Debug.Log("[BlenderMeshFixer] Scene modification successfully saved!");
        }

        /// <summary>
        /// Handles standard bakes (like the -90 degrees Blender fix) using the original or previous baked mesh as source.
        /// </summary>
        private void BakeAll(BlenderMeshFixer fixer, bool bakeAxes, bool bakeScale)
        {
            MeshFilter mf = fixer.GetComponent<MeshFilter>();
            if (!ValidateMeshFilter(mf)) return;

            // Retrieve the source mesh (uses importMesh if it exists, or use the original FBX asset)
            Mesh sourceMesh = GetOrCaptureImportMesh(fixer, mf);
            if (sourceMesh == null) return;

            // Define targeted modifications based on button choice
            Quaternion rotation = bakeAxes ? Quaternion.Euler(-90f, 0f, 0f) : Quaternion.identity;
            Vector3 scale = Vector3.one;

            string meshName = fixer.gameObject.name + "_baked";
            Mesh bakedMesh = BakeMeshWithTransform(sourceMesh, rotation, scale, meshName);

            bool resetScale = bakeScale;
            bool resetRot = bakeAxes;

            SaveMeshAndPrefab(mf, fixer, bakedMesh, resetRot, resetScale);

            string op = bakeAxes ? "Axes baked" : "Scale baked";
            Debug.Log($"[BlenderMeshFixer] {op} applied to the mesh.");
        }

        /// <summary>
        /// Make sure we have a valid starting mesh, tracking the persistent baked mesh asset for modifications.
        /// </summary>
        private static Mesh GetOrCaptureImportMesh(BlenderMeshFixer fixer, MeshFilter mf)
        {
            // If importMesh is already assigned from a previous bake, use it as the new baseMesh
            if (fixer.importMesh != null)
                return fixer.importMesh;

            // First-time bake validation: ensure the original mesh belongs to an actual project asset (FBX)
            string currentPath = AssetDatabase.GetAssetPath(mf.sharedMesh);

            if (string.IsNullOrEmpty(currentPath))
            {
                EditorUtility.DisplayDialog("BlenderMeshFixer", "The current sharedMesh is not an asset on disk.", "OK");
                return null;
            }

            return mf.sharedMesh;
        }

        /// <summary>
        /// Instantiates a copy of the source mesh and manually transforms its vertices and normals in local space.
        /// </summary>
        private static Mesh BakeMeshWithTransform(Mesh source, Quaternion rotation, Vector3 scale, string newName)
        {
            Mesh result = Instantiate(source);
            result.name = newName;

            Vector3[] vertices = result.vertices;
            Vector3[] normals = result.normals;

            // Iteratively apply rotation and scale vectors directly into the vertex array data
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 v = rotation * vertices[i];
                vertices[i] = Vector3.Scale(v, scale);

                if (i < normals.Length)
                    normals[i] = rotation * normals[i];
            }

            // Assign modified arrays back and force bounds/tangents recalculation for correct rendering/lighting
            result.vertices = vertices;
            result.normals = normals;
            result.RecalculateBounds();
            result.RecalculateTangents();

            return result;
        }

        /// <summary>
        /// Serializes the new mesh data to disk, updates references, resets scene transform, and regenerates the Prefab asset safely.
        /// </summary>
        private static void SaveMeshAndPrefab(MeshFilter mf, BlenderMeshFixer fixer,Mesh bakedMesh, bool resetRot, bool resetScale)
        {
            EnsureFolderExists();

            string meshPath = $"{SAVE_FOLDER}/Meshs/{bakedMesh.name}.asset";

            Mesh existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existingMesh != null)
            {
                //Clear the existing mesh cache to force Unity Renderers and the GPU memory to refresh instantly
                existingMesh.Clear();
                EditorUtility.CopySerialized(bakedMesh, existingMesh);
                EditorUtility.SetDirty(existingMesh);
                AssetDatabase.SaveAssets();
                bakedMesh = existingMesh;
            }
            else
            {
                AssetDatabase.CreateAsset(bakedMesh, meshPath);
                AssetDatabase.SaveAssets();
            }

            // Update the component reference
            fixer.importMesh = bakedMesh;

            // Register undo states for Unity Editor workflow
            Undo.RecordObject(mf, "Bake Mesh");
            Undo.RecordObject(fixer.transform, "Bake Mesh Transform");
            Undo.RecordObject(fixer, "Bake Mesh Fixer");

            mf.sharedMesh = bakedMesh;

            // Reset scene transform since changes are now hardbaked inside the mesh data
            if (resetRot)
                fixer.transform.localRotation = Quaternion.identity;
            if (resetScale)
                fixer.transform.localScale = Vector3.one;

            EditorUtility.SetDirty(mf);
            EditorUtility.SetDirty(fixer);

            string prefabPath = $"{SAVE_FOLDER}/{fixer.gameObject.name}.prefab";

            // Temporary layout data to re-instantiate the prefab variant in the scene 
            Transform t = fixer.transform;
            Transform parent = t.parent;
            Vector3 pos = t.localPosition;
            Quaternion rot = t.localRotation;
            Vector3 scl = t.localScale;
            int siblingIndex = t.GetSiblingIndex();

            Mesh importMeshRef = fixer.importMesh;

            // Save the GameObject instance as a Prefab Asset
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(fixer.gameObject, prefabPath, out bool success);

            if (!success || prefabAsset == null)
            {
                EditorUtility.DisplayDialog("BlenderMeshFixer", "Error while saving the prefab.", "OK");
                return;
            }

            // Sync the component reference inside the saved asset prefab file
            BlenderMeshFixer prefabFixer = prefabAsset.GetComponent<BlenderMeshFixer>();
            if (prefabFixer != null)
            {
                prefabFixer.importMesh = importMeshRef;
                EditorUtility.SetDirty(prefabFixer);
                AssetDatabase.SaveAssetIfDirty(prefabAsset);
            }

            // Destroy the modified instance and swap it with a linked Prefab Instance
            GameObject oldInstance = fixer.gameObject;
            Undo.DestroyObjectImmediate(oldInstance);

            GameObject newInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset, parent);
            newInstance.transform.localPosition = pos;
            newInstance.transform.localRotation = rot;
            newInstance.transform.localScale = scl;
            newInstance.transform.SetSiblingIndex(siblingIndex);

            Undo.RegisterCreatedObjectUndo(newInstance, "Reload Prefab Instance");

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("BlenderMeshFixer", "Operation successful!\n\nThe 'Import Mesh' slot now points to the baked mesh.", "OK");
        }

        /// <summary>
        /// Generates target folders automatically if they are missing from the project structure.
        /// </summary>
        private static void EnsureFolderExists()
        {
            if (!AssetDatabase.IsValidFolder(SAVE_FOLDER))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "BlenderSavedPrefabs");
            if (!AssetDatabase.IsValidFolder(SAVE_FOLDER + "/Meshs"))
                AssetDatabase.CreateFolder(SAVE_FOLDER, "Meshs");
        }

        /// <summary>
        /// Safety check ensuring the GameObject actually contains geometry to look at.
        /// </summary>
        private static bool ValidateMeshFilter(MeshFilter mf)
        {
            if (mf == null || mf.sharedMesh == null)
            {
                EditorUtility.DisplayDialog("BlenderMeshFixer", "No MeshFilter / mesh found on this object.", "OK");
                return false;
            }
            return true;
        }
        #endregion
    }
}