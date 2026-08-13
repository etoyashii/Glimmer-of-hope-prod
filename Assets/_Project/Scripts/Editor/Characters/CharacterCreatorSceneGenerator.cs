using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Gameplay.Characters;
using GlimmerOfHope.UI.Character;
using GlimmerOfHope.UI.Widgets;
using static GlimmerOfHope.Editor.Characters.CharacterUIConstants;
using RectMask = UnityEngine.UI.RectMask2D;

namespace GlimmerOfHope.Editor.Characters
{
    public static class CharacterCreatorSceneGenerator
    {
        #region Menu

        private const string DATA_PATH   = "Assets/_Project/Data";
        private const string PREFAB_PATH = "Assets/_Project/Prefabs/UI/Characters";

        private static CharacterRegistrySO _registry;

        public static void Generate()
        {
            if (!ValidateAssets()) return;

            _registry = AssetDatabase.LoadAssetAtPath<CharacterRegistrySO>($"{DATA_PATH}/Characters/_Registry.asset");
            if (_registry == null)
            {
                EditorUtility.DisplayDialog("Assets manquants",
                    "Registry introuvable :\n" + DATA_PATH + "/Characters/_Registry.asset", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Generer la scene Character Creator",
                    "Cree toute la hierarchie UI dans la scene active.\nContinuer ?",
                    "Generer", "Annuler"))
            {
                _registry = null;
                return;
            }

            var root = BuildRoot();
            WireAllComponents(root);
            Selection.activeGameObject = root;

            _registry = null;

            EditorUtility.DisplayDialog(
                "Scene generee",
                "Hierarchie creee et composants branches automatiquement.\n\nTout est pret.",
                "OK");
        }

        #endregion

        #region Root

        private static GameObject BuildRoot()
        {
            var existing = GameObject.Find("CharacterCreator");
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            var root = new GameObject("CharacterCreator");
            Undo.RegisterCreatedObjectUndo(root, "Generate Character Creator Scene");

            BuildCamera(root);
            BuildCharacterPreview(root);
            BuildCanvas(root);
            BuildEventSystem(root);

            return root;
        }

        #endregion

        #region Camera & Preview

        private static void BuildCamera(GameObject parent)
        {
            var go = Child("PreviewCamera", parent);
            go.transform.position = new Vector3(0f, 1.2f, -3.5f);

            var cam = go.AddComponent<Camera>();
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = CAMERA_BG;
            cam.fieldOfView     = 40f;
            cam.nearClipPlane   = 0.1f;
            cam.farClipPlane    = 100f;
            cam.rect = new Rect(0f, 0.052f, PANEL_SPLIT, 0.892f);

            var light = Child("Directional Light", parent);
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var l = light.AddComponent<Light>();
            l.type      = LightType.Directional;
            l.intensity = 1.2f;
            l.color     = new Color(1f, 0.97f, 0.92f);
        }

        private static void BuildCharacterPreview(GameObject parent)
        {
            var preview = Child("CharacterPreview", parent);
            if (_registry == null) return;

            foreach (var category in _registry.Categories)
            {
                if (category == null) continue;
                var pascal = ToPascalCase(category.CategoryID);

                if (category.DefaultPartType == CharacterPartType.Prefab3D)
                    Child($"Anchor_{pascal}", preview);
                else
                    BuildSpriteAnchor($"Sprite_{pascal}", preview, Vector3.zero);
            }
        }

        private static void BuildSpriteAnchor(string name, GameObject parent, Vector3 pos)
        {
            var go = Child(name, parent);
            go.transform.localPosition = pos;
            go.AddComponent<SpriteRenderer>().sortingOrder = 1;
        }

        #endregion

        #region EventSystem

