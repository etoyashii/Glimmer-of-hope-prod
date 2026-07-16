using UnityEngine;
using UnityEditor;
using System.IO;

namespace GlimmerOfHope.Editor.Characters
{
    public static class CharacterUIStyleGenerator
    {
        #region Constants

        private const string OUTPUT_PATH = "Assets/_Project/Art/Textures/UI/Characters";
        private const int SPRITE_PPU = 100;

        #endregion

        #region Menu

        [MenuItem("Tools/GlimmerOfHope/1 — Generate UI Style Assets")]
        public static void Generate()
        {
            EnsureFolder(OUTPUT_PATH);

            CreateRoundedRect("rounded-panel",       64, 64, 16, Color.white, 16);
            CreateRoundedRect("rounded-button",      48, 48, 12, Color.white, 14);
            CreateRoundedRect("rounded-card",        48, 48, 10, Color.white, 12);
            CreateRoundedRect("rounded-card-lg",     64, 64, 12, Color.white, 16);
            CreateShadow("shadow-soft",              96, 96, 14, 0.10f, 28);
            CreateSelectionBorder("selection-border", 64, 64, 12, 3, 16);
            CreateRoundedRect("tab-underline",       32,  6,  3, Color.white, 4);
            CreateCircle("circle-thumb",             128, 128);

            AssetDatabase.Refresh();

            int found = 0;
            string[] names = {
                "rounded-panel", "rounded-button", "rounded-card",
                "rounded-card-lg", "shadow-soft", "selection-border",
                "tab-underline", "circle-thumb"
            };
            foreach (var n in names)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{OUTPUT_PATH}/{n}.png");
                if (s != null) found++;
                else Debug.LogWarning($"[UIStyleGenerator] Sprite NON CHARGÉ : {n}");
            }

            Debug.Log($"[UIStyleGenerator] {found}/{names.Length} sprites OK dans {OUTPUT_PATH}");
            EditorUtility.DisplayDialog(
                "Style Assets générés",
                $"{found}/{names.Length} sprites créés dans :\n{OUTPUT_PATH}\n\n" +
                "Prochaine étape :\nTools → GlimmerOfHope → 2 — Generate Character UI Prefabs",
                "OK");
        }

        #endregion

        #region Generators

        private static void CreateRoundedRect(string name, int w, int h, int radius, Color fill, int border)
        {
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float a = RoundedAlpha(x, y, w, h, radius);
                    pixels[y * w + x] = new Color(fill.r, fill.g, fill.b, a);
                }

            SaveAndConfigure(name, w, h, pixels, border);
        }

        private static void CreateShadow(string name, int w, int h, int pad, float maxAlpha, int border)
        {
            var pixels = new Color[w * h];
            int innerW = w - pad * 2;
            int innerH = h - pad * 2;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dist = DistToRoundedRect(x, y, pad, pad, innerW, innerH, 12);
                    float a = dist <= 0 ? maxAlpha : Mathf.Lerp(maxAlpha, 0f, dist / pad);
                    pixels[y * w + x] = new Color(0f, 0f, 0f, a);
                }

            SaveAndConfigure(name, w, h, pixels, border);
        }

        private static void CreateSelectionBorder(string name, int w, int h, int radius, int thickness, int border)
        {
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float outer = RoundedAlpha(x, y, w, h, radius);
                    float inner = RoundedAlpha(
                        x - thickness, y - thickness,
                        w - thickness * 2, h - thickness * 2,
                        Mathf.Max(1, radius - thickness));
                    pixels[y * w + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(outer - inner));
                }

            SaveAndConfigure(name, w, h, pixels, border);
        }

        private static void CreateCircle(string name, int w, int h)
        {
            var pixels = new Color[w * h];
            float cx = w * 0.5f, cy = h * 0.5f;
            float r = Mathf.Min(cx, cy) - 1f;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float a = dist <= r - 0.5f ? 1f : dist >= r + 0.5f ? 0f : Mathf.Clamp01(r + 0.5f - dist);
                    pixels[y * w + x] = new Color(1f, 1f, 1f, a);
                }

            SaveAndConfigure(name, w, h, pixels, 0);
        }

        #endregion

        #region Pixel Math

        private static float RoundedAlpha(int x, int y, int w, int h, int r)
        {
            if (w <= 0 || h <= 0 || x < 0 || y < 0 || x >= w || y >= h) return 0f;

            int cx, cy;
            if      (x < r && y < r)               { cx = r;     cy = r; }
            else if (x >= w - r && y < r)           { cx = w - r; cy = r; }
            else if (x < r && y >= h - r)           { cx = r;     cy = h - r; }
            else if (x >= w - r && y >= h - r)      { cx = w - r; cy = h - r; }
            else return 1f;

            float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
            if (dist <= r - 0.5f) return 1f;
            if (dist >= r + 0.5f) return 0f;
            return Mathf.Clamp01(r + 0.5f - dist);
        }

        private static float DistToRoundedRect(int px, int py, int rx, int ry, int rw, int rh, int rr)
        {
            float dx = Mathf.Max(rx + rr - px, 0, px - (rx + rw - rr));
            float dy = Mathf.Max(ry + rr - py, 0, py - (ry + rh - rr));

            bool inCorner = dx > 0 && dy > 0;
            float cornerDist = inCorner ? Mathf.Sqrt(dx * dx + dy * dy) - rr : Mathf.Max(dx, dy) - rr;
            float rawDist = Mathf.Max(rx - px, px - (rx + rw), ry - py, py - (ry + rh));

            if (!inCorner && rawDist <= 0) return rawDist;
            return inCorner ? Mathf.Max(0, cornerDist) : Mathf.Max(0, rawDist);
        }

        #endregion

        #region Save & Configure

        private static void SaveAndConfigure(string name, int w, int h, Color[] pixels, int border)
        {
            // Write PNG
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();

            var path = $"{OUTPUT_PATH}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            // Force synchronous import so the TextureImporter is available NOW
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

            // Configure as 9-slice sprite
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[UIStyleGenerator] Importer null pour {name} — import raté.");
                return;
            }

            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = SPRITE_PPU;
            importer.filterMode          = FilterMode.Bilinear;
            importer.textureCompression  = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled       = false;

            if (border > 0)
                importer.spriteBorder = new Vector4(border, border, border, border);

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMode     = (int)SpriteImportMode.Single;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }

        #endregion

        #region Helpers

        private static void EnsureFolder(string path)
        {
            var parts   = path.Split('/');
            var current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        #endregion
    }
}
