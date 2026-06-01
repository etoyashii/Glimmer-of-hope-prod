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
            simplifier.PreserveBorderEdges = true;
            simplifier.PreserveSurfaceCurvature = true;
            simplifier.Initialize(source);
            simplifier.SimplifyMesh(Mathf.Clamp01(quality));

            var mesh = simplifier.ToMesh();
            mesh.name = source.name + "_q" + Mathf.RoundToInt(quality * 100f);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static string ResolveLodPath(Mesh source, int level, float quality)
        {
            string sourcePath = AssetDatabase.GetAssetPath(source);
            if (string.IsNullOrEmpty(sourcePath)) return null;

            string dir = Path.GetDirectoryName(sourcePath).Replace("\\", "/");
            string quality100 = Mathf.RoundToInt(quality * 100f).ToString();
            string fileName = source.name + "_LOD" + level + "_q" + quality100 + ".asset";
            return dir + "/" + LODSettings.GENERATED_FOLDER + "/" + fileName;
        }

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
