using System.IO;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.Editor
{
    public static class GrayZoneTestMaskGenerator
    {
        const int Size = 256;
        const string SavePath = "Assets/Data/GrayZone/TestMaskArray.asset";

        [MenuItem("Glimmer/Corruption/Generate Test Mask Array")]
        public static void Generate()
        {
            // 4 slices pratiques pour tester threshold / maskIndex :
            // 0 = plein blanc  -> zone toujours grise, quel que soit le threshold
            // 1 = plein noir   -> zone jamais grise
            // 2 = dégradé radial -> bon pour tester différents thresholds sur une même zone
            // 3 = damier       -> bon pour vérifier visuellement que maskIndex pointe la bonne slice
            Texture2D[] slices =
            {
                MakeSolid(1f),
                MakeSolid(0f),
                MakeRadialGradient(),
                MakeCheckerboard(8),
            };

            var array = new Texture2DArray(Size, Size, slices.Length, TextureFormat.R8, false, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            for (int i = 0; i < slices.Length; i++)
            {
                Graphics.CopyTexture(slices[i], 0, 0, array, i, 0);
                Object.DestroyImmediate(slices[i]);
            }
            array.Apply(false, false);

            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));

            var existing = AssetDatabase.LoadAssetAtPath<Texture2DArray>(SavePath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(SavePath);
            }

            AssetDatabase.CreateAsset(array, SavePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = array;
            EditorGUIUtility.PingObject(array);

        }

        static Texture2D MakeSolid(float value)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.R8, false, true);
            var pixels = new Color[Size * Size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color(value, value, value, 1f);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        static Texture2D MakeRadialGradient()
        {
            var tex = new Texture2D(Size, Size, TextureFormat.R8, false, true);
            var center = new Vector2(Size / 2f, Size / 2f);
            float maxDist = Size / 2f;
            var pixels = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float v = Mathf.Clamp01(1f - dist / maxDist);
                    pixels[y * Size + x] = new Color(v, v, v, 1f);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        static Texture2D MakeCheckerboard(int cells)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.R8, false, true);
            int cellSize = Mathf.Max(1, Size / cells);
            var pixels = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    bool on = ((x / cellSize) + (y / cellSize)) % 2 == 0;
                    float v = on ? 1f : 0f;
                    pixels[y * Size + x] = new Color(v, v, v, 1f);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}