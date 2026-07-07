using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor.Tools
{
    /// <summary>Decides where each candidate gets built: scene object, prefab asset, or model asset.</summary>
    public static class LODApplier
    {
        /// <summary>Builds every applicable candidate and returns how many were processed.</summary>
        public static int Apply(IReadOnlyList<LODCandidate> candidates, bool preferPrefab)
        {
            var enabledPaths = EnableReadWriteForBlocked(candidates);
            try
            {
                int applied = 0;
                foreach (var c in candidates)
                {
                    if (!IsApplicable(c)) continue;
                    if (ApplyOne(c, preferPrefab)) applied++;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return applied;
            }
            finally
            {
                RestoreReadWrite(enabledPaths);
            }
        }

        private static List<string> EnableReadWriteForBlocked(IReadOnlyList<LODCandidate> candidates)
        {
            var paths = new List<string>();
            foreach (var c in candidates)
            {
                if (!c.Enabled || c.Chosen == LODStrategy.Skip) continue;
                if (c.IsSkinned || c.SourceMesh == null || c.IsReadable) continue;

                string path = AssetDatabase.GetAssetPath(c.SourceMesh);
                if (!string.IsNullOrEmpty(path) && !paths.Contains(path)) paths.Add(path);
            }

            var flipped = new List<string>();
            foreach (string path in paths)
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null || importer.isReadable) continue;

                importer.isReadable = true;
                importer.SaveAndReimport();
                flipped.Add(path);
            }

            if (flipped.Count > 0) RefreshReadability(candidates);
            return flipped;
        }

        private static void RefreshReadability(IReadOnlyList<LODCandidate> candidates)
        {
            foreach (var c in candidates)
            {
                if (c.Renderer == null) continue;

                var mesh = LODClassifier.ResolveMesh(c.Renderer);
                c.SourceMesh = mesh;
                c.IsReadable = mesh != null && mesh.isReadable;
            }
        }

        private static void RestoreReadWrite(List<string> paths)
        {
            if (paths.Count == 0) return;

            foreach (string path in paths)
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null || !importer.isReadable) continue;

                importer.isReadable = false;
                importer.SaveAndReimport();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static bool IsApplicable(LODCandidate c)
        {
            if (!c.Enabled || c.Chosen == LODStrategy.Skip) return false;
            if (c.IsSkinned || c.SourceMesh == null) return false;
            return c.IsReadable;
        }

        // Three cases: a renderer already on disk (model), a prefab instance we edit at the source,
        // or a plain scene object. Editing a prefab asset cannot be undone with Ctrl+Z.
        private static bool ApplyOne(LODCandidate c, bool preferPrefab)
        {
            // Case 1 — renderer lives directly in an asset (e.g. selected model file): edit that asset.
            if (EditorUtility.IsPersistent(c.Renderer))
                return ApplyByPath(c, AssetDatabase.GetAssetPath(c.Renderer), c.Renderer.transform.root);

            // Case 2 — scene object that is a prefab instance: edit the prefab source so every instance benefits.
            if (preferPrefab && PrefabUtility.IsPartOfPrefabInstance(c.Renderer))
            {
                var root = PrefabUtility.GetNearestPrefabInstanceRoot(c.Renderer.gameObject);
                string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root);
                if (!string.IsNullOrEmpty(path))
                    return ApplyByPath(c, path, root.transform);
            }

            // Case 3 — plain scene object (or prefab editing turned off): build in place, with undo support.
            return LODGroupBuilder.Build(c, true);
        }

        // Open the prefab/model in an isolated editing scene, find the matching renderer inside it by its
        // path relative to the root, build the LODGroup there, then save and always unload the contents.
        private static bool ApplyByPath(LODCandidate c, string path, Transform anchorRoot)
        {
            // The scene renderer and the in-asset renderer are different objects; the relative path is what links them.
            string relativePath = RelativePath(anchorRoot, c.Renderer.transform);
            var contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var target = FindRenderer(contents, relativePath);
                if (target == null) return false;

                // Rebind the candidate to the in-asset renderer; useUndo = false because asset edits are not undoable.
                var clone = CloneCandidate(c, target);
                if (!LODGroupBuilder.Build(clone, false)) return false;

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                return true;
            }
            finally
            {
                // Must run even on early return, otherwise the loaded prefab scene leaks.
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static LODCandidate CloneCandidate(LODCandidate source, Renderer target)
        {
            var mesh = LODClassifier.ResolveMesh(target);
            return new LODCandidate
            {
                Renderer = target,
                SourceMesh = mesh,
                Chosen = source.Chosen,
                IsSkinned = target is SkinnedMeshRenderer,
                IsReadable = mesh != null && mesh.isReadable,
                Enabled = true
            };
        }

        private static Renderer FindRenderer(GameObject root, string path)
        {
            Transform t = string.IsNullOrEmpty(path) ? root.transform : root.transform.Find(path);
            if (t == null) return null;

            var renderer = t.GetComponent<Renderer>();
            return renderer != null ? renderer : t.GetComponentInChildren<Renderer>();
        }

        // Walk parents from target up to root collecting names, then reverse into a "Child/Sub/Leaf" path
        // that Transform.Find can resolve inside the loaded prefab contents.
        private static string RelativePath(Transform root, Transform target)
        {
            if (target == root || root == null) return string.Empty;

            var segments = new List<string>();
            var current = target;
            while (current != null && current != root)
            {
                segments.Add(current.name);
                current = current.parent;
            }

            segments.Reverse();
            return string.Join("/", segments);
        }
    }
}
