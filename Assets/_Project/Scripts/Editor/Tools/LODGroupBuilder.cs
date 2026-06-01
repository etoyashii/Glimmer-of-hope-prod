using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor.Tools
{
    /// <summary>Turns one candidate into a working LODGroup: three child renderers plus the component.</summary>
    public static class LODGroupBuilder
    {
        /// <summary>
        /// Builds the LOD0/1/2 children and the LODGroup on the candidate's object.
        /// Pass useUndo = true for scene objects (Ctrl+Z works), false when editing prefab contents.
        /// Returns false if the candidate cannot be processed (skinned, no mesh, not readable, Skip).
        /// </summary>
        public static bool Build(LODCandidate candidate, bool useUndo)
        {
            if (candidate.IsSkinned || candidate.SourceMesh == null) return false;
            if (candidate.Chosen == LODStrategy.Skip) return false;
            if (!candidate.SourceMesh.isReadable) return false;

            var root = candidate.Renderer.gameObject;
            CleanExisting(root, useUndo);

            var lod1Mesh = LODMeshGenerator.GetOrCreateLodMesh(candidate.SourceMesh, 1, LODSettings.LOD1_QUALITY);
            var lod2Mesh = LODMeshGenerator.GetOrCreateLodMesh(candidate.SourceMesh, 2, LODSettings.LOD2_QUALITY);
            if (lod1Mesh == null || lod2Mesh == null) return false;

            var materials = candidate.Renderer.sharedMaterials;
            var r0 = BuildChild(root, LODSettings.LOD0_SUFFIX, candidate.SourceMesh, materials, candidate.Renderer, useUndo);
            var r1 = BuildChild(root, LODSettings.LOD1_SUFFIX, lod1Mesh, materials, candidate.Renderer, useUndo);
            var r2 = BuildChild(root, LODSettings.LOD2_SUFFIX, lod2Mesh, materials, candidate.Renderer, useUndo);

            StripRootRenderer(root, useUndo);
            ConfigureGroup(root, candidate.Chosen, r0, r1, r2, useUndo);
            return true;
        }

        private static void ConfigureGroup(GameObject root, LODStrategy strategy, Renderer r0, Renderer r1, Renderer r2, bool useUndo)
        {
            var group = root.GetComponent<LODGroup>();
            if (group == null)
                group = useUndo ? Undo.AddComponent<LODGroup>(root) : root.AddComponent<LODGroup>();

            float[] heights = ResolveHeights(strategy);
            var lods = new[]
            {
                new LOD(heights[0], new[] { r0 }),
                new LOD(heights[1], new[] { r1 }),
                new LOD(heights[2], new[] { r2 })
            };

            group.fadeMode = LODFadeMode.CrossFade;
            group.animateCrossFading = true;
            group.SetLODs(lods);
            group.RecalculateBounds();
        }

        private static float[] ResolveHeights(LODStrategy strategy)
        {
            if (strategy == LODStrategy.AlwaysVisible)
                return new[] { LODSettings.VISIBLE_LOD0, LODSettings.VISIBLE_LOD1, LODSettings.VISIBLE_LOD2 };
            return new[] { LODSettings.CULL_LOD0, LODSettings.CULL_LOD1, LODSettings.CULL_LOD2 };
        }

        private static Renderer BuildChild(GameObject root, string suffix, Mesh mesh, Material[] materials, Renderer source, bool useUndo)
        {
            var child = new GameObject(root.name + suffix);
            if (useUndo) Undo.RegisterCreatedObjectUndo(child, "Build LOD");
            child.transform.SetParent(root.transform, false);
            child.layer = source.gameObject.layer;

            var filter = child.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = source.shadowCastingMode;
            renderer.receiveShadows = source.receiveShadows;
            return renderer;
        }

        // The original renderer must go, otherwise it draws on top of the LOD children.
        private static void StripRootRenderer(GameObject root, bool useUndo)
        {
            var renderer = root.GetComponent<MeshRenderer>();
            var filter = root.GetComponent<MeshFilter>();
            if (renderer != null) Remove(renderer, useUndo);
            if (filter != null) Remove(filter, useUndo);
        }

        private static void CleanExisting(GameObject root, bool useUndo)
        {
            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                var child = root.transform.GetChild(i).gameObject;
                if (IsLodChild(child.name)) Remove(child, useUndo);
            }

            var group = root.GetComponent<LODGroup>();
            if (group != null) Remove(group, useUndo);
        }

        private static bool IsLodChild(string name)
        {
            return name.EndsWith(LODSettings.LOD0_SUFFIX)
                || name.EndsWith(LODSettings.LOD1_SUFFIX)
                || name.EndsWith(LODSettings.LOD2_SUFFIX);
        }

        private static void Remove(Object target, bool useUndo)
        {
            if (useUndo) Undo.DestroyObjectImmediate(target);
            else Object.DestroyImmediate(target, true);
        }
    }
}
