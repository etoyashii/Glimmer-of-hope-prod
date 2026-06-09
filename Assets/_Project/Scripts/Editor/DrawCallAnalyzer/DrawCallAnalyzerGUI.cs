using UnityEngine;
using UnityEditor;
using System.Linq;

namespace GlimmerOfHope.Editor
{
    public partial class DrawCallAnalyzer
    {
        #region Unity Lifecycle
        /// <summary>
        /// Handles the rendering of the editor window UI.
        /// </summary>
        private void OnGUI()
        {
            InitStyles();
            DrawHeader();
            DrawStats();
            DrawControls();
            EditorGUILayout.Space(4);
            DrawRendererList();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initializes all GUI styles used in the window. Only runs once.
        /// </summary>
        private void InitStyles()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };
            headerStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

            statBoxStyle = new GUIStyle("helpbox")
            {
                padding = new RectOffset(10, 10, 8, 8),
                margin = new RectOffset(4, 4, 4, 4)
            };

            warningRowStyle = new GUIStyle("CN EntryBackEven");
            warningRowStyle.normal.background = MakeTex(1, 1, new Color(0.6f, 0.1f, 0.1f, 0.35f));

            normalRowStyle = new GUIStyle("CN EntryBackEven");

            badgeRedStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            badgeRedStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);

            badgeOrangeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            badgeOrangeStyle.normal.textColor = new Color(1f, 0.75f, 0.2f);

            badgeGreenStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            badgeGreenStyle.normal.textColor = new Color(0.4f, 1f, 0.55f);