        private static void BuildEventSystem(GameObject parent)
        {
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
                return;

            var go = Child("EventSystem", parent);
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        #endregion

        #region Canvas

        private static void BuildCanvas(GameObject parent)
        {
            var go = UI("Canvas", parent);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight  = 0.5f;

            go.AddComponent<GraphicRaycaster>();

            BuildTopBar(go);
            BuildPreviewZone(go);
            BuildRightPanel(go);
            BuildBottomBar(go);
        }

        #endregion

        #region TopBar

        private static void BuildTopBar(GameObject canvas)
        {
            var go = UI("TopBar", canvas);
            Stretch(go, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, TOPBAR_HEIGHT);
            AddSlicedImage(go, "rounded-panel", TOPBAR);

            var shadow = UI("Shadow", go);
            var srt = shadow.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 0f);
            srt.pivot     = new Vector2(0.5f, 1f);
            srt.sizeDelta = new Vector2(0f, 8f);
            var sImg = shadow.AddComponent<Image>();
            sImg.color = SHADOW_COLOR;
            sImg.raycastTarget = false;

            AddText("Title", go,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(28f, 0f), new Vector2(600f, 44f),
                "Creer mon personnage", FONT_TITLE, FontStyles.Bold,
                TOPBAR_TEXT, TextAlignmentOptions.Left);

            AddText("Subtitle", go,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-28f, 0f), new Vector2(320f, 36f),
                "Glimmer of Hope", FONT_SUBTITLE, FontStyles.Normal,
                TOPBAR_SUB, TextAlignmentOptions.Right);
        }

        #endregion

        #region Preview Zone

        private static void BuildPreviewZone(GameObject canvas)
        {
            var go = UI("PreviewZone", canvas);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(PANEL_SPLIT, 1f);
            rt.offsetMin = new Vector2(PANEL_GAP, BOTTOMBAR_HEIGHT + PANEL_GAP);
            rt.offsetMax = new Vector2(-PANEL_GAP / 2f, -(TOPBAR_HEIGHT + PANEL_GAP));
            AddSlicedImage(go, "rounded-panel", PREVIEW_OVERLAY);

            var row = UI("ActionRow", go);
            var rrt = row.GetComponent<RectTransform>();
            rrt.anchorMin = new Vector2(0.1f, 0f);
            rrt.anchorMax = new Vector2(0.9f, 0f);
            rrt.pivot     = new Vector2(0.5f, 0f);
            rrt.anchoredPosition = new Vector2(0f, 16f);
            rrt.sizeDelta = new Vector2(0f, 48f);

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing                = 12f;
            hlg.childForceExpandWidth  = true;
            hlg.childForceExpandHeight = true;
            hlg.childAlignment         = TextAnchor.MiddleCenter;

            BuildActionButton("BtnReset",   row, "Reinitialiser", BTN_NEUTRAL_BG, TEXT_PRIMARY);
            BuildActionButton("BtnConfirm", row, "Confirmer",     CONFIRM,        TEXT_ON_ACCENT);
        }

        private static void BuildActionButton(string name, GameObject parent,
            string label, Color bg, Color textCol)
        {
            var go = UI(name, parent);
            AddSlicedImage(go, "rounded-button", bg);

            var btn = go.AddComponent<Button>();
            var c   = btn.colors;
            c.highlightedColor = new Color(bg.r - 0.04f, bg.g - 0.04f, bg.b - 0.04f);
            c.pressedColor     = new Color(bg.r - 0.10f, bg.g - 0.10f, bg.b - 0.10f);
            c.fadeDuration     = 0.08f;
            btn.colors         = c;
            btn.targetGraphic  = go.GetComponent<Image>();

            AddText("Text", go,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                label, FONT_BUTTON, FontStyles.Bold,
                textCol, TextAlignmentOptions.Center, true);
        }

        #endregion

        #region Right Panel

