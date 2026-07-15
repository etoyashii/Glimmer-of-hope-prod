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

            // A generated LOD is never a valid source. Deriving from it produces a LOD of a LOD, which
            // is both meaningless and unbounded. The classifier already filters these out; this is the
            // second, independent guard, so that a direct caller cannot start the recursion either.
            if (IsGenerated(source))
            {
                Debug.LogError(
                    "[LOD] Refus de generer un LOD a partir d'un mesh deja genere : \"" + source.name + "\".\n" +
                    "Un LOD de LOD n'a pas de sens et la chaine ne s'arrete jamais. " +
                    "Repartez du mesh source d'origine.");
                return null;
            }

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

        /// <summary>True if the mesh is one of our own generated LODs (it lives under the generated root).</summary>
        public static bool IsGenerated(Mesh mesh)
        {
            if (mesh == null) return false;

            string path = AssetDatabase.GetAssetPath(mesh);
            if (string.IsNullOrEmpty(path)) return false;

            return path.Replace("\\", "/").StartsWith(LODSettings.GENERATED_ROOT + "/");
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

        /// <summary>
        /// Where the generated asset goes. Flat, under a single root, and independent of where the source
        /// lives — see LODSettings.GENERATED_ROOT for why that independence is not negotiable.
        ///
        /// The file name is keyed by the GUID *and* the local file id of the source. The GUID alone is not
        /// enough: one FBX is a single GUID holding many meshes, and two submeshes exported with the same
        /// name (two "Cube", which DCC tools produce all the time) would resolve to the same path. The
        /// cache would then hand back the first mesh's LOD for the second mesh, silently and wrongly.
        /// </summary>
        private static string ResolveLodPath(Mesh source, int level, float quality)
        {
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long localId))
                return null; // runtime/primitive mesh with no asset on disk

            string quality100 = Mathf.RoundToInt(quality * 100f).ToString();
            string fileName = Sanitize(source.name) + "_" + guid + "_" + localId
                              + "_LOD" + level + "_q" + quality100 + ".asset";

            return LODSettings.GENERATED_ROOT + "/" + fileName;
        }

        // The source name is only there to keep the folder readable by a human; the GUID and the local id
        // carry the identity. Anything that is not a plain ASCII word would break the naming convention.
        private static string Sanitize(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Mesh";

            var chars = name.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';
                if (!ok) chars[i] = '_';
            }
            return new string(chars);
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
