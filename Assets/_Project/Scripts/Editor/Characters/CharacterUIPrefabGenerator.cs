using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using GlimmerOfHope.UI.Widgets;
using static GlimmerOfHope.Editor.Characters.CharacterUIConstants;

namespace GlimmerOfHope.Editor.Characters
{
    public static class CharacterUIPrefabGenerator
    {
        #region Constants

        private const string OUTPUT_PATH = "Assets/_Project/Prefabs/UI/Characters";

        private const float TAB_WIDTH      = 88f;
        private const float TAB_HEIGHT     = 96f;
        private const float PART_BTN_SIZE  = 88f;
        private const float ICON_SIZE      = 48f;
        private const float THUMB_PAD      = 8f;
        private const float LABEL_H        = 20f;
        private const float LABEL_FONT_SZ  = 12f;
        private const float TAB_FONT_SZ    = 13f;

        #endregion

        #region Menu

        [MenuItem("Tools/GlimmerOfHope/2 — Generate Character UI Prefabs")]
        public static void Generate()
        {
            if (!ValidateSprites()) return;
            EnsureFolder(OUTPUT_PATH);

            var canvas = CreateTempCanvas();
            try
            {
                BuildCategoryTabPrefab(canvas);
                BuildPartButtonPrefab(canvas);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Prefabs UI générés",
                    $"Prefabs dans : {OUTPUT_PATH}\n\n" +
                    "• CategoryTabPrefab (shadow + ActiveBar)\n" +
                    "• PartButtonPrefab  (shadow + SelectionIndicator + fond coloré)\n\n" +
                    "Prochaine étape : step 3", "OK");
            }
            finally { Object.DestroyImmediate(canvas); }
        }

        #endregion

        #region Category Tab Prefab

