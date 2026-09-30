using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GlimmerOfHope.Editor.Tools
{
    public static class SpriteMaterialShaderProbe
    {
        public static bool HasTextureProperty(Shader shader, string propertyName)
        {
            if (shader == null || string.IsNullOrEmpty(propertyName))
                return false;

            int index = shader.FindPropertyIndex(propertyName);

            if (index < 0)
                return false;

            return shader.GetPropertyType(index) == ShaderPropertyType.Texture;
        }

        public static List<string> ListTextureProperties(Shader shader)
        {
            var names = new List<string>();

            if (shader == null)
                return names;

            int count = shader.GetPropertyCount();

            for (int i = 0; i < count; i++)
            {
                if (shader.GetPropertyType(i) == ShaderPropertyType.Texture)
                    names.Add(shader.GetPropertyName(i));
            }

            return names;
        }
    }
}
