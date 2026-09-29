using System.Collections.Generic;

namespace GlimmerOfHope.Editor.Tools
{
    public enum SpriteMaterialAction
    {
        Create,
        Update,
        Skip
    }

    public sealed class SpriteMaterialEntry
    {
        public string TextureGuid;
        public string TexturePath;
        public string TextureName;
        public string MaterialPath;
        public string MaterialName;
        public SpriteMaterialAction Action;
        public string Reason;
        public bool Enabled = true;
        public bool Applied;
    }

    public sealed class SpriteMaterialRequest
    {
        public string TextureFolder;
        public string OutputFolder;
        public bool Recursive = true;
        public bool Overwrite;
        public string[] ExcludedSuffixes = new string[0];
    }

    public sealed class SpriteMaterialReport
    {
        public int Created;
        public int Updated;
        public int Skipped;
        public int Failed;
        public bool Cancelled;
        public readonly List<string> Errors = new List<string>();
    }

    public static class SpriteMaterialSettings
    {
        public const string DEFAULT_TEXTURE_PROPERTY = "_Sprite";
        public const string MATERIAL_PREFIX = "M_";
        public const string MATERIAL_EXTENSION = ".mat";
        public const string MANIFEST_FILE = "SpriteMaterials.manifest.json";
        public const string GENERATED_ROOT = "Assets/_Generated";
        public const string MENU_PATH = "Tools/GlimmerOfHope/Sprite Material Generator";
        public const string LOG_PREFIX = "[SpriteMaterialGenerator] ";

        public const int MAX_ERRORS_LOGGED = 20;

        public static readonly string[] TEXTURE_PREFIXES = { "T_SP_", "T_" };
    }
}