        private static void BuildCategoryTabPrefab(GameObject parent)
        {
            var root = UIChild("CategoryTabPrefab", parent);
            SetSize(root, new Vector2(TAB_WIDTH, TAB_HEIGHT));

            // Background card
            var rootImg = root.AddComponent<Image>();
            rootImg.sprite = Spr("rounded-card-lg");
            rootImg.type   = Image.Type.Sliced;
            rootImg.color  = CARD_BG;
            rootImg.raycastTarget = true;

            // Shadow behind card
            AddCardShadow(root, TAB_WIDTH, TAB_HEIGHT);

            // Button with visible hover/press
            var btn = root.AddComponent<Button>();
            var c   = btn.colors;
            c.normalColor      = CARD_BG;
            c.highlightedColor = CARD_HOVER;
            c.pressedColor     = CARD_PRESSED;
            c.selectedColor    = CARD_SELECTED_BG;
            c.fadeDuration     = 0.08f;
            btn.colors         = c;
            btn.targetGraphic  = rootImg;

            // Icon background
            var iconBg   = UIChild("IconBg", root);
            var ibRt = iconBg.GetComponent<RectTransform>();
            ibRt.anchorMin        = new Vector2(0.5f, 1f);
            ibRt.anchorMax        = new Vector2(0.5f, 1f);
            ibRt.pivot            = new Vector2(0.5f, 1f);
            ibRt.anchoredPosition = new Vector2(0f, -10f);
            ibRt.sizeDelta        = new Vector2(ICON_SIZE, ICON_SIZE);
            var ibImg = iconBg.AddComponent<Image>();
            ibImg.sprite = Spr("rounded-card");
            ibImg.type   = Image.Type.Sliced;
            ibImg.color  = PANEL_BG;
            ibImg.raycastTarget = false;

            // Icon image
            var icon = UIChild("Icon", iconBg);
            var iRt  = icon.GetComponent<RectTransform>();
            iRt.anchorMin = Vector2.zero;
            iRt.anchorMax = Vector2.one;
            iRt.offsetMin = new Vector2(8f, 8f);
            iRt.offsetMax = new Vector2(-8f, -8f);
            var iImg = icon.AddComponent<Image>();
            iImg.preserveAspect = true;
            iImg.color = TEXT_PRIMARY;
            iImg.raycastTarget = false;

            // Label
            var lbl = UIChild("Label", root);
            var lRt = lbl.GetComponent<RectTransform>();
            lRt.anchorMin        = new Vector2(0f, 0f);
            lRt.anchorMax        = new Vector2(1f, 0f);
            lRt.pivot            = new Vector2(0.5f, 0f);
            lRt.anchoredPosition = new Vector2(0f, 6f);
            lRt.sizeDelta        = new Vector2(0f, LABEL_H);
            var lTmp = lbl.AddComponent<TextMeshProUGUI>();
            lTmp.text      = "Cat.";
            lTmp.fontSize  = TAB_FONT_SZ;
            lTmp.fontStyle = FontStyles.Bold;
            lTmp.color     = TEXT_MUTED;
            lTmp.alignment = TextAlignmentOptions.Center;
            lTmp.overflowMode  = TextOverflowModes.Ellipsis;
            lTmp.raycastTarget = false;

            // Active bar (underline accent)
            var bar = UIChild("ActiveBar", root);
            var bRt = bar.GetComponent<RectTransform>();
            bRt.anchorMin        = new Vector2(0.1f, 0f);
            bRt.anchorMax        = new Vector2(0.9f, 0f);
            bRt.pivot            = new Vector2(0.5f, 0f);
            bRt.anchoredPosition = new Vector2(0f, 1f);
            bRt.sizeDelta        = new Vector2(0f, 4f);
            var bImg = bar.AddComponent<Image>();
            bImg.sprite = Spr("tab-underline");
            bImg.color  = ACCENT;
            bImg.raycastTarget = false;
            bar.SetActive(false);

            // Wire CategoryTabView
            var view = root.AddComponent<CategoryTabView>();
            var so = new SerializedObject(view);
            so.FindProperty("_label").objectReferenceValue  = lTmp;
            so.FindProperty("_icon").objectReferenceValue   = iImg;
            so.FindProperty("_button").objectReferenceValue = btn;
            so.FindProperty("_activeIndicator").objectReferenceValue = bar;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<CharacterUIAnimator>();

            SavePrefab(root, "CategoryTabPrefab");
        }

        #endregion

        #region Part Button Prefab

        private static void BuildPartButtonPrefab(GameObject parent)
        {
            var root = UIChild("PartButtonPrefab", parent);
            SetSize(root, new Vector2(PART_BTN_SIZE, PART_BTN_SIZE));

            // Background card — white on darker panel = visible contrast
            var rootImg = root.AddComponent<Image>();
            rootImg.sprite = Spr("rounded-card-lg");
            rootImg.type   = Image.Type.Sliced;
            rootImg.color  = CARD_BG;
            rootImg.raycastTarget = true;

            // Shadow behind card
            AddCardShadow(root, PART_BTN_SIZE, PART_BTN_SIZE);

            // Button
            var btn = root.AddComponent<Button>();
            var c   = btn.colors;
            c.normalColor      = CARD_BG;
            c.highlightedColor = CARD_HOVER;
            c.pressedColor     = CARD_PRESSED;
            c.selectedColor    = CARD_SELECTED_BG;
            c.fadeDuration     = 0.08f;
            btn.colors         = c;
            btn.targetGraphic  = rootImg;

            // Thumbnail
            var thumb = UIChild("Thumbnail", root);
            var tRt   = thumb.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0f, 1f);
            tRt.anchorMax = new Vector2(1f, 1f);
            tRt.pivot     = new Vector2(0.5f, 1f);
            tRt.offsetMin = new Vector2(THUMB_PAD, -(PART_BTN_SIZE - LABEL_H - 4f));
            tRt.offsetMax = new Vector2(-THUMB_PAD, -THUMB_PAD);
            var tImg = thumb.AddComponent<Image>();
            tImg.preserveAspect = true;
            tImg.color = new Color(0.85f, 0.84f, 0.82f);
            tImg.raycastTarget = false;

