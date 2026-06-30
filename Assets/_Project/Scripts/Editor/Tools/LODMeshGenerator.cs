using System.IO;
using UnityEditor;
using UnityEngine;
using UnityMeshSimplifier;

namespace GlimmerOfHope.Editor.Tools
{
    /// <summary>Decimates a mesh with UnityMeshSimplifier and caches the result as an asset.</summary>
    public static class LODMeshGenerator
    {
        /// <summary>
        /// Returns the simplified mesh for the given level (level 0 is the source).
        /// Reuses the cached asset if it already exists, otherwise generates and saves it.
        /// Needs Read/Write enabled on the source mesh, returns null if it is not readable.
        /// </summary>
        public static Mesh GetOrCreateLodMesh(Mesh source, int level, float quality)
        {
            if (source == null || !source.isReadable) return null;
            if (level <= 0) return source;

            string path = ResolveLodPath(source, level, quality);
            if (string.IsNullOrEmpty(path)) return Simplify(source, quality);

            // Already generated once: load it back instead of recomputing.
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;

            var simplified = Simplify(source, quality);
            if (simplified == null) return null;

            EnsureFolder(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(simplified, path);
            AssetDatabase.SaveAssets();
            return simplified;
        }

        private static Mesh Simplify(Mesh source, float quality)
        {
            var simplifier = new MeshSimplifier();
            // Keep silhouette and shape: border edges hold UV/seam integrity, curvature avoids flat-shading artefacts.
            simplifier.PreserveBorderEdges = true;
            simplifier.PreserveSurfaceCurvature = true;
            simplifier.Initialize(source);
            simplifier.SimplifyMesh(Mathf.Clamp01(quality)); // quality is the fraction of triangles to keep (0..1)

            var mesh = simplifier.ToMesh();
            mesh.name = source.name + "_q" + Mathf.RoundToInt(quality * 100f);
            mesh.RecalculateBounds();
            return mesh;
        }

        // Cache path lives next to the source mesh, under LOD_Generated, keyed by level + quality
        // so two strategies asking for the same quality reuse one asset instead of regenerating.
        private static string ResolveLodPath(Mesh source, int level, float quality)
        {
            string quality100 = Mathf.RoundToInt(quality * 100f).ToString();
            string fileName = source.name + "_LOD" + level + "_q" + quality100 + ".asset";

            string sourcePath = AssetDatabase.GetAssetPath(source);

            // Built-in/primitive ("Library/...") or runtime meshes have no editable source folder:
            // persist the LOD under a shared project folder instead of crashing on an out-of-Assets path.
            if (string.IsNullOrEmpty(sourcePath) || !sourcePath.StartsWith("Assets/"))
                return LODSettings.FALLBACK_FOLDER + "/" + fileName;

            string dir = Path.GetDirectoryName(sourcePath).Replace("\\", "/");
            return dir + "/" + LODSettings.GENERATED_FOLDER + "/" + fileName;
        }

        // AssetDatabase.CreateFolder only makes one level at a time, so recurse to create the parents first.
        private static void EnsureFolder(string folder)
        {
            folder = folder.Replace("\\", "/");
            if (AssetDatabase.IsValidFolder(folder)) return;

            string parent = Path.GetDirectoryName(folder).Replace("\\", "/");
            string leaf = Path.GetFileName(folder);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
