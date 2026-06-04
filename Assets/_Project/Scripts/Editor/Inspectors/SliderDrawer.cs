using GlimmerOfHope.Editor;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor
{
    /// <summary>
    /// PropertyDrawer pour SliderAttribute.
    /// Affiche un slider sous le champ natif d'Unity.
    /// </summary>
    [CustomPropertyDrawer(typeof(SliderAttribute))]
    public class SliderDrawer : PropertyDrawer
    {
        #region Constantes
        private const float _SLIDER_HEIGHT = 16f;
        private const float _SPACING = 3f;
        private const float _TRACK_HEIGHT = 6f;
        private const float _THUMB_SIZE = 12f;
        private const float _LABEL_WIDTH = 30f;
        #endregion

        #region PropertyDrawer
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight + _SPACING + _SLIDER_HEIGHT;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Float &&
                property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.HelpBox(position, "[Slider] : float ou int uniquement.", MessageType.Error);
                return;
            }

            SliderAttribute attr = attribute as SliderAttribute;
            if (attr == null)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            Rect fieldRect = new Rect(position.x, position.y,
                                       position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(fieldRect, property, label);

            if (property.propertyType == SerializedPropertyType.Float)
                property.floatValue = Mathf.Clamp(property.floatValue, attr.min, attr.max);
            else
                property.intValue = Mathf.RoundToInt(Mathf.Clamp(property.intValue, attr.min, attr.max));

            Rect sliderRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + _SPACING,
                                        position.width, _SLIDER_HEIGHT);
            DrawCustomSlider(sliderRect, property, attr);

            EditorGUI.EndProperty();
        }
        #endregion

        #region Draw
        private void DrawCustomSlider(Rect rect, SerializedProperty property, SliderAttribute attr)
        {
            float currentValue = property.propertyType == SerializedPropertyType.Float
                ? property.floatValue
                : (float)property.intValue;

            float t = Mathf.InverseLerp(attr.min, attr.max, currentValue);

            float indent = EditorGUI.indentLevel * 15f;
            float x = rect.x + indent;
            float width = rect.width - indent;

            Rect minRect = new Rect(x, rect.y, _LABEL_WIDTH, _SLIDER_HEIGHT);
            Rect maxRect = new Rect(x + width - _LABEL_WIDTH, rect.y, _LABEL_WIDTH, _SLIDER_HEIGHT);
            Rect trackZone = new Rect(x + _LABEL_WIDTH + 2f, rect.y,
                                       width - _LABEL_WIDTH * 2f - 4f, _SLIDER_HEIGHT);

            GUIStyle smallLabel = new GUIStyle(EditorStyles.miniLabel);

            smallLabel.alignment = TextAnchor.MiddleLeft;
            GUI.Label(minRect, FormatBound(attr.min, property), smallLabel);

            smallLabel.alignment = TextAnchor.MiddleRight;
            GUI.Label(maxRect, FormatBound(attr.max, property), smallLabel);

            float trackY = trackZone.y + (_SLIDER_HEIGHT - _TRACK_HEIGHT) * 0.5f;
            Rect track = new Rect(trackZone.x, trackY, trackZone.width, _TRACK_HEIGHT);
            EditorGUI.DrawRect(track, new Color(0.12f, 0.12f, 0.12f));

            float fillWidth = (track.width - 2f) * t;
            EditorGUI.DrawRect(new Rect(track.x + 1f, track.y + 1f, fillWidth, track.height - 2f),
                                attr.color);

            float thumbX = track.x + (track.width - _THUMB_SIZE) * t;
            float thumbY = trackY + (_TRACK_HEIGHT - _THUMB_SIZE) * 0.5f;
            EditorGUI.DrawRect(new Rect(thumbX, thumbY, _THUMB_SIZE, _THUMB_SIZE),
                                attr.color);

            Color lighter = Color.Lerp(attr.color, Color.white, 0.35f);
            EditorGUI.DrawRect(new Rect(thumbX + 2f, thumbY + 2f, _THUMB_SIZE - 4f, _THUMB_SIZE - 4f),
                                lighter);

            int id = GUIUtility.GetControlID(
                FocusType.Passive, new Rect(thumbX, thumbY, _THUMB_SIZE, _THUMB_SIZE));

            Event ev = Event.current;
            EventType evt = ev.GetTypeForControl(id);

            bool overTrack = track.Contains(ev.mousePosition);

            if (evt == EventType.MouseDown && overTrack)
            {
                GUIUtility.hotControl = id;
                ApplyMouseX(ev.mousePosition.x, track, property, attr);
                GUI.changed = true;
                ev.Use();
            }
            else if (evt == EventType.MouseDrag && GUIUtility.hotControl == id)
            {
                ApplyMouseX(ev.mousePosition.x, track, property, attr);
                GUI.changed = true;
                ev.Use();
            }
            else if (evt == EventType.MouseUp && GUIUtility.hotControl == id)
            {
                GUIUtility.hotControl = 0;
                ev.Use();
            }

            if (overTrack || GUIUtility.hotControl == id)
                EditorGUIUtility.AddCursorRect(track, MouseCursor.SlideArrow);
        }
        #endregion

        #region Helpers
        private void ApplyMouseX(float mouseX, Rect track,
                                  SerializedProperty property, SliderAttribute attr)
        {
            float t = Mathf.Clamp01((mouseX - track.x) / track.width);
            float value = Mathf.Lerp(attr.min, attr.max, t);

            if (property.propertyType == SerializedPropertyType.Float)
                property.floatValue = value;
            else
                property.intValue = Mathf.RoundToInt(value);
        }

        private string FormatBound(float v, SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.Integer)
                return ((int)v).ToString();

            if (v % 1f == 0f)
                return v.ToString("F0");

            return v.ToString("F1");
        }
        #endregion
    }
}
