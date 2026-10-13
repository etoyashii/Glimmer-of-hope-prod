using System.IO;
using UnityEditor;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;

namespace GlimmerOfHope.Editor
{
    public class UITexturesGenerators
    {
        private const int WHEEL_TEX_SIZE = 512;

        [MenuItem("Tools/GlimmerOfHope/GenerateHUEColorWheel")]
        public static void GenerateHUEColorWheel()
        {
            Texture2D texture = GenerateHUEColorWheelTexture(WHEEL_TEX_SIZE);

            string filePath = EditorUtility.SaveFilePanelInProject("ColorWheel", "ColorWheel", "png", "Save Color wheel");

            if (filePath == null || filePath == string.Empty)
                return;
            Debug.LogFormat("Save ColorWheel at {0}", filePath);
            AssetDatabase.Refresh();
            File.WriteAllBytes(filePath, texture.EncodeToPNG());
        }

        public static Texture2D GenerateHUEColorWheelTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            float center = size * 0.5f;
            float radius = center - 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > radius)
                    {
                        pixels[y * size + x] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    float hue = (Mathf.Atan2(dy, dx) / (Mathf.PI * 2f) + 1f) % 1f;
                    float sat = dist / radius;
                    Color c = Color.HSVToRGB(hue, sat, 1f);
                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return tex;
        }
    }
}
