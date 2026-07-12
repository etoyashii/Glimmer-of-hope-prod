using UnityEngine;

namespace GlimmerOfHope.Editor.Tools
{
    /// <summary>
    /// What the tool decides to do with a renderer.
    /// Skip = leave it alone, LODGroup = 3 levels then cull when tiny,
    /// AlwaysVisible = 3 levels but never culled (kept for structural meshes).
    /// </summary>
    public enum LODStrategy
    {
        Skip,
        LODGroup,
        AlwaysVisible
    }

    /// <summary>One renderer the tool looked at, plus everything the UI needs to show and apply it.</summary>
    public sealed class LODCandidate
    {
        public Renderer Renderer;
        public Mesh SourceMesh;
        public int TriangleCount;
        public float BoundsSize;
        public int InstanceCount;
        public bool IsSkinned;
        public bool IsReadable;
        public bool AlreadyProcessed;
        public bool Blocked;
        public string Reason;
        public LODStrategy Recommended;
        public LODStrategy Chosen;
        public bool Enabled;
    }

    /// <summary>All tuning values live here. Change thresholds and keywords in this file only.</summary>
    public static class LODSettings
    {
        // Classification thresholds. Below MIN or above the upper ones, the strategy flips.
        public const int MIN_TRIANGLES = 200;
        public const int ALWAYS_VISIBLE_TRIANGLES = 1500;
        public const float MIN_BOUNDS = 0.5f;
        public const float LARGE_BOUNDS = 12f;
        public const int MASS_INSTANCE_COUNT = 8;

        // Share of triangles kept per level (1 = source mesh, not stored here).
        public const float LOD1_QUALITY = 0.5f;
        public const float LOD2_QUALITY = 0.2f;

        // Screen-relative heights where each level takes over. Last value = cull point.
        public const float CULL_LOD0 = 0.5f;
        public const float CULL_LOD1 = 0.25f;
        public const float CULL_LOD2 = 0.08f;

        // Same idea but the last level reaches 0 so the object is never culled.
        public const float VISIBLE_LOD0 = 0.5f;
        public const float VISIBLE_LOD1 = 0.2f;
        public const float VISIBLE_LOD2 = 0f;

        public const string LOD0_SUFFIX = "_LOD0";
        public const string LOD1_SUFFIX = "_LOD1";
        public const string LOD2_SUFFIX = "_LOD2";

        // Every generated mesh lands here, and nowhere else. The destination of a generated asset must
        // NEVER derive from the folder of its source: writing next to the source puts the output back
        // inside the scanned tree, the next pass picks it up as a candidate, and the tool builds LODs
        // of its own LODs — nesting one folder deeper on each run.
        public const string GENERATED_ROOT = "Assets/_Project/Art/Mesh/_Generated/LOD";

        public static readonly string[] MASS_KEYWORDS =
        {
            "grass", "fleur", "herbe", "buisson", "flower", "arbre", "bush", "tree", "plant"
        };

        public static readonly string[] STRUCTURE_KEYWORDS =
        {
            "ground", "road", "wall", "platform", "mountain", "word", "floor", "terrain"
        };
    }
}