            // Label
            var lbl = UIChild("Label", root);
            var lRt = lbl.GetComponent<RectTransform>();
            lRt.anchorMin        = new Vector2(0f, 0f);
            lRt.anchorMax        = new Vector2(1f, 0f);
            lRt.pivot            = new Vector2(0.5f, 0f);
            lRt.anchoredPosition = new Vector2(0f, 3f);
            lRt.sizeDelta        = new Vector2(-6f, LABEL_H);
            var lTmp = lbl.AddComponent<TextMeshProUGUI>();
            lTmp.text      = "Part";
            lTmp.fontSize  = LABEL_FONT_SZ;
            lTmp.fontStyle = FontStyles.Bold;
            lTmp.color     = TEXT_MUTED;
            lTmp.alignment = TextAlignmentOptions.Center;
            lTmp.overflowMode  = TextOverflowModes.Ellipsis;
            lTmp.raycastTarget = false;

            // Selection indicator — thick border + colored background
            var sel = UIChild("SelectionIndicator", root);
            var sRt = sel.GetComponent<RectTransform>();
            sRt.anchorMin = Vector2.zero;
            sRt.anchorMax = Vector2.one;
            sRt.offsetMin = new Vector2(-3f, -3f);
            sRt.offsetMax = new Vector2(3f, 3f);
            var sImg = sel.AddComponent<Image>();
            sImg.sprite = Spr("selection-border");
            sImg.type   = Image.Type.Sliced;
            sImg.color  = ACCENT;
            sImg.raycastTarget = false;
            sel.SetActive(false);

            // Wire PartButtonView
            var view = root.AddComponent<PartButtonView>();
            var so = new SerializedObject(view);
            so.FindProperty("_label").objectReferenceValue     = lTmp;
            so.FindProperty("_thumbnail").objectReferenceValue = tImg;
            so.FindProperty("_button").objectReferenceValue    = btn;
            so.FindProperty("_selectionIndicator").objectReferenceValue = sel;
            so.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<CharacterUIAnimator>();

            SavePrefab(root, "PartButtonPrefab");
        }

        #endregion

        #region Card Shadow Helper

        private static void AddCardShadow(GameObject card, float w, float h)
        {
            var shadow = UIChild("Shadow", card);
            shadow.transform.SetAsFirstSibling();
            var sRt = shadow.GetComponent<RectTransform>();
            sRt.anchorMin = Vector2.zero;
            sRt.anchorMax = Vector2.one;
            sRt.offsetMin = new Vector2(-3f, -4f);
            sRt.offsetMax = new Vector2(3f, 1f);
            var img = shadow.AddComponent<Image>();
            img.sprite = Spr("shadow-soft");
            img.type   = Image.Type.Sliced;
            img.color  = CARD_SHADOW;
            img.raycastTarget = false;
        }

        #endregion

        #region Helpers

        private static bool ValidateSprites()
        {
            string[] needed = { "rounded-card-lg", "rounded-card", "selection-border", "tab-underline", "shadow-soft" };
            foreach (var s in needed)
            {
                if (Spr(s) == null)
                {
                    EditorUtility.DisplayDialog("Sprites manquants",
                        $"'{s}' introuvable dans :\n{SPRITES_PATH}\n\nLance d'abord le step 1.", "OK");
                    return false;
                }
            }
            return true;
        }

        private static Sprite Spr(string name) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/{name}.png");

        private static GameObject UIChild(string name, GameObject parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void SetSize(GameObject go, Vector2 size)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
        }

        private static void SavePrefab(GameObject go, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(go, $"{OUTPUT_PATH}/{name}.prefab");
            Debug.Log($"[UIPrefabGenerator] {name} sauvegardé.");
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        private static GameObject CreateTempCanvas()
        {
            var go = new GameObject("__TempCanvas__");
            go.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            go.hideFlags = HideFlags.HideAndDontSave;
            return go;
        }

        #endregion
    }
}
