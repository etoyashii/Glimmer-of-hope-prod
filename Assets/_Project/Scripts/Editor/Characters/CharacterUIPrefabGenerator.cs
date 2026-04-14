using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace GlimmerOfHope.Editor.Characters
{
    public static class CharacterUIPrefabGenerator
    {
        #region Constants

        private const string OUTPUT_PATH = "Assets/_Project/Prefabs/UI/Characters";

        private static readonly Color COLOR_ICON_BG         = new Color(0.94f,  0.95f,  0.98f);
        private static readonly Color COLOR_TEXT_MUTED      = new Color(0.478f, 0.522f, 0.627f);
        private static readonly Color COLOR_TEXT_PRIMARY    = new Color(0.173f, 0.133f, 0.208f);
        private static readonly Color COLOR_PART_BTN_BG     = new Color(0.965f, 0.973f, 0.992f);
        private static readonly Color COLOR_PART_HIGHLIGHT  = new Color(0.867f, 0.933f, 1f);
        private static readonly Color COLOR_PART_PRESSED    = new Color(0.722f, 0.847f, 0.961f);
        private static readonly Color COLOR_THUMB_PLACEHOLDER = new Color(0.8f, 0.8f, 0.8f);

        #endregion

        #region Menu

        [MenuItem("Tools/GlimmerOfHope/Generate Character UI Prefabs")]
        public static void Generate()
        {
            EnsureOutputFolder();

            var tempCanvas = CreateTemporaryCanvas();

            try
            {
                BuildCategoryTabPrefab(tempCanvas);
                BuildPartButtonPrefab(tempCanvas);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog(
                    "Prefabs générés ✔",
                    $"Deux prefabs créés dans :\n{OUTPUT_PATH}\n\n" +
                    "• CategoryTabPrefab\n" +
                    "• PartButtonPrefab\n\n" +
                    "Assigne-les dans CharacterCreatorUI :\n" +
                    "_categoryTabPrefab → CategoryTabPrefab\n" +
                    "_partButtonPrefab  → PartButtonPrefab",
                    "OK");
            }
            finally
            {
                Object.DestroyImmediate(tempCanvas);
            }
        }

        #endregion

        #region Temporary Canvas

        private static GameObject CreateTemporaryCanvas()
        {
            var go     = new GameObject("__TempCanvas__");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            go.hideFlags = HideFlags.HideAndDontSave;
            return go;
        }

        #endregion

        #region CategoryTabPrefab

        private static void BuildCategoryTabPrefab(GameObject canvasParent)
        {
            var root = CreateUIChild("CategoryTabPrefab", canvasParent);
            SetSize(root, new Vector2(72f, 80f));

            var rootImg = root.AddComponent<Image>();
            rootImg.color         = Color.clear;
            rootImg.raycastTarget = true;

            // Button
            var btn    = root.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor      = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(0.933f, 0.961f, 0.988f, 1f);
            colors.pressedColor     = new Color(0.839f, 0.914f, 0.973f, 1f);
            colors.selectedColor    = new Color(0.933f, 0.961f, 0.988f, 1f);
            colors.fadeDuration     = 0.1f;
            btn.colors              = colors;
            btn.targetGraphic       = rootImg;

            // IconBg
            var iconBg = CreateUIChild("IconBg", root);
            var iconBgRt = iconBg.GetComponent<RectTransform>();
            iconBgRt.anchorMin        = new Vector2(0.5f, 1f);
            iconBgRt.anchorMax        = new Vector2(0.5f, 1f);
            iconBgRt.pivot            = new Vector2(0.5f, 1f);
            iconBgRt.anchoredPosition = new Vector2(0f, -10f);
            iconBgRt.sizeDelta        = new Vector2(44f, 44f);
            var iconBgImg = iconBg.AddComponent<Image>();
            iconBgImg.color         = COLOR_ICON_BG;
            iconBgImg.raycastTarget = false;

            // Icon
            var icon   = CreateUIChild("Icon", iconBg);
            var iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.pivot     = new Vector2(0.5f, 0.5f);
            iconRt.offsetMin = new Vector2(8f, 8f);
            iconRt.offsetMax = new Vector2(-8f, -8f);
            var iconImg = icon.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.color          = COLOR_TEXT_PRIMARY;
            iconImg.raycastTarget  = false;

            // Label
            var label   = CreateUIChild("Label", root);
            var labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin        = new Vector2(0f, 0f);
            labelRt.anchorMax        = new Vector2(1f, 0f);
            labelRt.pivot            = new Vector2(0.5f, 0f);
            labelRt.anchoredPosition = new Vector2(0f, 6f);
            labelRt.sizeDelta        = new Vector2(0f, 16f);
            var tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text          = "Catégorie";
            tmp.fontSize      = 10f;
            tmp.fontStyle     = FontStyles.Bold;
            tmp.color         = COLOR_TEXT_MUTED;
            tmp.alignment     = TextAlignmentOptions.Center;
            tmp.overflowMode  = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;

            SavePrefab(root, "CategoryTabPrefab");
        }

        #endregion

        #region PartButtonPrefab

        private static void BuildPartButtonPrefab(GameObject canvasParent)
        {
            var root = CreateUIChild("PartButtonPrefab", canvasParent);
            SetSize(root, new Vector2(78f, 78f));

            // Image de fond
            var rootImg = root.AddComponent<Image>();
            rootImg.color         = COLOR_PART_BTN_BG;
            rootImg.raycastTarget = true;

            // Button
            var btn    = root.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor      = COLOR_PART_BTN_BG;
            colors.highlightedColor = COLOR_PART_HIGHLIGHT;
            colors.pressedColor     = COLOR_PART_PRESSED;
            colors.selectedColor    = COLOR_PART_HIGHLIGHT;
            colors.fadeDuration     = 0.1f;
            btn.colors              = colors;
            btn.targetGraphic       = rootImg;

            // Thumbnail
            var thumb   = CreateUIChild("Thumbnail", root);
            var thumbRt = thumb.GetComponent<RectTransform>();
            thumbRt.anchorMin = new Vector2(0f, 1f);
            thumbRt.anchorMax = new Vector2(1f, 1f);
            thumbRt.pivot     = new Vector2(0.5f, 1f);
            thumbRt.offsetMin = new Vector2(8f, -62f);
            thumbRt.offsetMax = new Vector2(-8f, -6f);
            var thumbImg = thumb.AddComponent<Image>();
            thumbImg.preserveAspect = true;
            thumbImg.color          = COLOR_THUMB_PLACEHOLDER;
            thumbImg.raycastTarget  = false;

            // Label
            var label   = CreateUIChild("Label", root);
            var labelRt = label.GetComponent<RectTransform>();
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 0f);
            labelRt.pivot     = new Vector2(0.5f, 0f);
            labelRt.anchoredPosition = new Vector2(0f, 4f);
            labelRt.sizeDelta        = new Vector2(-8f, 18f);
            var tmp = label.AddComponent<TextMeshProUGUI>();
            tmp.text          = "Part";
            tmp.fontSize      = 9f;
            tmp.fontStyle     = FontStyles.Bold;
            tmp.color         = COLOR_TEXT_MUTED;
            tmp.alignment     = TextAlignmentOptions.Center;
            tmp.overflowMode  = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;

            SavePrefab(root, "PartButtonPrefab");
        }

        #endregion

        #region Helpers

        private static void EnsureOutputFolder()
        {
            var parts   = OUTPUT_PATH.Split('/');
            var current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static GameObject CreateUIChild(string name, GameObject parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void SetSize(GameObject go, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = size;
        }

        private static void SavePrefab(GameObject go, string prefabName)
        {
            var path = $"{OUTPUT_PATH}/{prefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Debug.Log($"[CharacterUIPrefabGenerator] Prefab sauvegardé : {path}");
        }

        #endregion
    }
}
