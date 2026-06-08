#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class EditorGUIHelper
{
    // Palette
    public static readonly Color ColorBackground = new Color(0.18f, 0.18f, 0.18f, 1f);
    public static readonly Color ColorSurface = new Color(0.24f, 0.24f, 0.24f, 1f);
    public static readonly Color ColorBorder = new Color(0.10f, 0.10f, 0.10f, 1f);
    public static readonly Color ColorAccent = new Color(0.28f, 0.65f, 1.00f, 1f);
    public static readonly Color ColorAccentDim = new Color(0.28f, 0.65f, 1.00f, 0.35f);
    public static readonly Color ColorWaveform = new Color(0.28f, 0.65f, 1.00f, 0.85f);
    public static readonly Color ColorWaveformTrim = new Color(0.00f, 0.00f, 0.00f, 0.55f);
    public static readonly Color ColorHandleHover = new Color(1.00f, 0.75f, 0.20f, 1f);
    public static readonly Color ColorText = new Color(0.88f, 0.88f, 0.88f, 1f);
    public static readonly Color ColorTextDim = new Color(0.55f, 0.55f, 0.55f, 1f);

    // Constantes layout
    public const float BorderRadius = 4f;
    public const float Padding = 6f;
    public const float HandleSize = 6f;

    // Rects
    public static Rect Inset(Rect rect, float amount) =>
        new Rect(rect.x + amount, rect.y + amount,
                 rect.width - amount * 2, rect.height - amount * 2);

    public static Rect SliceTop(ref Rect rect, float height)
    {
        var slice = new Rect(rect.x, rect.y, rect.width, height);
        rect.y += height;
        rect.height -= height;
        return slice;
    }

    public static Rect SliceBottom(ref Rect rect, float height)
    {
        rect.height -= height;
        return new Rect(rect.x, rect.y + rect.height, rect.width, height);
    }

    // Draw
    public static void DrawBox(Rect rect, Color fill, Color border)
    {
        EditorGUI.DrawRect(rect, border);
        EditorGUI.DrawRect(Inset(rect, 1f), fill);
    }

    public static void DrawSeparator(Rect rect)
    {
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), ColorBorder);
    }

    public static void DrawLabel(Rect rect, string text, int fontSize = 10,
                                  bool bold = false, TextAnchor anchor = TextAnchor.MiddleLeft,
                                  Color? color = null)
    {
        var style = new GUIStyle(EditorStyles.label)
        {
            fontSize = fontSize,
            fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
            alignment = anchor,
            normal = { textColor = color ?? ColorText }
        };
        GUI.Label(rect, text, style);
    }

    // Handle resize
    public static float DrawResizeHandle(Rect rect, string prefsKey,
                                          float minHeight = 64f, float maxHeight = 512f)
    {
        float currentHeight = EditorPrefs.GetFloat(prefsKey, minHeight);

        var handleRect = new Rect(rect.x, rect.y + currentHeight - 4f, rect.width, 8f);

        bool isHover = handleRect.Contains(Event.current.mousePosition);
        EditorGUI.DrawRect(handleRect,
            isHover ? ColorHandleHover : new Color(0.35f, 0.35f, 0.35f, 0.6f));

        var center = new Vector2(handleRect.x + handleRect.width * 0.5f,
                                  handleRect.y + handleRect.height * 0.5f);
        for (int i = -1; i <= 1; i++)
        {
            EditorGUI.DrawRect(
                new Rect(center.x - 16f, center.y + i * 2.5f - 0.5f, 32f, 1f),
                ColorTextDim);
        }

        // Drag
        int controlId = GUIUtility.GetControlID(prefsKey.GetHashCode(), FocusType.Passive, handleRect);
        var ev = Event.current;

        switch (ev.GetTypeForControl(controlId))
        {
            case EventType.MouseDown when handleRect.Contains(ev.mousePosition):
                GUIUtility.hotControl = controlId;
                ev.Use();
                break;

            case EventType.MouseDrag when GUIUtility.hotControl == controlId:
                currentHeight = Mathf.Clamp(
                    ev.mousePosition.y - rect.y,
                    minHeight, maxHeight);
                EditorPrefs.SetFloat(prefsKey, currentHeight);
                GUI.changed = true;
                ev.Use();
                break;

            case EventType.MouseUp when GUIUtility.hotControl == controlId:
                GUIUtility.hotControl = 0;
                ev.Use();
                break;
        }

        // Curseur
        EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeVertical);

        return currentHeight;
    }

    // Boutons
    public static bool DrawButton(Rect rect, string label, bool active = false)
    {
        bool hover = rect.Contains(Event.current.mousePosition);
        Color bg = active ? ColorAccent : (hover ? ColorSurface : ColorBackground);
        Color border = active ? ColorAccent : (hover ? ColorAccent : ColorBorder);
        Color text = active ? Color.black : ColorText;

        DrawBox(rect, bg, border);
        DrawLabel(rect, label, 10, true, TextAnchor.MiddleCenter, text);

        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            Event.current.Use();
            return true;
        }
        return false;
    }
}
#endif