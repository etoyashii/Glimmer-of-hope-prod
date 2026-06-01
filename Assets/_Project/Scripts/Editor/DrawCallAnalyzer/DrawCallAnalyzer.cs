
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace GlimmerOfHope.Editor
{

    public class DrawCallAnalyzer : EditorWindow
    {
        #region Constants

        private const string WINDOW_TITLE = "Draw Call Analyzer";
        private const string PREFS_KEY = "DrawCallAnalyzer_Threshold";
        private const int DEFAULT_THRESH = 3;
        #endregion

        #region Private Fields

        private List<RendererInfo> rendererInfos = new List<RendererInfo>();
        private List<RendererInfo> problematicObjects = new List<RendererInfo>();

        private int drawCallThreshold = DEFAULT_THRESH;
        private int totalDrawCalls = 0;
        private bool showOnlyProblematic = false;
        private bool autoRefresh = true;

        private Vector2 scrollPos;
        private double lastRefreshTime;
        private const double AUTO_REFRESH_INTERVAL = 1.0; 

        private GUIStyle headerStyle;
        private GUIStyle statBoxStyle;
        private GUIStyle warningRowStyle;
        private GUIStyle normalRowStyle;
        private GUIStyle badgeRedStyle;
        private GUIStyle badgeGreenStyle;
        private GUIStyle badgeOrangeStyle;
        private bool stylesInitialized = false;

        private static readonly Color HIGHLIGHT_CRITICAL = new Color(1f, 0.15f, 0.15f, 0.55f);
        private static readonly Color HIGHLIGHT_WARNING = new Color(1f, 0.65f, 0f, 0.40f);
        private static readonly Color HIGHLIGHT_OK = new Color(0.2f, 0.9f, 0.4f, 0.20f);
        #endregion
        private struct RendererInfo
        {
            public Renderer renderer;
            public GameObject go;
            public string name;
            public int drawCalls;
            public int materialCount;
            public bool isProblematic;
            public bool isCritical;     
        }


        [MenuItem("Tools/Draw Call Analyzer %#d")]
        #region Public Methods

        public static void OpenWindow()
        {
            var window = GetWindow<DrawCallAnalyzer>(WINDOW_TITLE);
            window.minSize = new Vector2(420, 340);
            window.Show();
        }
        #endregion

        #region Private Methods
        private void OnEnable()
        {
            drawCallThreshold = EditorPrefs.GetInt(PREFS_KEY, DEFAULT_THRESH);
            SceneView.duringSceneGui += OnSceneGUI;
            RefreshData();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            ClearHighlights();
        }

        private void OnDestroy()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            ClearHighlights();
        }

        private void OnFocus()
        {
            RefreshData();
        }

        private void Update()
        {
            if (autoRefresh && EditorApplication.timeSinceStartup - lastRefreshTime > AUTO_REFRESH_INTERVAL)
            {
                RefreshData();
                Repaint();
            }
        }

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

        private static Texture2D MakeTex(int width, int height, Color col)
        {
            var tex = new Texture2D(width, height);
            tex.SetPixels(Enumerable.Repeat(col, width * height).ToArray());
            tex.Apply();
            return tex;
        }

        // ─── Collecte des données ─────────────────────────────────────────────────
        private void RefreshData()
        {
            lastRefreshTime = EditorApplication.timeSinceStartup;
            rendererInfos.Clear();
            problematicObjects.Clear();
            totalDrawCalls = 0;

            var allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);

            foreach (var r in allRenderers)
            {
                if (!r.gameObject.activeInHierarchy || !r.enabled) continue;

                int mats = r.sharedMaterials.Length;
                // En Unity, chaque material = 1 draw call (hors batching GPU/SRP)
                // On tient compte du submesh count si c'est un MeshRenderer
                int dc = CalculateDrawCalls(r);

                totalDrawCalls += dc;

                var info = new RendererInfo
                {
                    renderer = r,
                    go = r.gameObject,
                    name = r.gameObject.name,
                    drawCalls = dc,
                    materialCount = mats,
                    isProblematic = dc > drawCallThreshold,
                    isCritical = dc > drawCallThreshold * 2
                };

                rendererInfos.Add(info);

                if (info.isProblematic)
                    problematicObjects.Add(info);
            }

            // Tri : plus de draw calls en premier
            rendererInfos = rendererInfos.OrderByDescending(x => x.drawCalls).ToList();
            problematicObjects = problematicObjects.OrderByDescending(x => x.drawCalls).ToList();

            SceneView.RepaintAll();
        }

        private int CalculateDrawCalls(Renderer r)
        {
            // Chaque material unique génère un draw call
            // Pour les MeshRenderer, on vérifie aussi les submeshes
            int matCount = r.sharedMaterials.Count(m => m != null);

            if (r is MeshRenderer mr)
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    int submeshes = mf.sharedMesh.subMeshCount;
                    // draw calls = max(submeshes, materials non-null)
                    return Mathf.Max(submeshes, matCount);
                }
            }
            else if (r is SkinnedMeshRenderer smr)
            {
                if (smr.sharedMesh != null)
                    return Mathf.Max(smr.sharedMesh.subMeshCount, matCount);
            }

            return Mathf.Max(1, matCount);
        }

        private void OnGUI()
        {
            InitStyles();

            DrawHeader();
            DrawStats();
            DrawControls();
            EditorGUILayout.Space(4);
            DrawRendererList();
        }

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

        private void DrawStats()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
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
                    showOnlyProblematic = EditorGUILayout.ToggleLeft("Afficher uniquement les problématiques", showOnlyProblematic, GUILayout.Width(260));
                    autoRefresh = EditorGUILayout.ToggleLeft("Auto-refresh", autoRefresh);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Sélectionner tous les problématiques", EditorStyles.miniButton))
                        SelectAllProblematic();

                    if (GUILayout.Button("Désélectionner", EditorStyles.miniButton, GUILayout.Width(100)))
                        Selection.objects = new Object[0];
                }
            }
        }

        private void DrawRendererList()
        {
            var list = showOnlyProblematic ? problematicObjects : rendererInfos;

            // En-tête tableau
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
                {
                    var info = list[i];
                    DrawRow(info, i);
                }
            }
        }

        private void DrawRow(RendererInfo info, int index)
        {
            bool isSelected = Selection.Contains(info.go);
            Color bg = info.isCritical ? new Color(0.55f, 0.1f, 0.1f, 0.4f) :
                       info.isProblematic ? new Color(0.55f, 0.35f, 0.05f, 0.35f) :
                                            (index % 2 == 0 ? new Color(0.2f, 0.2f, 0.2f, 0.1f) : Color.clear);

            var rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(20));
            EditorGUI.DrawRect(rowRect, bg);

            if (isSelected)
                EditorGUI.DrawRect(rowRect, new Color(0.2f, 0.5f, 1f, 0.18f));

            // Icône
            string icon = info.isCritical ? "⛔" : info.isProblematic ? "⚠️" : "✓";

            // Nom (cliquable)
            if (GUILayout.Button($"{icon}  {info.name}", EditorStyles.label, GUILayout.Width(180), GUILayout.Height(20)))
            {
                Selection.activeGameObject = info.go;
                EditorGUIUtility.PingObject(info.go);
            }

            // Draw calls
            var dcStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            dcStyle.normal.textColor = info.isCritical ? new Color(1f, 0.4f, 0.4f) :
                                       info.isProblematic ? new Color(1f, 0.75f, 0.2f) :
                                                            new Color(0.6f, 0.9f, 0.6f);
            GUILayout.Label(info.drawCalls.ToString(), dcStyle, GUILayout.Width(80), GUILayout.Height(20));

            // Matériaux
            GUILayout.Label(info.materialCount.ToString(), EditorStyles.centeredGreyMiniLabel, GUILayout.Width(80), GUILayout.Height(20));

            // Badge statut
            string statusLabel = info.isCritical ? "CRITIQUE" : info.isProblematic ? "ATTENTION" : "OK";
            GUIStyle badgeStyle = info.isCritical ? badgeRedStyle : info.isProblematic ? badgeOrangeStyle : badgeGreenStyle;
            GUILayout.Label(statusLabel, badgeStyle, GUILayout.Width(80), GUILayout.Height(20));

            // Bouton focus
            if (GUILayout.Button("Focus", EditorStyles.miniButton, GUILayout.Width(46), GUILayout.Height(16)))
            {
                Selection.activeGameObject = info.go;
                SceneView.lastActiveSceneView?.FrameSelected();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void SelectAllProblematic()
        {
            Selection.objects = problematicObjects.Select(x => (Object)x.go).ToArray();
        }

        private void ClearHighlights()
        {
            SceneView.RepaintAll();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (rendererInfos == null || rendererInfos.Count == 0) return;

            Handles.BeginGUI();

            DrawSceneLegend(sceneView);

            Handles.EndGUI();

            DrawHighlights(sceneView);
        }

        private void DrawHighlights(SceneView sceneView)
        {
            var cam = sceneView.camera;

            foreach (var info in rendererInfos)
            {
                if (info.renderer == null || info.go == null) continue;
                if (!info.go.activeInHierarchy) continue;

                Color color = info.isCritical ? HIGHLIGHT_CRITICAL :
                              info.isProblematic ? HIGHLIGHT_WARNING :
                                                   HIGHLIGHT_OK;

                // Wireframe coloré sur les bounds
                Bounds b = info.renderer.bounds;

                // Dessin de la boîte de surbrillance
                DrawWireCube(b.center, b.size, color, info.isCritical ? 2.5f : 1.5f);

                // Label flottant uniquement pour les objets problématiques
                if (info.isProblematic)
                {
                    Vector3 labelPos = b.center + Vector3.up * (b.extents.y + 0.15f);
                    DrawSceneLabel(labelPos, cam, info);
                }
            }
        }

        private void DrawWireCube(Vector3 center, Vector3 size, Color color, float thickness)
        {
            Color prev = Handles.color;
            Handles.color = color;

            // 12 arêtes d'un cube
            Vector3 h = size * 0.5f;
            Vector3[] corners = new Vector3[8]
            {
            center + new Vector3(-h.x, -h.y, -h.z),
            center + new Vector3( h.x, -h.y, -h.z),
            center + new Vector3( h.x, -h.y,  h.z),
            center + new Vector3(-h.x, -h.y,  h.z),
            center + new Vector3(-h.x,  h.y, -h.z),
            center + new Vector3( h.x,  h.y, -h.z),
            center + new Vector3( h.x,  h.y,  h.z),
            center + new Vector3(-h.x,  h.y,  h.z),
            };

            int[][] edges = new int[][]
            {
            new[]{0,1}, new[]{1,2}, new[]{2,3}, new[]{3,0},
            new[]{4,5}, new[]{5,6}, new[]{6,7}, new[]{7,4},
            new[]{0,4}, new[]{1,5}, new[]{2,6}, new[]{3,7}
            };

            foreach (var e in edges)
                Handles.DrawLine(corners[e[0]], corners[e[1]], thickness);

            Handles.color = prev;
        }

        private void DrawSceneLabel(Vector3 worldPos, Camera cam, RendererInfo info)
        {
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            if (screenPos.z < 0) return;

            // Distance culling
            float dist = Vector3.Distance(cam.transform.position, worldPos);
            if (dist > 80f) return;

            float alpha = Mathf.Clamp01(1f - (dist - 30f) / 50f);

            Handles.BeginGUI();

            float x = screenPos.x - 45f;
            float y = cam.pixelHeight - screenPos.y - 14f;

            Color bg = info.isCritical ? new Color(0.7f, 0.05f, 0.05f, 0.85f * alpha) :
                                          new Color(0.65f, 0.4f, 0f, 0.80f * alpha);
            Color txt = new Color(1f, 1f, 1f, alpha);

            var bgRect = new Rect(x, y, 90f, 20f);
            EditorGUI.DrawRect(bgRect, bg);

            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            labelStyle.normal.textColor = txt;

            string icon = info.isCritical ? "⛔" : "⚠";
            GUI.Label(bgRect, $"{icon} {info.drawCalls} DC", labelStyle);

            Handles.EndGUI();
        }

        private void DrawSceneLegend(SceneView sceneView)
        {
            float pw = sceneView.position.width;

            var legendRect = new Rect(pw - 185f, 8f, 178f, 90f);
            EditorGUI.DrawRect(legendRect, new Color(0.1f, 0.1f, 0.1f, 0.82f));

            var border = legendRect;
            border.x -= 1; border.y -= 1;
            border.width += 2; border.height += 2;
            EditorGUI.DrawRect(border, new Color(0.4f, 0.4f, 0.4f, 0.5f));

            var titleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = Color.white;

            var itemStyle = new GUIStyle(EditorStyles.miniLabel);

            GUI.Label(new Rect(legendRect.x, legendRect.y + 4, legendRect.width, 16), "Draw Call Analyzer", titleStyle);

            DrawLegendItem(legendRect.x + 8, legendRect.y + 22, HIGHLIGHT_CRITICAL, "Critique (> seuil×2)", itemStyle);
            DrawLegendItem(legendRect.x + 8, legendRect.y + 38, HIGHLIGHT_WARNING, $"Attention (> {drawCallThreshold} DC)", itemStyle);
            DrawLegendItem(legendRect.x + 8, legendRect.y + 54, HIGHLIGHT_OK, "OK", itemStyle);

            var totalStyle = new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold };
            totalStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            GUI.Label(new Rect(legendRect.x + 8, legendRect.y + 70, legendRect.width, 16),
                      $"Total scène : {totalDrawCalls} draw calls", totalStyle);
        }

        private void DrawLegendItem(float x, float y, Color color, string label, GUIStyle style)
        {
            EditorGUI.DrawRect(new Rect(x, y + 3, 12, 10), color);
            style.normal.textColor = new Color(0.82f, 0.82f, 0.82f);
            GUI.Label(new Rect(x + 16, y, 150, 16), label, style);
        }
        #endregion
    }
}