        private static void BuildRightPanel(GameObject canvas)
        {
            var go = UI("RightPanel", canvas);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(PANEL_SPLIT, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(PANEL_GAP / 2f, BOTTOMBAR_HEIGHT + PANEL_GAP);
            rt.offsetMax = new Vector2(-PANEL_GAP, -(TOPBAR_HEIGHT + PANEL_GAP));
            AddSlicedImage(go, "rounded-panel", PANEL_BG);

            AddShadowBehind(go);

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
        }

        private static void BuildCategoryBar(GameObject parent)
        {
            var go = UI("CategoryBar", parent);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 104f);
            AddLayoutElement(go, 104f, 0f);

            go.AddComponent<RectMask>();

            var sr = go.AddComponent<ScrollRect>();
            sr.horizontal        = true;
            sr.vertical          = false;
            sr.movementType      = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 30f;

            var content = UI("CategoryContent", go);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 0f);
            crt.anchorMax = new Vector2(0f, 1f);
            crt.pivot     = new Vector2(0f, 0.5f);
            crt.sizeDelta = Vector2.zero;

            var hlg = content.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(14, 14, 8, 0);
            hlg.spacing = 6f;
            hlg.childAlignment         = TextAnchor.UpperLeft;
            hlg.childControlWidth      = false;
            hlg.childControlHeight     = false;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;

