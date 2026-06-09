using UnityEngine;
using UnityEditor;
using System.Linq;

namespace GlimmerOfHope.Editor
{
    /// <summary>
    /// Editor window for analyzing and visualizing draw calls in the scene, helping to identify performance bottlenecks.
    /// </summary>
    public partial class DrawCallAnalyzer : EditorWindow
    {
        [MenuItem("Tools/GlimmerOfHope/Draw Call Analyzer %#d")]

        #region Public Methods
        /// <summary>
        /// Opens the Draw Call Analyzer window.
        /// </summary>
        public static void OpenWindow()
        {
            var window = GetWindow<DrawCallAnalyzer>(WINDOW_TITLE);
            window.minSize = new Vector2(420, 340);
            window.Show();
        }
        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            drawCallThreshold = EditorPrefs.GetInt(PREFS_KEY, DEFAULT_THRESH);
            SceneView.duringSceneGui += OnSceneGUI;
            RefreshData();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.RepaintAll();
        }

        private void OnDestroy()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.RepaintAll();
        }

        private void OnFocus()
        {
            RefreshData();
        }

        private void Update()
        {
            // Auto-refresh data at regular intervals if enabled
            if (autoRefresh && EditorApplication.timeSinceStartup - lastRefreshTime > AUTO_REFRESH_INTERVAL)
            {
                RefreshData();
                Repaint();
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Scans the scene for all renderers, calculates their draw calls, and updates the lists of renderer info and problematic objects.
        /// </summary>
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

                int dc = CalculateDrawCalls(r);
                int mats = r.sharedMaterials.Length;
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

            rendererInfos = rendererInfos.OrderByDescending(x => x.drawCalls).ToList();
            problematicObjects = problematicObjects.OrderByDescending(x => x.drawCalls).ToList();

            SceneView.RepaintAll();
        }

        /// <summary>
        /// Calculates the number of draw calls for a given renderer, based on its mesh and materials.
        /// </summary>
        private int CalculateDrawCalls(Renderer r)
        {
            int matCount = r.sharedMaterials.Count(m => m != null);

            if (r is MeshRenderer mr)
            {
                var mf = mr.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                    return Mathf.Max(mf.sharedMesh.subMeshCount, matCount);
            }
            else if (r is SkinnedMeshRenderer smr)
            {
                if (smr.sharedMesh != null)
                    return Mathf.Max(smr.sharedMesh.subMeshCount, matCount);
            }

            return Mathf.Max(1, matCount);
        }

        private void SelectAllProblematic()
        {
            Selection.objects = problematicObjects
                .Select(x => (UnityEngine.Object)x.go)
                .ToArray();
        }
        #endregion
    }
}