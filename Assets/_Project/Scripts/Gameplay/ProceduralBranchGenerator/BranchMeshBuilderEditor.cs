using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor.branch
{
    [CustomEditor(typeof(BranchMeshBuilder))]
    public class BranchMeshBuilderEditor : UnityEditor.Editor
    {
        const string MeshRoot = "Assets/_Project/Art/Models";
        const string GeneratedFolder = "_Generated";
        const string OutputFolder = MeshRoot + "/" + GeneratedFolder;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var builder = (BranchMeshBuilder)target;

            EditorGUILayout.Space(10);

            var splines = builder.GetComponent<ProceduralBranchSplines>();
            if (splines != null && GUILayout.Button("Generate Branches", GUILayout.Height(36)))
            {
                splines.Generate();
                EditorUtility.SetDirty(splines);
            }

            EditorGUILayout.Space(4);

            // ── Build ──────────────────────────────────────────────────
            if (GUILayout.Button("Build Mesh", GUILayout.Height(36)))
            {
                builder.BuildMesh();
                EditorUtility.SetDirty(builder);
                var mf = builder.GetComponent<MeshFilter>();
                EditorUtility.SetDirty(mf);
                if (mf.sharedMesh != null)
                    EditorUtility.SetDirty(mf.sharedMesh);
            }

            // ── Stats ──────────────────────────────────────────────────
            if (builder.BakedMesh != null)
            {
                EditorGUILayout.HelpBox(
                    $"Mesh : {builder.BakedMesh.vertexCount} vertices  |  {builder.BakedMesh.triangles.Length / 3} triangles",
                    MessageType.Info
                );

                EditorGUILayout.Space(4);

                // ── Save as asset ──────────────────────────────────────
                if (GUILayout.Button("Sauvegarder le Mesh (.asset)", GUILayout.Height(30)))
                    SaveMeshAsset(builder);
            }
        }

        void SaveMeshAsset(BranchMeshBuilder builder)
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder))
                AssetDatabase.CreateFolder(MeshRoot, GeneratedFolder);

            string path = EditorUtility.SaveFilePanelInProject(
                "Sauvegarder le mesh",
                builder.BakedMesh.name,
                "asset",
                "Choisir où sauvegarder le mesh",
                OutputFolder
            );

            if (string.IsNullOrEmpty(path)) return;

            // Si un asset existe déjà à ce chemin, on l'écrase
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                // Copie les données dans l'asset existant (évite de casser les références)
                existing.Clear();
                existing.SetVertices(builder.BakedMesh.vertices);
                existing.SetUVs(0, builder.BakedMesh.uv);
                existing.SetTriangles(builder.BakedMesh.triangles, 0);
                existing.RecalculateNormals();
                existing.RecalculateBounds();
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();

                // Réassigne l'asset existant au MeshFilter
                builder.BakedMesh = existing;
                builder.GetComponent<MeshFilter>().sharedMesh = existing;
            }
            else
            {
                // Crée un nouvel asset
                var savedMesh = Object.Instantiate(builder.BakedMesh);
                savedMesh.name = builder.BakedMesh.name;
                AssetDatabase.CreateAsset(savedMesh, path);
                AssetDatabase.SaveAssets();

                // Réassigne l'asset sauvegardé (plus de mesh en mémoire volatile)
                builder.BakedMesh = savedMesh;
                builder.GetComponent<MeshFilter>().sharedMesh = savedMesh;
            }

            EditorUtility.SetDirty(builder);
            EditorUtility.SetDirty(builder.GetComponent<MeshFilter>());

            Debug.Log($"[BranchMeshBuilder] Mesh sauvegardé : {path}");
        }
    }
}