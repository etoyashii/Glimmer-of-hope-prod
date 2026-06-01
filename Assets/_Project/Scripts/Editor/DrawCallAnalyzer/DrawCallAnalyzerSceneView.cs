using UnityEngine;
using UnityEditor;

namespace GlimmerOfHope.Editor
{
    public partial class DrawCallAnalyzer
    {
        #region Private Methods
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

                Color color = info.isCritical    ? HIGHLIGHT_CRITICAL :
                              info.isProblematic ? HIGHLIGHT_WARNING   :
                                                   HIGHLIGHT_OK;

                Bounds b = info.renderer.bounds;
                DrawWireCube(b.center, b.size, color, info.isCritical ? 2.5f : 1.5f);

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

            float dist = Vector3.Distance(cam.transform.position, worldPos);
            if (dist > 80f) return;

            float alpha = Mathf.Clamp01(1f - (dist - 30f) / 50f);

            Handles.BeginGUI();

            float x = screenPos.x - 45f;
            float y = cam.pixelHeight - screenPos.y - 14f;

            Color bg  = info.isCritical ? new Color(0.7f,  0.05f, 0.05f, 0.85f * alpha) :
                                          new Color(0.65f, 0.4f,  0f,    0.80f * alpha);

            var bgRect = new Rect(x, y, 90f, 20f);
            EditorGUI.DrawRect(bgRect, bg);

            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            labelStyle.normal.textColor = new Color(1f, 1f, 1f, alpha);

            string icon = info.isCritical ? "⛔" : "⚠";
            GUI.Label(bgRect, $"{icon} {info.drawCalls} DC", labelStyle);

            Handles.EndGUI();
        }

        private void DrawSceneLegend(SceneView sceneView)
        {
            float pw = sceneView.position.width;

            var legendRect = new Rect(pw - 185f, 8f, 178f, 90f);

            var border = legendRect;
            border.x -= 1; border.y -= 1;
            border.width += 2; border.height += 2;
            EditorGUI.DrawRect(border, new Color(0.4f, 0.4f, 0.4f, 0.5f));
            EditorGUI.DrawRect(legendRect, new Color(0.1f, 0.1f, 0.1f, 0.82f));

            var titleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = Color.white;

            var itemStyle = new GUIStyle(EditorStyles.miniLabel);

            GUI.Label(new Rect(legendRect.x, legendRect.y + 4, legendRect.width, 16), "Draw Call Analyzer", titleStyle);

            DrawLegendItem(legendRect.x + 8, legendRect.y + 22, HIGHLIGHT_CRITICAL, "Critique (> seuil×2)", itemStyle);
            DrawLegendItem(legendRect.x + 8, legendRect.y + 38, HIGHLIGHT_WARNING,  $"Attention (> {drawCallThreshold} DC)", itemStyle);
            DrawLegendItem(legendRect.x + 8, legendRect.y + 54, HIGHLIGHT_OK,       "OK", itemStyle);

            var totalStyle = new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold };
            totalStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            GUI.Label(
                new Rect(legendRect.x + 8, legendRect.y + 70, legendRect.width, 16),
                $"Total scène : {totalDrawCalls} draw calls",
                totalStyle);
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
