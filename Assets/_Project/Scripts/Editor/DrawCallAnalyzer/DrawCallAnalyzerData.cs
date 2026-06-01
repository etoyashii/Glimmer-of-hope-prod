using UnityEngine;
using System.Collections.Generic;

namespace GlimmerOfHope.Editor
{
    public partial class DrawCallAnalyzer
    {
        #region Constants
        private const string WINDOW_TITLE = "Draw Call Analyzer";
        private const string PREFS_KEY = "DrawCallAnalyzer_Threshold";
        private const int DEFAULT_THRESH = 3;
        private const double AUTO_REFRESH_INTERVAL = 1.0;
        #endregion

        private struct RendererInfo
        {
            #region Public Properties

            public Renderer   renderer;
            public GameObject go;
            public string     name;
            public int        drawCalls;
            public int        materialCount;
            public bool       isProblematic;
            public bool       isCritical;
            #endregion
        }
        #region Private Fields

        private static readonly Color HIGHLIGHT_CRITICAL = new Color(1f, 0.15f, 0.15f, 0.55f);
        private static readonly Color HIGHLIGHT_WARNING  = new Color(1f, 0.65f, 0f,    0.40f);
        private static readonly Color HIGHLIGHT_OK       = new Color(0.2f, 0.9f, 0.4f, 0.20f);

        private List<RendererInfo> rendererInfos      = new List<RendererInfo>();
        private List<RendererInfo> problematicObjects  = new List<RendererInfo>();

        private int    drawCallThreshold   = DEFAULT_THRESH;
        private int    totalDrawCalls      = 0;
        private bool   showOnlyProblematic = false;
        private bool   autoRefresh         = true;
        private Vector2 scrollPos;
        private double  lastRefreshTime;

        private GUIStyle headerStyle;
        private GUIStyle statBoxStyle;
        private GUIStyle warningRowStyle;
        private GUIStyle normalRowStyle;
        private GUIStyle badgeRedStyle;
        private GUIStyle badgeGreenStyle;
        private GUIStyle badgeOrangeStyle;
        private bool     stylesInitialized = false;

        #endregion

    }
}
