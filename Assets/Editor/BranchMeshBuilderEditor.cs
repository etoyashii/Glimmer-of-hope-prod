using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BranchMeshBuilder))]
public class BranchMeshBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var builder = (BranchMeshBuilder)target;

        EditorGUILayout.Space(10);

        // ── Build ──────────────────────────────────────────────────
        if (GUILayout.Button("Build Mesh", GUILayout.Height(36)))
        {
            builder.BuildMesh();
            EditorUtility.SetDirty(builder);
            EditorUtility.SetDirty(builder.GetComponent<MeshFilter>());
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
        // Propose un chemin par défaut dans Assets/Meshes/
        string defaultPath = $"Assets/Meshes/{builder.BakedMesh.name}.asset";
        string path = EditorUtility.SaveFilePanelInProject(
            "Sauvegarder le mesh",
            builder.BakedMesh.name,
            "asset",
            "Choisir où sauvegarder le mesh",
            "Assets/Meshes"
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
            builder.GetComponent<MeshFilter>().mesh = existing;
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