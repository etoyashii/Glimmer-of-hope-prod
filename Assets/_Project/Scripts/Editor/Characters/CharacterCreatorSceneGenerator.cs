using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace GlimmerOfHope.Editor.Characters
{
    public static class CharacterCreatorSceneGenerator
    {
        #region Constants

        private static readonly Color COLOR_TOPBAR         = new Color(0.29f,  0.565f, 0.851f);
        private static readonly Color COLOR_PANEL          = Color.white;
        private static readonly Color COLOR_CONFIRM        = new Color(0.357f, 0.749f, 0.447f);
        private static readonly Color COLOR_BTN_OK         = new Color(0.29f,  0.565f, 0.851f);
        private static readonly Color COLOR_BTN_CANCEL_BG  = new Color(0.94f,  0.95f,  0.98f);
        private static readonly Color COLOR_TEXT_PRIMARY   = new Color(0.173f, 0.133f, 0.208f);
        private static readonly Color COLOR_TEXT_MUTED     = new Color(0.478f, 0.522f, 0.627f);
        private static readonly Color COLOR_DIVIDER        = new Color(0.91f,  0.925f, 0.961f);
        private static readonly Color COLOR_CAMERA_BG      = new Color(0.659f, 0.769f, 0.933f);
        private static readonly Color COLOR_RESET_BG       = Color.white;

        private const float TOPBAR_HEIGHT    = 60f;
        private const float BOTTOMBAR_HEIGHT = 56f;
        private const float PANEL_SPLIT      = 0.575f;

        #endregion

        #region Menu

        [MenuItem("Tools/GlimmerOfHope/Generate Character Creator Scene")]
        public static void Generate()
        {
            if (!EditorUtility.DisplayDialog(
                    "Générer la scène Character Creator",
                    "Cette action va créer toute la hiérarchie UI dans la scène active.\n\nContinuer ?",
                    "Générer", "Annuler"))
                return;

            var root = BuildSceneRoot();
            Selection.activeGameObject = root;

            EditorUtility.DisplayDialog(
                "Génération terminée ✔",
                "La hiérarchie a été générée.\n\n" +
                "Reste à faire manuellement :\n" +
                "• RightPanel → Add Component → CharacterCreatorUI\n" +
                "  → assigner _onPartChanged, _categoryTabPrefab, _partButtonPrefab\n\n" +
                "• CharacterPreview → Add Component → CharacterPreviewRenderer\n" +
                "  → assigner _onPartChanged, _anchors3D (x4), _spriteRenderers (x2)",
                "OK");
        }

        #endregion

        #region Root

        private static GameObject BuildSceneRoot()
        {
            var existing = GameObject.Find("CharacterCreator");
            if (existing != null)
                Undo.DestroyObjectImmediate(existing);

            var root = new GameObject("CharacterCreator");
            Undo.RegisterCreatedObjectUndo(root, "Generate Character Creator Scene");

            BuildCamera(root);
            BuildCharacterPreview(root);
            BuildCanvas(root);

            return root;
        }

        #endregion

        #region Camera

        private static void BuildCamera(GameObject parent)
        {
            var go = CreateChild("PreviewCamera", parent);
            go.transform.position = new Vector3(0f, 1.2f, -3.5f);

            var cam = go.AddComponent<Camera>();
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = COLOR_CAMERA_BG;
            cam.fieldOfView     = 40f;
            cam.nearClipPlane   = 0.1f;
            cam.farClipPlane    = 100f;
            cam.rect = new Rect(0f, 0.052f, PANEL_SPLIT, 0.892f);

            // Lumière directionnelle
            var lightGo = CreateChild("Directional Light", parent);
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type      = LightType.Directional;
            light.intensity = 1.2f;
            light.color     = new Color(1f, 0.97f, 0.92f);
        }

        #endregion

        #region CharacterPreview

        private static void BuildCharacterPreview(GameObject parent)
        {
            var preview = CreateChild("CharacterPreview", parent);
            preview.transform.position = Vector3.zero;

            CreateChild("Anchor_Tete",       preview).transform.localPosition = new Vector3(0f,  1.6f, 0f);
            CreateChild("Anchor_Cheveux",     preview).transform.localPosition = new Vector3(0f,  1.7f, 0f);
            CreateChild("Anchor_Vetements",   preview).transform.localPosition = new Vector3(0f,  1.0f, 0f);
            CreateChild("Anchor_Accessoires", preview).transform.localPosition = new Vector3(0f,  1.8f, 0f);

            BuildSpriteAnchor("Sprite_Yeux",   preview, new Vector3(-0.12f, 1.52f, -0.05f));
            BuildSpriteAnchor("Sprite_Bouche", preview, new Vector3(0f,     1.38f, -0.05f));

            Debug.Log("[SceneGenerator] CharacterPreview créé. " +
                      "Attache CharacterPreviewRenderer manuellement sur ce GO.");
        }

        private static void BuildSpriteAnchor(string name, GameObject parent, Vector3 localPos)
        {
            var go = CreateChild(name, parent);
            go.transform.localPosition = localPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 1;
        }

        #endregion

        #region Canvas

        private static void BuildCanvas(GameObject parent)
        {
            var canvasGo = CreateUIElement("Canvas", parent);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight  = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            BuildBottomBar(canvasGo);
            BuildPreviewZone(canvasGo);
            BuildRightPanel(canvasGo);
            BuildTopBar(canvasGo);
        }

        #endregion

        #region TopBar

        private static void BuildTopBar(GameObject canvas)
        {
            var go = CreateUIElement("TopBar", canvas);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(1f, 1f);
            rt.pivot            = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(0f, TOPBAR_HEIGHT);
            AddImage(go, COLOR_TOPBAR);

            // Title
            var title = CreateUIElement("Title", go);
            var trt   = title.GetComponent<RectTransform>();
            trt.anchorMin        = new Vector2(0f, 0.5f);
            trt.anchorMax        = new Vector2(0f, 0.5f);
            trt.pivot            = new Vector2(0f, 0.5f);
            trt.anchoredPosition = new Vector2(28f, 0f);
            trt.sizeDelta        = new Vector2(600f, 44f);
            var t1 = title.AddComponent<TextMeshProUGUI>();
            t1.text      = "✦ Créer mon personnage";
            t1.fontSize  = 22f;
            t1.fontStyle = FontStyles.Bold;
            t1.color     = Color.white;
            t1.alignment = TextAlignmentOptions.Left;

            // StepLabel
            var step = CreateUIElement("StepLabel", go);
            var srt  = step.GetComponent<RectTransform>();
            srt.anchorMin        = new Vector2(1f, 0.5f);
            srt.anchorMax        = new Vector2(1f, 0.5f);
            srt.pivot            = new Vector2(1f, 0.5f);
            srt.anchoredPosition = new Vector2(-28f, 0f);
            srt.sizeDelta        = new Vector2(320f, 36f);
            var t2 = step.AddComponent<TextMeshProUGUI>();
            t2.text      = "Prototype — Glimmer of Hope";
            t2.fontSize  = 13f;
            t2.color     = new Color(1f, 1f, 1f, 0.75f);
            t2.alignment = TextAlignmentOptions.Right;
        }

        #endregion

        #region BottomBar

        private static void BuildBottomBar(GameObject canvas)
        {
            var go = CreateUIElement("BottomBar", canvas);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(1f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(0f, BOTTOMBAR_HEIGHT);
            AddImage(go, COLOR_PANEL);

            // StatusText
            var status = CreateUIElement("StatusText", go);
            var srt    = status.GetComponent<RectTransform>();
            srt.anchorMin        = new Vector2(0f, 0.5f);
            srt.anchorMax        = new Vector2(0f, 0.5f);
            srt.pivot            = new Vector2(0f, 0.5f);
            srt.anchoredPosition = new Vector2(24f, 0f);
            srt.sizeDelta        = new Vector2(500f, 36f);
            var ts = status.AddComponent<TextMeshProUGUI>();
            ts.text      = "Personnalisez votre personnage";
            ts.fontSize  = 13f;
            ts.color     = COLOR_TEXT_MUTED;
            ts.alignment = TextAlignmentOptions.Left;

            BuildSimpleButton("BtnCancel", go,
                new Vector2(-126f, 0f), new Vector2(90f, 38f),
                "Annuler", COLOR_BTN_CANCEL_BG, COLOR_TEXT_MUTED,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));

            BuildSimpleButton("BtnOK", go,
                new Vector2(-24f, 0f), new Vector2(90f, 38f),
                "OK", COLOR_BTN_OK, Color.white,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f));
        }

        #endregion

        #region PreviewZone

        private static void BuildPreviewZone(GameObject canvas)
        {
            var go = CreateUIElement("PreviewZone", canvas);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(PANEL_SPLIT, 1f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(16f, BOTTOMBAR_HEIGHT + 8f);
            rt.offsetMax = new Vector2(-8f, -(TOPBAR_HEIGHT + 8f));

            var img = go.AddComponent<Image>();
            img.color         = new Color(COLOR_CAMERA_BG.r, COLOR_CAMERA_BG.g, COLOR_CAMERA_BG.b, 0.25f);
            img.raycastTarget = false;

            // ActionRow (Reset + Confirm)
            var row = CreateUIElement("ActionRow", go);
            var rrt = row.GetComponent<RectTransform>();
            rrt.anchorMin        = new Vector2(0f, 0f);
            rrt.anchorMax        = new Vector2(1f, 0f);
            rrt.pivot            = new Vector2(0.5f, 0f);
            rrt.anchoredPosition = new Vector2(0f, 12f);
            rrt.sizeDelta        = new Vector2(0f, 48f);

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing                = 10f;
            hlg.childForceExpandWidth  = true;
            hlg.childForceExpandHeight = true;
            hlg.childAlignment         = TextAnchor.MiddleCenter;

            // Boutons dans le layout group
            BuildSimpleButton("BtnReset", row,
                Vector2.zero, Vector2.zero,
                "↺  Réinitialiser", COLOR_RESET_BG, COLOR_TEXT_PRIMARY,
                Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));

            BuildSimpleButton("BtnConfirm", row,
                Vector2.zero, Vector2.zero,
                "✔  Confirmer", COLOR_CONFIRM, Color.white,
                Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f));
        }

        #endregion

        #region RightPanel

        private static void BuildRightPanel(GameObject canvas)
        {
            var go = CreateUIElement("RightPanel", canvas);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(PANEL_SPLIT, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(8f,  BOTTOMBAR_HEIGHT + 8f);
            rt.offsetMax = new Vector2(-16f, -(TOPBAR_HEIGHT + 8f));
            AddImage(go, COLOR_PANEL);

            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.padding                = new RectOffset(0, 0, 0, 14);
            vlg.spacing                = 0f;
            vlg.childAlignment         = TextAnchor.UpperCenter;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = false;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;

            BuildCategoryBar(go);
            BuildDivider(go);
            BuildPartsLabel(go);
            BuildPartsGrid(go);

            Debug.Log("[SceneGenerator] RightPanel créé. " +
                      "Attache CharacterCreatorUI manuellement sur RightPanel.");
        }

        private static void BuildCategoryBar(GameObject parent)
        {
            var go = CreateUIElement("CategoryBar", parent);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 88f);
            AddLayoutElement(go, preferredHeight: 88f, flexibleHeight: 0f);

            var bgImg = go.AddComponent<Image>();
            bgImg.color         = Color.clear;
            bgImg.raycastTarget = false;

            var mask = go.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var sr = go.AddComponent<ScrollRect>();
            sr.horizontal        = true;
            sr.vertical          = false;
            sr.movementType      = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 30f;

            // CategoryContent
            var content = CreateUIElement("CategoryContent", go);
            var crt     = content.GetComponent<RectTransform>();
            crt.anchorMin        = new Vector2(0f, 0f);
            crt.anchorMax        = new Vector2(0f, 1f);
            crt.pivot            = new Vector2(0f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta        = Vector2.zero;

            var hlg = content.AddComponent<HorizontalLayoutGroup>();
            hlg.padding                = new RectOffset(12, 12, 8, 0);
            hlg.spacing                = 4f;
            hlg.childAlignment         = TextAnchor.UpperLeft;
            hlg.childControlWidth      = false;
            hlg.childControlHeight     = false;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;

            var csf = content.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit   = ContentSizeFitter.FitMode.Unconstrained;

            sr.content  = crt;
            sr.viewport = go.GetComponent<RectTransform>();
        }

        private static void BuildDivider(GameObject parent)
        {
            var go = CreateUIElement("Divider", parent);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 2f);
            AddLayoutElement(go, preferredHeight: 2f, flexibleHeight: 0f);
            AddImage(go, COLOR_DIVIDER);
        }

        private static void BuildPartsLabel(GameObject parent)
        {
            var go = CreateUIElement("PartsLabel", parent);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 38f);
            AddLayoutElement(go, preferredHeight: 38f, flexibleHeight: 0f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text             = "TÊTE";
            tmp.fontSize         = 11f;
            tmp.fontStyle        = FontStyles.Bold;
            tmp.color            = COLOR_TEXT_MUTED;
            tmp.alignment        = TextAlignmentOptions.Left;
            tmp.margin           = new Vector4(18f, 0f, 0f, 0f);
            tmp.characterSpacing = 3f;
        }

        private static void BuildPartsGrid(GameObject parent)
        {
            var scroll = CreateUIElement("PartsGridScroll", parent);
            scroll.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            AddLayoutElement(scroll, preferredHeight: -1f, flexibleHeight: 1f);

            var scrollImg = scroll.AddComponent<Image>();
            scrollImg.color         = Color.clear;
            scrollImg.raycastTarget = false;

            var scrollMask = scroll.AddComponent<Mask>();
            scrollMask.showMaskGraphic = false;

            var sr = scroll.AddComponent<ScrollRect>();
            sr.horizontal        = false;
            sr.vertical          = true;
            sr.movementType      = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 30f;
            sr.viewport          = scroll.GetComponent<RectTransform>();

            // PartsContent
            var content = CreateUIElement("PartsContent", scroll);
            var crt     = content.GetComponent<RectTransform>();
            crt.anchorMin        = new Vector2(0f, 1f);
            crt.anchorMax        = new Vector2(1f, 1f);
            crt.pivot            = new Vector2(0.5f, 1f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta        = Vector2.zero;

            var grid = content.AddComponent<GridLayoutGroup>();
            grid.padding         = new RectOffset(14, 14, 6, 14);
            grid.cellSize        = new Vector2(78f, 78f);
            grid.spacing         = new Vector2(10f, 10f);
            grid.startCorner     = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis       = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment  = TextAnchor.UpperLeft;
            grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            var csf = content.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            sr.content = crt;
        }

        #endregion

        #region Helpers

        private static GameObject CreateChild(string name, GameObject parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static GameObject CreateUIElement(string name, GameObject parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static Image AddImage(GameObject go, Color color)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static void AddLayoutElement(GameObject go, float preferredHeight, float flexibleHeight)
        {
            var le = go.AddComponent<LayoutElement>();
            if (preferredHeight >= 0f) le.preferredHeight = preferredHeight;
            le.flexibleHeight = flexibleHeight;
        }

        private static void BuildSimpleButton(
            string name, GameObject parent,
            Vector2 anchoredPos, Vector2 sizeDelta,
            string label, Color bgColor, Color textColor,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var go = CreateUIElement(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = sizeDelta;

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn    = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(
                Mathf.Clamp01(bgColor.r - 0.06f),
                Mathf.Clamp01(bgColor.g - 0.06f),
                Mathf.Clamp01(bgColor.b - 0.06f));
            colors.pressedColor = new Color(
                Mathf.Clamp01(bgColor.r - 0.14f),
                Mathf.Clamp01(bgColor.g - 0.14f),
                Mathf.Clamp01(bgColor.b - 0.14f));
            btn.colors        = colors;
            btn.targetGraphic = img;

            var textGo = CreateUIElement("Text", go);
            var trt    = textGo.GetComponent<RectTransform>();
            trt.anchorMin  = Vector2.zero;
            trt.anchorMax  = Vector2.one;
            trt.offsetMin  = Vector2.zero;
            trt.offsetMax  = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 14f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color     = textColor;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        #endregion
    }
}
