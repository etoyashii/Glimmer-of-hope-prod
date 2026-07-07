using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Editor.Tools
{
    /// <summary>Looks at renderers and guesses a strategy for each. The UI can still override every choice.</summary>
    public static class LODClassifier
    {
        /// <summary>Builds one candidate per renderer, with triangle count, size and a recommended strategy.</summary>
        public static List<LODCandidate> Scan(IReadOnlyList<Renderer> renderers)
        {
            // Count shared-mesh reuse first: a mesh used many times is mass-instanced
            // decoration (grass, foliage) and gets a different strategy than a one-off.
            var instanceCounts = CountInstances(renderers);
            var candidates = new List<LODCandidate>();

            foreach (var renderer in renderers)
            {
                var candidate = BuildCandidate(renderer, instanceCounts);
                if (candidate != null) candidates.Add(candidate);
            }

            return candidates;
        }

        /// <summary>Gets the mesh behind a renderer, whether it is a SkinnedMeshRenderer or a MeshFilter.</summary>
        public static Mesh ResolveMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned) return skinned.sharedMesh;
            var filter = renderer.GetComponent<MeshFilter>();
            return filter != null ? filter.sharedMesh : null;
        }

        private static Dictionary<Mesh, int> CountInstances(IReadOnlyList<Renderer> renderers)
        {
            var counts = new Dictionary<Mesh, int>();
            foreach (var renderer in renderers)
            {
                var mesh = ResolveMesh(renderer);
                if (mesh == null) continue;
                counts.TryGetValue(mesh, out int current);
                counts[mesh] = current + 1;
            }
            return counts;
        }

        private static LODCandidate BuildCandidate(Renderer renderer, Dictionary<Mesh, int> instanceCounts)
        {
            if (renderer == null) return null;

            var mesh = ResolveMesh(renderer);
            var candidate = new LODCandidate
            {
                Renderer = renderer,
                SourceMesh = mesh,
                IsSkinned = renderer is SkinnedMeshRenderer,
                Enabled = true
            };

            if (mesh == null)
            {
                candidate.Blocked = true;
                candidate.Reason = "Pas de mesh";
                candidate.Recommended = LODStrategy.Skip;
                candidate.Chosen = LODStrategy.Skip;
                return candidate;
            }

            candidate.TriangleCount = CountTriangles(mesh);
            candidate.BoundsSize = renderer.bounds.size.magnitude;
            candidate.InstanceCount = instanceCounts.TryGetValue(mesh, out int c) ? c : 1;
            candidate.IsReadable = mesh.isReadable;
            // A parent LODGroup means this object was already processed; the UI flags it as a rebuild.
            candidate.AlreadyProcessed = renderer.GetComponentInParent<LODGroup>() != null;
            candidate.Recommended = Recommend(candidate);
            candidate.Chosen = candidate.Recommended;
            return candidate;
        }

        // Order matters: the first matching rule wins. Mass-instanced decoration is caught before the
        // small/low-poly skips, otherwise thousands of tiny foliage instances get skipped instead of culled.
        private static LODStrategy Recommend(LODCandidate c)
        {
            if (c.IsSkinned) { c.Reason = "Mesh skinné"; return LODStrategy.Skip; }     // animated meshes break under simplification
            if (IsUI(c.Renderer)) { c.Reason = "Layer UI"; return LODStrategy.Skip; }   // UI never benefits from distance LOD

            // Mass-instanced decoration: the win is distance culling and it scales with the count, so it
            // beats the size/triangle skips (grass, bushes, small flowers are exactly what must be culled).
            if (c.InstanceCount >= LODSettings.MASS_INSTANCE_COUNT || NameMatches(c.Renderer.name, LODSettings.MASS_KEYWORDS))
            {
                c.Reason = "Déco mass-instanciée";
                return LODStrategy.LODGroup;
            }

            if (c.TriangleCount < LODSettings.MIN_TRIANGLES) { c.Reason = "Trop peu de triangles"; return LODStrategy.Skip; }
            if (c.BoundsSize < LODSettings.MIN_BOUNDS) { c.Reason = "Objet minuscule"; return LODStrategy.Skip; }

            // Large structures (ground, walls...) stay on screen, so keep a low LOD instead of culling them to nothing.
            if (c.BoundsSize >= LODSettings.LARGE_BOUNDS || NameMatches(c.Renderer.name, LODSettings.STRUCTURE_KEYWORDS))
            {
                c.Reason = "Structure large";
                return LODStrategy.AlwaysVisible;
            }

            // Heavy hero mesh: worth LODs but never fully culled, the player will look right at it.
            if (c.TriangleCount >= LODSettings.ALWAYS_VISIBLE_TRIANGLES)
            {
                c.Reason = "Mesh lourd";
                return LODStrategy.AlwaysVisible;
            }

            // Medium mesh seen once: decimating it costs more than it saves.
            c.Reason = "Pas de gain attendu";
            return LODStrategy.Skip;
        }

        // Sum indices across every submesh, then divide by 3 — a triangle is 3 indices.
        private static int CountTriangles(Mesh mesh)
        {
            long indices = 0;
            for (int i = 0; i < mesh.subMeshCount; i++)
                indices += mesh.GetIndexCount(i);
            return (int)(indices / 3);
        }

        private static bool IsUI(Renderer renderer)
        {
            return renderer.GetComponent<RectTransform>() != null
                || renderer.gameObject.layer == LayerMask.NameToLayer("UI");
        }

        private static bool NameMatches(string name, string[] keywords)
        {
            string lower = name.ToLowerInvariant();
            foreach (var keyword in keywords)
                if (lower.Contains(keyword)) return true;
            return false;
        }
    }
}