            var csf = content.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.content  = crt;
            sr.viewport = go.GetComponent<RectTransform>();
        }

        private static void BuildDivider(GameObject parent)
        {
            var go = UI("Divider", parent);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 1f);
            AddLayoutElement(go, 1f, 0f);
            go.AddComponent<Image>().color = DIVIDER;
        }

        private static void BuildPartsLabel(GameObject parent)
        {
            var go = UI("PartsLabel", parent);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 40f);
            AddLayoutElement(go, 40f, 0f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text             = "CATEGORIE";
            tmp.fontSize         = FONT_SECTION;
            tmp.fontStyle        = FontStyles.Bold;
            tmp.color            = TEXT_MUTED;
            tmp.alignment        = TextAlignmentOptions.Left;
            tmp.margin           = new Vector4(GRID_PADDING + 2f, 0f, 0f, 0f);
            tmp.characterSpacing = 2.5f;
        }

        private static void BuildPartsGrid(GameObject parent)
        {
            var scroll = UI("PartsGridScroll", parent);
            AddLayoutElement(scroll, -1f, 1f);

            scroll.AddComponent<RectMask>();

            var sr = scroll.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical   = true;
            sr.movementType      = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 30f;
            sr.viewport = scroll.GetComponent<RectTransform>();

            var content = UI("PartsContent", scroll);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot     = new Vector2(0.5f, 1f);
            crt.sizeDelta = Vector2.zero;

            var grid = content.AddComponent<GridLayoutGroup>();
            int p = (int)GRID_PADDING;
            grid.padding         = new RectOffset(p, p, 8, p);
            grid.cellSize        = new Vector2(GRID_CELL_SIZE, GRID_CELL_SIZE);
            grid.spacing         = new Vector2(GRID_SPACING, GRID_SPACING);
            grid.startCorner     = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis       = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment  = TextAnchor.UpperLeft;
            grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = GRID_COLUMNS;

            var csf = content.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            sr.content = crt;
        }

        #endregion

        #region Bottom Bar

        private static void BuildBottomBar(GameObject canvas)
        {
            var go = UI("BottomBar", canvas);
            Stretch(go, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f));
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, BOTTOMBAR_HEIGHT);
            AddSlicedImage(go, "rounded-panel", PANEL_BG);

            AddText("StatusText", go,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(24f, 0f), new Vector2(500f, 36f),
                "Personnalisez votre personnage", FONT_STATUS, FontStyles.Normal,
                TEXT_MUTED, TextAlignmentOptions.Left);
        }

        #endregion

        #region Auto-Wire Components

        private static void WireAllComponents(GameObject root)
        {
            var evtCat     = AssetDatabase.LoadAssetAtPath<StringEventChannel>($"{DATA_PATH}/Events/Characters/OnCategorySelected.asset");
            var evtPart    = AssetDatabase.LoadAssetAtPath<StringEventChannel>($"{DATA_PATH}/Events/Characters/OnCharacterPartChanged.asset");
            var evtConfirm = AssetDatabase.LoadAssetAtPath<VoidEventChannel>($"{DATA_PATH}/Events/Characters/OnCharacterConfirmed.asset");
            var tabPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_PATH}/CategoryTabPrefab.prefab");
            var partPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_PATH}/PartButtonPrefab.prefab");

            if (evtCat == null || evtPart == null)
            {
                Debug.LogError("[SceneGenerator] EventChannels manquants (OnCategorySelected, OnCharacterPartChanged).");
                return;
            }

            WireBootstrapper(root, evtPart);
            WirePreviewRenderer(root, evtPart);
            WireCategoryTabs(root, tabPrefab, evtCat);
            WirePartsGrid(root, partPrefab, evtCat, evtPart);
            WireCategoryLabel(root, evtCat);
            WireResetButton(root, evtCat);
            WireConfirmButton(root, evtConfirm);

            Debug.Log("[SceneGenerator] Tous les composants branches automatiquement.");
        }

        private static void WireBootstrapper(GameObject root, StringEventChannel evtPart)
        {
            var boot = root.AddComponent<CharacterSystemBootstrapper>();
            var so = new SerializedObject(boot);
            so.FindProperty("_characterRegistry").objectReferenceValue      = _registry;
            so.FindProperty("_onCharacterPartChanged").objectReferenceValue = evtPart;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WirePreviewRenderer(GameObject root, StringEventChannel evtPart)
        {
            var preview = root.transform.Find("CharacterPreview");
            if (preview == null || _registry == null) return;

            var renderer = preview.gameObject.AddComponent<CharacterPreviewRenderer>();
            var so = new SerializedObject(renderer);
            so.FindProperty("_onPartChanged").objectReferenceValue = evtPart;

            // Priorite : champ defini sur le Registry, sinon detection automatique
            var masterPrefab = _registry.MasterCharacterPrefab ?? CharacterPartsImporter.FindMasterCharacterPrefab();
            if (masterPrefab != null)
                so.FindProperty("_masterCharacterPrefab").objectReferenceValue = masterPrefab;

            var cats3D = new List<CharacterCategorySO>();
            var cats2D = new List<CharacterCategorySO>();
            foreach (var cat in _registry.Categories)
            {
                if (cat == null) continue;
                if (cat.DefaultPartType == CharacterPartType.Prefab3D) cats3D.Add(cat);
                else cats2D.Add(cat);
            }

            var anchors = so.FindProperty("_anchors3D");
            anchors.arraySize = cats3D.Count;
            for (int i = 0; i < cats3D.Count; i++)
            {
                var el     = anchors.GetArrayElementAtIndex(i);
                var pascal = ToPascalCase(cats3D[i].CategoryID);
                el.FindPropertyRelative("categoryId").stringValue      = cats3D[i].CategoryID;
                el.FindPropertyRelative("anchor").objectReferenceValue = preview.Find($"Anchor_{pascal}");
            }

            var sprites = so.FindProperty("_spriteRenderers");
            sprites.arraySize = cats2D.Count;
            for (int i = 0; i < cats2D.Count; i++)
            {
                var el     = sprites.GetArrayElementAtIndex(i);
                var pascal = ToPascalCase(cats2D[i].CategoryID);
                el.FindPropertyRelative("categoryId").stringValue              = cats2D[i].CategoryID;
                el.FindPropertyRelative("spriteRenderer").objectReferenceValue = preview.Find($"Sprite_{pascal}")?.GetComponent<SpriteRenderer>();
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireCategoryTabs(GameObject root, GameObject prefab, StringEventChannel evtCat)
        {
            var target = FindDeep(root.transform, "CategoryContent");
            if (target == null) return;

            var ctrl = target.gameObject.AddComponent<CharacterCategoryTabsController>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("_categoryTabPrefab").objectReferenceValue   = prefab;
            so.FindProperty("_onCategorySelected").objectReferenceValue  = evtCat;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WirePartsGrid(GameObject root, GameObject prefab, StringEventChannel evtCat, StringEventChannel evtPart)
        {
            var target = FindDeep(root.transform, "PartsContent");
            if (target == null) return;

            var ctrl = target.gameObject.AddComponent<CharacterPartsGridController>();
            var so = new SerializedObject(ctrl);
            so.FindProperty("_partButtonPrefab").objectReferenceValue    = prefab;
            so.FindProperty("_onCategorySelected").objectReferenceValue  = evtCat;
            so.FindProperty("_onPartChanged").objectReferenceValue       = evtPart;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireCategoryLabel(GameObject root, StringEventChannel evtCat)
        {
            var target = FindDeep(root.transform, "PartsLabel");
            if (target == null) return;

            var label = target.gameObject.AddComponent<CharacterCategoryLabel>();
            var so = new SerializedObject(label);
            so.FindProperty("_onCategorySelected").objectReferenceValue = evtCat;
            so.FindProperty("_uppercase").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireResetButton(GameObject root, StringEventChannel evtCat)
        {
            var target = FindDeep(root.transform, "BtnReset");
            if (target == null) return;

            var btn = target.gameObject.AddComponent<CharacterResetButton>();
            var so = new SerializedObject(btn);
            so.FindProperty("_onCategorySelected").objectReferenceValue = evtCat;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireConfirmButton(GameObject root, VoidEventChannel evtConfirm)
        {
            var target = FindDeep(root.transform, "BtnConfirm");
            if (target == null) return;

            var btn = target.gameObject.AddComponent<CharacterConfirmButton>();
            var so = new SerializedObject(btn);
            so.FindProperty("_onCharacterConfirmed").objectReferenceValue = evtConfirm;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindDeep(child, name);
                if (found != null) return found;
            }
            return null;
        }

        #endregion

        #region Helpers

        private static bool ValidateAssets()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/rounded-panel.png");
            if (sprite != null) return true;

            EditorUtility.DisplayDialog("Assets manquants",
                "Sprites UI introuvables.\n\n" +
                "Lance d'abord :\nTools > GlimmerOfHope > 1 - Generate UI Style Assets\n" +
                "Puis : Tools > GlimmerOfHope > 2 - Generate Character UI Prefabs",
                "OK");
            return false;
        }

        private static string ToPascalCase(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        private static GameObject Child(string name, GameObject parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static GameObject UI(string name, GameObject parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void Stretch(GameObject go, Vector2 ancMin, Vector2 ancMax, Vector2 pivot)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = ancMin;
            rt.anchorMax        = ancMax;
            rt.pivot            = pivot;
            rt.anchoredPosition = Vector2.zero;
        }

        private static Image AddSlicedImage(GameObject go, string spriteName, Color color)
        {
            var img = go.AddComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/{spriteName}.png");
            img.type   = Image.Type.Sliced;
            img.color  = color;
            return img;
        }

        private static void AddShadowBehind(GameObject panel)
        {
            var shadow = UI("PanelShadow", panel);
            shadow.transform.SetAsFirstSibling();
            var srt = shadow.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(-4f, -6f);
            srt.offsetMax = new Vector2(4f, 2f);
            var img = shadow.AddComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_PATH}/shadow-soft.png");
            img.type   = Image.Type.Sliced;
            img.color  = new Color(0f, 0f, 0f, 0.08f);
            img.raycastTarget = false;
        }

        private static void AddText(string name, GameObject parent,
            Vector2 ancMin, Vector2 ancMax, Vector2 pivot,
            Vector2 pos, Vector2 size,
            string text, float fontSize, FontStyles style,
            Color color, TextAlignmentOptions align, bool fill = false)
        {
            var go = UI(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = ancMin;
            rt.anchorMax        = fill ? ancMax : ancMin;
            rt.pivot            = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta        = size;

            if (fill)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.fontStyle = style;
            tmp.color     = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
        }

        private static void AddLayoutElement(GameObject go, float prefH, float flexH)
        {
            var le = go.AddComponent<LayoutElement>();
            if (prefH >= 0f) le.preferredHeight = prefH;
            le.flexibleHeight = flexH;
        }

        #endregion
    }
}