            stylesInitialized = true;
        }

        /// <summary>
        /// Creates a single-color texture for use in GUI backgrounds.
        /// </summary>
        private static Texture2D MakeTex(int width, int height, Color col)
        {
            var tex = new Texture2D(width, height);
            tex.SetPixels(Enumerable.Repeat(col, width * height).ToArray());
            tex.Apply();
            return tex;
        }

        /// <summary>
        /// Draws the header section with the window title and refresh button.
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.Space(6);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                GUILayout.Label("⬡  Draw Call Analyzer", headerStyle, GUILayout.Height(24));
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("↻ Refresh", EditorStyles.miniButton, GUILayout.Width(72)))
                    RefreshData();

                GUILayout.Space(6);
            }

            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
            EditorGUILayout.Space(4);
        }

        /// <summary>
        /// Draws the statistics cards (total draw calls, scanned objects, problematic objects).
        /// </summary>
        private void DrawStats()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // Color changes based on threshold: red if high, orange if medium, green if low
                DrawStatCard("TOTAL DRAW CALLS", totalDrawCalls.ToString(),
                    totalDrawCalls > 100 ? new Color(1f, 0.4f, 0.4f) :
                    totalDrawCalls > 50 ? new Color(1f, 0.75f, 0.2f) :
                                           new Color(0.4f, 1f, 0.55f));

                DrawStatCard("OBJETS SCANNÉS", rendererInfos.Count.ToString(), new Color(0.55f, 0.75f, 1f));

                DrawStatCard("PROBLÉMATIQUES", problematicObjects.Count.ToString(),
                    problematicObjects.Count > 0 ? new Color(1f, 0.55f, 0.2f) : new Color(0.4f, 1f, 0.55f));
            }
        }

        private void DrawStatCard(string label, string value, Color valueColor)
        {
            using (new EditorGUILayout.VerticalScope(statBoxStyle))
            {
                GUILayout.Label(label, EditorStyles.centeredGreyMiniLabel);
                var style = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 20,
                    alignment = TextAnchor.MiddleCenter
                };
                style.normal.textColor = valueColor;
                GUILayout.Label(value, style, GUILayout.Height(28));
            }
        }

        /// <summary>
        /// Draws the control panel with threshold slider, toggle options, and action buttons.
        /// </summary>
        private void DrawControls()
        {
            using (new EditorGUILayout.VerticalScope("helpbox"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Seuil draw calls :", GUILayout.Width(130));
                    int newThresh = EditorGUILayout.IntSlider(drawCallThreshold, 1, 20);
                    if (newThresh != drawCallThreshold)
                    {
                        drawCallThreshold = newThresh;
                        EditorPrefs.SetInt(PREFS_KEY, drawCallThreshold);
                        RefreshData();
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    showOnlyProblematic = EditorGUILayout.ToggleLeft(
                        "Afficher uniquement les problématiques", showOnlyProblematic, GUILayout.Width(260));
                    autoRefresh = EditorGUILayout.ToggleLeft("Auto-refresh", autoRefresh);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Sélectionner tous les problématiques", EditorStyles.miniButton))
                        SelectAllProblematic();

                    if (GUILayout.Button("Désélectionner", EditorStyles.miniButton, GUILayout.Width(100)))
                        Selection.objects = new UnityEngine.Object[0];
                }
            }
        }

        /// <summary>
        /// Draws the list of renderers, with filtering based on user preferences.
        /// </summary>
        private void DrawRendererList()
        {
            var list = showOnlyProblematic ? problematicObjects : rendererInfos;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Objet", EditorStyles.toolbarButton, GUILayout.Width(180));
                GUILayout.Label("Draw Calls", EditorStyles.toolbarButton, GUILayout.Width(80));
                GUILayout.Label("Matériaux", EditorStyles.toolbarButton, GUILayout.Width(80));
                GUILayout.Label("Statut", EditorStyles.toolbarButton, GUILayout.Width(80));
                GUILayout.Label("Actions", EditorStyles.toolbarButton);
            }

            using (var scroll = new EditorGUILayout.ScrollViewScope(scrollPos))
            {
                scrollPos = scroll.scrollPosition;

                if (list.Count == 0)
                {
                    EditorGUILayout.Space(20);
                    EditorGUILayout.HelpBox(
                        showOnlyProblematic
                            ? "✓ Aucun objet problématique détecté !"
                            : "Aucun renderer trouvé dans la scène.",
                        MessageType.Info);
                    return;
                }

                for (int i = 0; i < list.Count; i++)
                    DrawRow(list[i], i);
            }
        }

        /// <summary>
        /// Draws a single row in the renderer list, with color coding based on draw call count.
        /// </summary>
        private void DrawRow(RendererInfo info, int index)
        {
            bool isSelected = Selection.Contains(info.go);

            // Background color depends on the severity of the draw call count
            Color bg = info.isCritical ? new Color(0.55f, 0.1f, 0.1f, 0.4f) :
                       info.isProblematic ? new Color(0.55f, 0.35f, 0.05f, 0.35f) :
                       index % 2 == 0 ? new Color(0.2f, 0.2f, 0.2f, 0.1f) : Color.clear;

            var rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
            EditorGUI.DrawRect(rowRect, bg);

            if (isSelected)
                EditorGUI.DrawRect(rowRect, new Color(0.2f, 0.5f, 1f, 0.18f));

            string icon = info.isCritical ? "⛔" : info.isProblematic ? "⚠️" : "✓";

            if (GUILayout.Button($"{icon}  {info.name}", EditorStyles.label, GUILayout.Width(180), GUILayout.Height(20)))
            {
                Selection.activeGameObject = info.go;
                EditorGUIUtility.PingObject(info.go);
            }

            var dcStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            dcStyle.normal.textColor = info.isCritical ? new Color(1f, 0.4f, 0.4f) :
                                       info.isProblematic ? new Color(1f, 0.75f, 0.2f) :
                                                            new Color(0.6f, 0.9f, 0.6f);
            GUILayout.Label(info.drawCalls.ToString(), dcStyle, GUILayout.Width(80), GUILayout.Height(20));
            GUILayout.Label(info.materialCount.ToString(), EditorStyles.centeredGreyMiniLabel, GUILayout.Width(80), GUILayout.Height(20));

            string statusLabel = info.isCritical ? "CRITIQUE" : info.isProblematic ? "ATTENTION" : "OK";
            GUIStyle badgeStyle = info.isCritical ? badgeRedStyle : info.isProblematic ? badgeOrangeStyle : badgeGreenStyle;
            GUILayout.Label(statusLabel, badgeStyle, GUILayout.Width(80), GUILayout.Height(20));

            if (GUILayout.Button("Focus", EditorStyles.miniButton, GUILayout.Width(46), GUILayout.Height(16)))
            {
                Selection.activeGameObject = info.go;
                SceneView.lastActiveSceneView?.FrameSelected();
            }

            EditorGUILayout.EndHorizontal();
        }
        #endregion
    }
}