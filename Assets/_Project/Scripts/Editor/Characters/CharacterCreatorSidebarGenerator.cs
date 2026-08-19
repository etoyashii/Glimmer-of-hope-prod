using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Gameplay.Characters;
using GlimmerOfHope.UI.Widgets;
using GlimmerOfHope.UI.Character;
using static GlimmerOfHope.Editor.Characters.CharacterUIConstants;
using RectMask = UnityEngine.UI.RectMask2D;

namespace GlimmerOfHope.Editor.Characters
{
    // Genere la scene CharacterCreator avec la sidebar verticale accordeon.
    // Etape 3b : remplace l'etape 3 (CategoryBar horizontale) par la nouvelle sidebar.
    // Prerequis : etapes 1 et 2 faites (sprites + prefabs PartButton existants).
    public static class CharacterCreatorSidebarGenerator
    {
        #region Constants

        private const string DATA_PATH   = "Assets/_Project/Data";
        private const string PREFAB_PATH = "Assets/_Project/Prefabs/UI/Characters";

        private const float SIDEBAR_WIDTH     = 80f;
        private const float SIDEBAR_ITEM_H    = 80f;
        private const float SIDEBAR_SUBITEM_H = 56f;
        private const float SIDEBAR_ICON_SIZE = 40f;

        private const float COLOR_SECTION_H  = 140f; // COLOR_WHEEL_H + padding top/bottom (10+10)
        private const float COLOR_WHEEL_H    = 120f;

        private static CharacterRegistrySO _registry;

        #endregion

        #region Menu

        [MenuItem("Tools/GlimmerOfHope/Rebuild UI - Sidebar Verticale")]
        public static void Generate()
        {
            _registry = AssetDatabase.LoadAssetAtPath<CharacterRegistrySO>(
                $"{DATA_PATH}/Characters/_Registry.asset");

            if (_registry == null)
            {
                EditorUtility.DisplayDialog("Registry manquant",
                    "Registry introuvable.\nLance d'abord les etapes 1 et 2.", "OK");
                _registry = null;
                return;
            }

            if (!EditorUtility.DisplayDialog(
                "Rebuild UI - Sidebar Verticale",
                "Supprime et recree CharacterCreator avec la sidebar verticale accordeon.\n\nContinuer ?",
                "Continuer", "Annuler"))
            {
                _registry = null;
                return;
            }

            EnsureFolder(PREFAB_PATH);

            var parentPrefab = BuildSidebarParentPrefab();
            var leafPrefab   = BuildSidebarLeafPrefab();
            var partPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_PATH}/PartButtonPrefab.prefab");

            if (parentPrefab == null || leafPrefab == null)
            {
                EditorUtility.DisplayDialog("Erreur",
                    "Impossible de creer les prefabs sidebar.\nVerifie les logs.", "OK");
                _registry = null;
                return;
            }

            AssetDatabase.SaveAssets();

            var root = BuildSceneRoot();
            WireAllComponents(root, parentPrefab, leafPrefab, partPrefab);

            Selection.activeGameObject = root;

            // Sauvegarde immediatement pour persister l'etat initial de la scene generee.
            EditorSceneManager.SaveOpenScenes();

            _registry = null;

            EditorUtility.DisplayDialog("UI generee",
                "Scene reconstruite et sauvegardee.\n\n" +
                "Tout est cable automatiquement.\n" +
                "Verifier les StringEventChannels dans l'Inspector si manquants.",
                "OK");
        }

        #endregion

        #region Sidebar Prefabs

