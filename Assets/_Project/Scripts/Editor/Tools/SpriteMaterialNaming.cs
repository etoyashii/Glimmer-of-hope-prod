using System;

namespace GlimmerOfHope.Editor.Tools
{
    public static class SpriteMaterialNaming
    {
        public static string ToMaterialName(string textureName)
        {
            if (string.IsNullOrEmpty(textureName))
                return string.Empty;

            string core = StripTexturePrefix(textureName);

            if (core.Length == 0)
                core = textureName;

            if (core.StartsWith(SpriteMaterialSettings.MATERIAL_PREFIX, StringComparison.Ordinal))
                return core;

            return SpriteMaterialSettings.MATERIAL_PREFIX + core;
        }

        public static string ToMaterialPath(string outputFolder, string textureName)
        {
            return outputFolder
                + "/"
                + ToMaterialName(textureName)
                + SpriteMaterialSettings.MATERIAL_EXTENSION;
        }

        private static string StripTexturePrefix(string textureName)
        {
            foreach (string prefix in SpriteMaterialSettings.TEXTURE_PREFIXES)
            {
                if (textureName.StartsWith(prefix, StringComparison.Ordinal))
                    return textureName.Substring(prefix.Length);
            }

            return textureName;
        }
    }
}