        private static GameObject BuildSidebarParentPrefab()
        {
            var path   = $"{PREFAB_PATH}/CategorySidebarParentItem.prefab";
            var canvas = TempCanvas();
            try
            {
                var root = MakeSidebarParentItemGO(canvas);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[SidebarGenerator] CategorySidebarParentItem sauvegarde.");
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        private static GameObject MakeSidebarParentItemGO(GameObject tempParent)
        {
            // Root - VerticalLayoutGroup empile Header + SubItemsPanel
            var root = UI("CategorySidebarParentItem", tempParent);

            var vlg = root.AddComponent<VerticalLayoutGroup>();
            vlg.spacing                = 0f;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth      = true;
            vlg.childControlHeight     = true;

            // Header : zone cliquable avec icon
            var header   = UI("Header", root);
            var headerLe = header.AddComponent<LayoutElement>();
            headerLe.preferredHeight = SIDEBAR_ITEM_H;
            headerLe.minHeight       = SIDEBAR_ITEM_H;

            // targetGraphic du Button (transparent, transitions hover/press)
            var headerImg = header.AddComponent<Image>();
            headerImg.color = Color.clear;

            var btn = header.AddComponent<Button>();
            var bc  = btn.colors;
            bc.normalColor      = Color.white;
            bc.highlightedColor = CARD_HOVER;
            bc.pressedColor     = CARD_PRESSED;
            bc.fadeDuration     = 0.10f;
            btn.colors        = bc;
            btn.targetGraphic = headerImg;

            // SelectionHighlight - fond CARD_SELECTED_BG + barre ACCENT (masque conjointement)
            var selHL   = UI("SelectionHighlight", header);
            var selHLRt = selHL.GetComponent<RectTransform>();
            selHLRt.anchorMin = Vector2.zero;
            selHLRt.anchorMax = Vector2.one;
            selHLRt.offsetMin = Vector2.zero;
            selHLRt.offsetMax = Vector2.zero;
            selHL.AddComponent<Image>().color = CARD_SELECTED_BG;
            selHL.SetActive(false);

            var selBar   = UI("AccentBar", selHL);
            var selBarRt = selBar.GetComponent<RectTransform>();
            selBarRt.anchorMin        = new Vector2(0f, 0f);
            selBarRt.anchorMax        = new Vector2(0f, 1f);
            selBarRt.pivot            = new Vector2(0f, 0.5f);
            selBarRt.sizeDelta        = new Vector2(5f, 0f);
            selBarRt.anchoredPosition = Vector2.zero;
            selBar.AddComponent<Image>().color = ACCENT;

            // IconBg - conteneur carre ACCENT_LIGHT (visible comme placeholder)
            var iconBg   = UI("IconBg", header);
            var iconBgRt = iconBg.GetComponent<RectTransform>();
            iconBgRt.anchorMin        = new Vector2(0.5f, 0.5f);
            iconBgRt.anchorMax        = new Vector2(0.5f, 0.5f);
            iconBgRt.pivot            = new Vector2(0.5f, 0.5f);
            iconBgRt.sizeDelta        = new Vector2(SIDEBAR_ICON_SIZE, SIDEBAR_ICON_SIZE);
            iconBgRt.anchoredPosition = Vector2.zero;
            iconBg.AddComponent<Image>().color = ACCENT_LIGHT;

            // Icon image (pour le sprite reel - transparent sans sprite)
            var icon   = UI("Icon", iconBg);
            var iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            var iconImg = icon.AddComponent<Image>();
            iconImg.color          = Color.clear;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget  = false;

            // IconLabel (TMPro - initiale de categorie quand pas de sprite)
            var iconLabel   = UI("IconLabel", iconBg);
            var iconLabelRt = iconLabel.GetComponent<RectTransform>();
            iconLabelRt.anchorMin = Vector2.zero;
            iconLabelRt.anchorMax = Vector2.one;
            iconLabelRt.offsetMin = Vector2.zero;
            iconLabelRt.offsetMax = Vector2.zero;
            var iconLabelTmp = iconLabel.AddComponent<TextMeshProUGUI>();
            iconLabelTmp.text         = "?";
            iconLabelTmp.fontSize     = 18f;
            iconLabelTmp.fontStyle    = FontStyles.Bold;
            iconLabelTmp.color        = ACCENT_DARK;
            iconLabelTmp.alignment    = TextAlignmentOptions.Center;
            iconLabelTmp.raycastTarget = false;

            // SubItemsPanel - hauteur DOTweenee de 0 a N
            var subPanel   = UI("SubItemsPanel", root);
            var subPanelLe = subPanel.AddComponent<LayoutElement>();
            subPanelLe.preferredHeight = 0f;
            subPanelLe.minHeight       = 0f;
            subPanel.AddComponent<RectMask>();

            // SubItemsContent - enfants spawnes ici par CategorySidebarController
            var subContent   = UI("SubItemsContent", subPanel);
            var subContentRt = subContent.GetComponent<RectTransform>();
            subContentRt.anchorMin = new Vector2(0f, 1f);
            subContentRt.anchorMax = new Vector2(1f, 1f);
            subContentRt.pivot     = new Vector2(0.5f, 1f);
            subContentRt.offsetMin = Vector2.zero;
            subContentRt.offsetMax = Vector2.zero;
            var subVlg = subContent.AddComponent<VerticalLayoutGroup>();
            subVlg.spacing                = 0f;
            subVlg.childForceExpandWidth  = true;
            subVlg.childForceExpandHeight = false;
            subVlg.childControlWidth      = true;
            subVlg.childControlHeight     = true;
            subContent.SetActive(false);

            // Wire CategorySidebarItemView
            var view   = root.AddComponent<CategorySidebarItemView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("_button").objectReferenceValue                = btn;
            viewSo.FindProperty("_iconImage").objectReferenceValue             = iconImg;
            viewSo.FindProperty("_iconLabel").objectReferenceValue             = iconLabelTmp;
            viewSo.FindProperty("_selectionHighlight").objectReferenceValue    = selHL;
            viewSo.FindProperty("_subItemsLayoutElement").objectReferenceValue = subPanelLe;
            viewSo.FindProperty("_subItemsContent").objectReferenceValue       = subContent.transform;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static GameObject BuildSidebarLeafPrefab()
        {
            var path   = $"{PREFAB_PATH}/CategorySidebarLeafItem.prefab";
            var canvas = TempCanvas();
            try
            {
                var root   = MakeSidebarLeafItemGO(canvas);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[SidebarGenerator] CategorySidebarLeafItem sauvegarde.");
                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(canvas);
            }
        }

        private static GameObject MakeSidebarLeafItemGO(GameObject tempParent)
        {
            const float LEAF_ICON = 32f;

            var root = UI("CategorySidebarLeafItem", tempParent);

            var le = root.AddComponent<LayoutElement>();
            le.preferredHeight = SIDEBAR_ITEM_H;
            le.minHeight       = SIDEBAR_ITEM_H;

            var rootImg = root.AddComponent<Image>();
            rootImg.color = Color.clear;

            var btn = root.AddComponent<Button>();
            var bc  = btn.colors;
            bc.normalColor      = Color.white;
            bc.highlightedColor = CARD_HOVER;
            bc.pressedColor     = CARD_PRESSED;
            bc.fadeDuration     = 0.10f;
            btn.colors        = bc;
            btn.targetGraphic = rootImg;

            // SelectionHighlight - fond + barre accent
            var selHL   = UI("SelectionHighlight", root);
            var selHLRt = selHL.GetComponent<RectTransform>();
            selHLRt.anchorMin = Vector2.zero;
            selHLRt.anchorMax = Vector2.one;
            selHLRt.offsetMin = Vector2.zero;
            selHLRt.offsetMax = Vector2.zero;
            selHL.AddComponent<Image>().color = CARD_SELECTED_BG;
            selHL.SetActive(false);

            var selBar   = UI("AccentBar", selHL);
            var selBarRt = selBar.GetComponent<RectTransform>();
            selBarRt.anchorMin        = new Vector2(0f, 0f);
            selBarRt.anchorMax        = new Vector2(0f, 1f);
            selBarRt.pivot            = new Vector2(0f, 0.5f);
            selBarRt.sizeDelta        = new Vector2(5f, 0f);
            selBarRt.anchoredPosition = Vector2.zero;
            selBar.AddComponent<Image>().color = ACCENT;

            // IconBg (indente vers la droite pour distinguer visuellement des parents)
            var iconBg   = UI("IconBg", root);
            var iconBgRt = iconBg.GetComponent<RectTransform>();
            iconBgRt.anchorMin        = new Vector2(0.5f, 0.5f);
            iconBgRt.anchorMax        = new Vector2(0.5f, 0.5f);
            iconBgRt.pivot            = new Vector2(0.5f, 0.5f);
            iconBgRt.sizeDelta        = new Vector2(LEAF_ICON, LEAF_ICON);
            iconBgRt.anchoredPosition = new Vector2(6f, 0f); // leger decalage droit
            iconBg.AddComponent<Image>().color = ACCENT_LIGHT;

            // Icon image (sprite slot)
            var icon   = UI("Icon", iconBg);
            var iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(4f, 4f);
            iconRt.offsetMax = new Vector2(-4f, -4f);
            var iconImg = icon.AddComponent<Image>();
            iconImg.color          = Color.clear;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget  = false;

            // IconLabel (initiale de la sous-categorie)
            var iconLabel   = UI("IconLabel", iconBg);
            var iconLabelRt = iconLabel.GetComponent<RectTransform>();
            iconLabelRt.anchorMin = Vector2.zero;
            iconLabelRt.anchorMax = Vector2.one;
            iconLabelRt.offsetMin = Vector2.zero;
            iconLabelRt.offsetMax = Vector2.zero;
            var iconLabelTmp = iconLabel.AddComponent<TextMeshProUGUI>();
            iconLabelTmp.text         = "?";
            iconLabelTmp.fontSize     = 13f;
            iconLabelTmp.fontStyle    = FontStyles.Bold;
            iconLabelTmp.color        = ACCENT_DARK;
            iconLabelTmp.alignment    = TextAlignmentOptions.Center;
            iconLabelTmp.raycastTarget = false;

            var view   = root.AddComponent<CategorySidebarItemView>();
            var viewSo = new SerializedObject(view);
            viewSo.FindProperty("_button").objectReferenceValue             = btn;
            viewSo.FindProperty("_iconImage").objectReferenceValue          = iconImg;
            viewSo.FindProperty("_iconLabel").objectReferenceValue          = iconLabelTmp;
            viewSo.FindProperty("_selectionHighlight").objectReferenceValue = selHL;
            viewSo.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        #endregion

        #region Scene Hierarchy

        private static GameObject BuildSceneRoot()
        {
            var existing = GameObject.Find("CharacterCreator");
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            var root = new GameObject("CharacterCreator");
            Undo.RegisterCreatedObjectUndo(root, "Rebuild CharacterCreator UI");

            BuildCamera(root);
            BuildCharacterPreview(root);
            BuildCanvas(root);
            BuildEventSystem(root);

            return root;
        }

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
            cam.rect            = new Rect(0f, 0.052f, PANEL_SPLIT, 0.892f);

            var light = Child("DirectionalLight", parent);
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            var l = light.AddComponent<Light>();
            l.type      = LightType.Directional;
            l.intensity = 1.2f;
            l.color     = new Color(1f, 0.97f, 0.92f);
        }

        private static void BuildCharacterPreview(GameObject parent)
        {
            Child("CharacterPreview", parent);
        }

        private static void BuildEventSystem(GameObject parent)
        {
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var go = Child("EventSystem", parent);
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

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

        private static void BuildTopBar(GameObject canvas)
        {
            var go = UI("TopBar", canvas);
            Pin(go, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, TOPBAR_HEIGHT));
            AddImg(go, TOPBAR);
            TextFill("Title", go, "Creer mon personnage",
                FONT_TITLE, FontStyles.Bold, TOPBAR_TEXT, TextAlignmentOptions.Left,
                new Vector4(24f, 0f, 24f, 0f));
        }

        private static void BuildPreviewZone(GameObject canvas)
        {
            var go = UI("PreviewZone", canvas);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(PANEL_SPLIT, 1f);
            rt.offsetMin = new Vector2(PANEL_GAP, BOTTOMBAR_HEIGHT + PANEL_GAP);
            rt.offsetMax = new Vector2(-PANEL_GAP / 2f, -(TOPBAR_HEIGHT + PANEL_GAP));
            AddImg(go, PREVIEW_OVERLAY);
        }

        private static void BuildRightPanel(GameObject canvas)
        {
            var go = UI("RightPanel", canvas);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(PANEL_SPLIT, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(PANEL_GAP / 2f, BOTTOMBAR_HEIGHT + PANEL_GAP);
            rt.offsetMax = new Vector2(-PANEL_GAP, -(TOPBAR_HEIGHT + PANEL_GAP));
            AddImg(go, PANEL_BG);

            // Sidebar gauche | Separateur | Zone de contenu droite
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing            = 0f;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth  = true;
            hlg.childControlHeight = true;

            BuildCategorySidebar(go);
            BuildVerticalDivider(go);
            BuildContentArea(go);
        }

        private static void BuildCategorySidebar(GameObject parent)
        {
            var go = UI("CategorySidebar", parent);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = SIDEBAR_WIDTH;
            le.minWidth       = SIDEBAR_WIDTH;
            le.flexibleWidth  = 0f;

            var sidebarColor = new Color(PANEL_BG.r - 0.03f, PANEL_BG.g - 0.03f, PANEL_BG.b - 0.03f);
            AddImg(go, sidebarColor);

            // ScrollRect pour gerer l'overflow si beaucoup de categories
            go.AddComponent<RectMask>();
            var scroll = go.AddComponent<ScrollRect>();
            scroll.horizontal        = false;
            scroll.vertical          = true;
            scroll.movementType      = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            scroll.viewport          = go.GetComponent<RectTransform>();

            var content   = UI("SidebarContent", go);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            var contentVlg = content.AddComponent<VerticalLayoutGroup>();
            contentVlg.spacing            = 0f;
            contentVlg.childForceExpandWidth  = true;
            contentVlg.childForceExpandHeight = false;
            contentVlg.childControlWidth  = true;
            contentVlg.childControlHeight = true;
            contentVlg.padding            = new RectOffset(0, 0, 8, 8);

            var csf = content.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRt;
        }

        private static void BuildVerticalDivider(GameObject parent)
        {
            var go = UI("VerticalDivider", parent);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 1f;
            le.minWidth       = 1f;
            le.flexibleWidth  = 0f;
            AddImg(go, DIVIDER);
        }

        private static void BuildContentArea(GameObject parent)
        {
            var go = UI("ContentArea", parent);
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;

            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing            = 0f;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth  = true;
            vlg.childControlHeight = true;

            BuildPartsLabel(go);
            BuildHorizontalDivider(go);
            BuildPartsGrid(go);
            BuildColorPickerSection(go);
        }

        private static void BuildPartsLabel(GameObject parent)
        {
            var go = UI("PartsLabel", parent);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 44f;
            le.minHeight       = 44f;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text             = "CATEGORIE";
            tmp.fontSize         = FONT_SECTION;
            tmp.fontStyle        = FontStyles.Bold;
            tmp.color            = TEXT_MUTED;
            tmp.alignment        = TextAlignmentOptions.Left;
            tmp.characterSpacing = 2.5f;
            tmp.margin           = new Vector4(GRID_PADDING, 0f, 0f, 0f);
            tmp.raycastTarget    = false;
        }

        private static void BuildHorizontalDivider(GameObject parent)
        {
            var go = UI("Divider", parent);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 1f;
            le.minHeight       = 1f;
            AddImg(go, DIVIDER);
        }

        private static void BuildPartsGrid(GameObject parent)
        {
            var scroll = UI("PartsGridScroll", parent);
            var le     = scroll.AddComponent<LayoutElement>();
            le.flexibleHeight = 1f;

            scroll.AddComponent<RectMask>();
            var sr = scroll.AddComponent<ScrollRect>();
            sr.horizontal        = false;
            sr.vertical          = true;
            sr.movementType      = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 30f;
            sr.viewport          = scroll.GetComponent<RectTransform>();

            var content   = UI("PartsContent", scroll);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = Vector2.zero;

            int p = (int)GRID_PADDING;
            var grid = content.AddComponent<GridLayoutGroup>();
            grid.padding         = new RectOffset(p, p, 8, p);
            grid.cellSize        = new Vector2(GRID_CELL_SIZE, GRID_CELL_SIZE);
            grid.spacing         = new Vector2(GRID_SPACING, GRID_SPACING);
            grid.startCorner     = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis       = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment  = TextAnchor.UpperLeft;
            grid.constraint      = GridLayoutGroup.Constraint.Flexible;

            var csf = content.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

            sr.content = contentRt;
        }

        private static void BuildColorPickerSection(GameObject parent)
        {
            var go = UI("ColorPickerSection", parent);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = COLOR_SECTION_H;
            le.minHeight       = COLOR_SECTION_H;

            go.AddComponent<Image>().color = new Color(PANEL_BG.r - 0.02f, PANEL_BG.g - 0.02f, PANEL_BG.b - 0.02f);

            // Invisible et hors layout par defaut : CharacterColorPicker gere la visibilite
            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha          = 0f;
            cg.blocksRaycasts = false;
            cg.interactable   = false;
            le.ignoreLayout   = true;
            // flexibleHeight=0 empeche le HLG enfant de remonter un flexibleHeight=1
            // au parent VLG (ContentArea), ce qui sinon volait la moitie de l'espace flexible.
            le.flexibleHeight  = 0f;

            // Layout horizontal : roue a gauche, colonne sliders a droite
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing                = 10f;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth      = true;
            hlg.childControlHeight     = true;
            hlg.padding                = new RectOffset(10, 10, 10, 10);

            // Roue chromatique : carre fixe COLOR_WHEEL_H x COLOR_WHEEL_H
            // padding (10+10) + COLOR_WHEEL_H = COLOR_SECTION_H -> hauteur = largeur = 120
            var wheelGo = UI("ColorWheel", go);
            var wheelLe = wheelGo.AddComponent<LayoutElement>();
            wheelLe.preferredWidth  = COLOR_WHEEL_H;
            wheelLe.minWidth        = COLOR_WHEEL_H;
            wheelLe.flexibleWidth   = 0f;
            wheelLe.flexibleHeight  = 0f;
            wheelGo.AddComponent<RawImage>();

            // Colonne droite : trois sliders RGB + bande preview
            var slidersPanel = UI("SlidersPanel", go);
            slidersPanel.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var panelVlg = slidersPanel.AddComponent<VerticalLayoutGroup>();
            panelVlg.spacing                = 4f;
            panelVlg.childForceExpandWidth  = true;
            panelVlg.childForceExpandHeight = false;
            panelVlg.childControlWidth      = true;
            panelVlg.childControlHeight     = true;
            panelVlg.childAlignment         = TextAnchor.MiddleCenter;

            BuildSliderRow("SliderRowR", slidersPanel, "R", new Color(0.85f, 0.30f, 0.30f));
            BuildSliderRow("SliderRowG", slidersPanel, "G", new Color(0.30f, 0.75f, 0.30f));
            BuildSliderRow("SliderRowB", slidersPanel, "B", new Color(0.30f, 0.50f, 0.85f));

            var previewGo = UI("ColorPreview", slidersPanel);
            var previewLe = previewGo.AddComponent<LayoutElement>();
            previewLe.preferredHeight = 16f;
            previewLe.minHeight       = 16f;
            previewGo.AddComponent<Image>().color = Color.white;

            go.AddComponent<CharacterColorPicker>();
        }

        private static void BuildSliderRow(string rowName, GameObject parent, string label, Color fillColor)
        {
            const float ROW_H   = 20f;
            const float LABEL_W = 14f;

            var row   = UI(rowName, parent);
            var rowLe = row.AddComponent<LayoutElement>();
            rowLe.preferredHeight = ROW_H;
            var rowHlg = row.AddComponent<HorizontalLayoutGroup>();
            rowHlg.spacing                = 6f;
            rowHlg.childAlignment         = TextAnchor.MiddleLeft;
            rowHlg.childForceExpandWidth  = false;
            rowHlg.childForceExpandHeight = true;
            rowHlg.childControlWidth      = true;
            rowHlg.childControlHeight     = true;

            // Lettre (R / G / B)
            var lblGo   = UI("Label", row);
            var lblLe   = lblGo.AddComponent<LayoutElement>();
            lblLe.preferredWidth = LABEL_W;
            var lblTmp  = lblGo.AddComponent<TMPro.TextMeshProUGUI>();
            lblTmp.text          = label;
            lblTmp.fontSize      = 11f;
            lblTmp.color         = fillColor;
            lblTmp.alignment     = TMPro.TextAlignmentOptions.MidlineRight;
            lblTmp.raycastTarget = false;

            // Slider Unity avec la hierarchie requise
            var sliderGo   = UI("Slider", row);
            var sliderLe   = sliderGo.AddComponent<LayoutElement>();
            sliderLe.flexibleWidth = 1f;
            var slider     = sliderGo.AddComponent<Slider>();
            slider.minValue  = 0f;
            slider.maxValue  = 1f;
            slider.value     = 1f;
            slider.direction = Slider.Direction.LeftToRight;
            slider.wholeNumbers = false;

            // Background
            var bgGo = UI("Background", sliderGo);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(0f,  3f);
            bgRt.offsetMax = new Vector2(0f, -3f);
            bgGo.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f);

            // Fill Area
            var fillAreaGo = UI("Fill Area", sliderGo);
            var fillAreaRt = fillAreaGo.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.offsetMin = new Vector2(5f,   3f);
            fillAreaRt.offsetMax = new Vector2(-15f, -3f);

            var fillGo = UI("Fill", fillAreaGo);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            fillGo.AddComponent<Image>().color = fillColor;

            // Handle Slide Area
            var handleAreaGo = UI("Handle Slide Area", sliderGo);
            var handleAreaRt = handleAreaGo.GetComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = new Vector2(10f, 0f);
            handleAreaRt.offsetMax = new Vector2(-10f, 0f);

            var handleGo = UI("Handle", handleAreaGo);
            var handleRt = handleGo.GetComponent<RectTransform>();
            handleRt.anchorMin = new Vector2(0f, 0f);
            handleRt.anchorMax = new Vector2(0f, 1f);
            handleRt.sizeDelta = new Vector2(16f, 0f);
            handleRt.anchoredPosition = Vector2.zero;
            var handleImg = handleGo.AddComponent<Image>();
            handleImg.color = new Color(0.9f, 0.9f, 0.9f);

            // Wire Slider
            slider.targetGraphic = handleImg;
            slider.fillRect      = fillRt;
            slider.handleRect    = handleRt;
        }

        private static void BuildBottomBar(GameObject canvas)
        {
            var go = UI("BottomBar", canvas);
            Pin(go, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, BOTTOMBAR_HEIGHT));
            AddImg(go, PANEL_BG);

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.padding            = new RectOffset(24, 16, 0, 0);
            hlg.spacing            = 16f;
            hlg.childAlignment     = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth  = true;
            hlg.childControlHeight = true;

            // Texte de statut (prend tout l'espace restant)
            var statusGo = UI("StatusText", go);
            var statusLe = statusGo.AddComponent<LayoutElement>();
            statusLe.flexibleWidth = 1f;
            var statusTmp = statusGo.AddComponent<TextMeshProUGUI>();
            statusTmp.text      = "Personnalisez votre personnage";
            statusTmp.fontStyle = FontStyles.Normal;
            statusTmp.color     = TEXT_MUTED;
            statusTmp.fontSize  = FONT_STATUS;
            statusTmp.alignment = TextAlignmentOptions.Left;

            // Bouton Confirmer
            const float BTN_W = 200f;
            const float BTN_H = 40f;
            var btnGo = UI("ConfirmButton", go);
            var btnLe = btnGo.AddComponent<LayoutElement>();
            btnLe.preferredWidth  = BTN_W;
            btnLe.preferredHeight = BTN_H;
            btnLe.minWidth        = BTN_W;
            btnGo.AddComponent<Image>().color = CONFIRM;
            btnGo.AddComponent<Button>();
            TextFill("Label", btnGo, "CONFIRMER",
                FONT_BUTTON, FontStyles.Bold, TEXT_ON_ACCENT,
                TextAlignmentOptions.Center, Vector4.zero);
        }

        #endregion

        #region Auto-Wire

        private static void WireAllComponents(
            GameObject root,
            GameObject parentPrefab,
            GameObject leafPrefab,
            GameObject partPrefab)
        {
            var evtCat  = AssetDatabase.LoadAssetAtPath<StringEventChannel>(
                $"{DATA_PATH}/Events/Characters/OnCategorySelected.asset");
            var evtPart = AssetDatabase.LoadAssetAtPath<StringEventChannel>(
                $"{DATA_PATH}/Events/Characters/OnCharacterPartChanged.asset");
            var evtConfirm = AssetDatabase.LoadAssetAtPath<VoidEventChannel>(
                $"{DATA_PATH}/Events/Characters/OnCharacterConfirmed.asset");

            if (evtCat == null || evtPart == null)
                Debug.LogWarning(
                    "[SidebarGenerator] EventChannels introuvables - assigner manuellement dans l'Inspector.");

            WireBootstrapper(root, evtPart);
            WirePreviewRenderer(root, evtPart);
            WireSidebarController(root, parentPrefab, leafPrefab, evtCat);
            WirePartsGrid(root, partPrefab, evtCat, evtPart);
            WireCategoryLabel(root, evtCat);
            WireConfirmButton(root);
            WireColorPicker(root, evtCat);
        }

        private static void WireBootstrapper(GameObject root, StringEventChannel evtPart)
        {
            var boot = root.AddComponent<CharacterSystemBootstrapper>();
            var so   = new SerializedObject(boot);
            so.FindProperty("_characterRegistry").objectReferenceValue      = _registry;
            so.FindProperty("_onCharacterPartChanged").objectReferenceValue = evtPart;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WirePreviewRenderer(GameObject root, StringEventChannel evtPart)
        {
            var preview = root.transform.Find("CharacterPreview");
            if (preview == null) return;

            var renderer = preview.gameObject.AddComponent<CharacterPreviewRenderer>();
            var so       = new SerializedObject(renderer);
            so.FindProperty("_onPartChanged").objectReferenceValue = evtPart;

            var masterPrefab = _registry?.MasterCharacterPrefab
                ?? CharacterPartsImporter.FindMasterCharacterPrefab();
            if (masterPrefab != null)
                so.FindProperty("_masterCharacterPrefab").objectReferenceValue = masterPrefab;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireSidebarController(
            GameObject root,
            GameObject parentPrefab,
            GameObject leafPrefab,
            StringEventChannel evtCat)
        {
            var target = FindDeep(root.transform, "SidebarContent");
            if (target == null) { Debug.LogWarning("[SidebarGenerator] SidebarContent introuvable."); return; }

            var ctrl = target.gameObject.AddComponent<CategorySidebarController>();
            var so   = new SerializedObject(ctrl);
            so.FindProperty("_parentItemPrefab").objectReferenceValue    = parentPrefab;
            so.FindProperty("_leafItemPrefab").objectReferenceValue      = leafPrefab;
            so.FindProperty("_subItemHeight").floatValue                 = SIDEBAR_SUBITEM_H;
            so.FindProperty("_onCategorySelected").objectReferenceValue  = evtCat;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WirePartsGrid(
            GameObject root,
            GameObject partPrefab,
            StringEventChannel evtCat,
            StringEventChannel evtPart)
        {
            var target = FindDeep(root.transform, "PartsContent");
            if (target == null) return;

            var ctrl = target.gameObject.AddComponent<CharacterPartsGridController>();
            var so   = new SerializedObject(ctrl);
            so.FindProperty("_partButtonPrefab").objectReferenceValue   = partPrefab;
            so.FindProperty("_onCategorySelected").objectReferenceValue = evtCat;
            so.FindProperty("_onPartChanged").objectReferenceValue      = evtPart;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireCategoryLabel(GameObject root, StringEventChannel evtCat)
        {
            var target = FindDeep(root.transform, "PartsLabel");
            if (target == null) return;

            var label = target.gameObject.AddComponent<CharacterCategoryLabel>();
            var so    = new SerializedObject(label);
            so.FindProperty("_onCategorySelected").objectReferenceValue = evtCat;
            so.FindProperty("_uppercase").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireColorPicker(GameObject root, StringEventChannel evtCat)
        {
            var target = FindDeep(root.transform, "ColorPickerSection");
            if (target == null)
            {
                Debug.LogWarning("[SidebarGenerator] ColorPickerSection introuvable.");
                return;
            }

            var picker = target.GetComponent<CharacterColorPicker>();
            if (picker == null) return;

            var so = new SerializedObject(picker);
            so.FindProperty("_onCategoryChanged").objectReferenceValue = evtCat;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireConfirmButton(GameObject root)
        {
            var target = FindDeep(root.transform, "ConfirmButton");
            if (target == null)
            {
                Debug.LogWarning("[SidebarGenerator] ConfirmButton introuvable - assigner manuellement.");
                return;
            }

            var btn  = target.gameObject.AddComponent<CharacterConfirmButton>();
            // La scene cible est laissee vide : l'assigner dans l'Inspector une fois la scene connue.
            var so   = new SerializedObject(btn);
            so.FindProperty("_targetScene").stringValue = "";
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion

        #region Helpers

        private static GameObject TempCanvas()
        {
            var go = new GameObject("__TempCanvas__");
            go.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            go.hideFlags = HideFlags.HideAndDontSave;
            return go;
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

        // Ancre en bord (pour TopBar, BottomBar)
        private static void Pin(GameObject go,
            Vector2 ancMin, Vector2 ancMax, Vector2 pivot, Vector2 sizeDelta)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin        = ancMin;
            rt.anchorMax        = ancMax;
            rt.pivot            = pivot;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = sizeDelta;
        }

        private static Image AddImg(GameObject go, Color color)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private static void TextFill(string name, GameObject parent,
            string text, float fontSize, FontStyles style,
            Color color, TextAlignmentOptions align, Vector4 margin)
        {
            var go = UI(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text         = text;
            tmp.fontSize     = fontSize;
            tmp.fontStyle    = style;
            tmp.color        = color;
            tmp.alignment    = align;
            tmp.margin       = margin;
            tmp.raycastTarget = false;
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

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var cur   = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        #endregion
    }
}
