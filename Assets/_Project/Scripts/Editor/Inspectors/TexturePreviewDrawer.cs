using UnityEngine;
using UnityEditor;

namespace GlimmerOfHope.Editor
{
    /// <summary>
    /// PropertyDrawer pour TexturePreviewAttribute.
    /// Affiche une preview sous le champ Sprite, Texture2D et Material.
    /// </summary>
    [CustomPropertyDrawer(typeof(TexturePreviewAttribute))]
    public class TexturePreviewDrawer : PropertyDrawer  
    {
        #region Constantes
        private const float _MIN_PREVIEW_HEIGHT = 64f;    
        private const float _MAX_PREVIEW_HEIGHT = 512f;    
        private const float _HANDLE_HEIGHT = 8f;    
        private const float _SPACING = 4f;
        #endregion

        #region PropertyDrawer
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!HasPreview(property))
                return EditorGUIUtility.singleLineHeight;

            float previewHeight = EditorPrefs.GetFloat(PrefsKey(property), _MIN_PREVIEW_HEIGHT);
            return EditorGUIUtility.singleLineHeight + _SPACING + previewHeight + _HANDLE_HEIGHT;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.HelpBox(position, "[TexturePreview] : Sprite ou Texture2D uniquement.", MessageType.Error);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            Rect fieldRect = new Rect(position.x, position.y,
                position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(fieldRect, property, label);

            if (HasPreview(property))
            {
                float previewY = position.y + EditorGUIUtility.singleLineHeight + _SPACING;
                float previewH = EditorPrefs.GetFloat(PrefsKey(property), _MIN_PREVIEW_HEIGHT);

                Rect previewRect = new Rect(position.x, previewY, position.width, previewH);
                DrawPreview(previewRect, property);

                Rect handleRect = new Rect(position.x, previewY + previewH, position.width, _HANDLE_HEIGHT);
                //DrawResize
            }

            EditorGUI.EndProperty();
        }
        #endregion

        #region Draw
        private void DrawPreview(Rect rect, SerializedProperty property)
        {
            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));

            Texture2D tex = GetTexture(property);
            Material mat = GetMaterial(property);
            GUIStyle info_s = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.LowerRight,
                normal = { textColor = new Color(0.7f, 0.7f, 0.7f) }
            };

            if (tex != null)
            {
                Rect texRect = FitRect(rect, tex.width, tex.height);
                GUI.DrawTexture(texRect, tex, ScaleMode.ScaleToFit, true);

                string info = tex.width + "x" + tex.height;
                GUI.Label(new Rect(rect.x, rect.y, rect.width - 4f, rect.height - 2f), info, info_s);
            }
            else if (mat != null)
            {
                Color col = mat.HasProperty("_Color") ? mat.color : Color.grey;
                Rect colorRect = FitRect(rect, 1, 1);
                EditorGUI.DrawRect(colorRect, col);

                string info = "r" + Mathf.RoundToInt(col.r * 255) + " "
                    + "g" + Mathf.RoundToInt(col.g * 255) + " "
                    + "b" + Mathf.RoundToInt(col.b * 255);
                GUI.Label(new Rect(rect.x, rect.y, rect.width - 4f, rect.height - 2f), info, info_s);
            }
        }

        private void DrawResizeHandle(Rect rect, SerializedProperty property)
        {
            string key = PrefsKey(property);

            bool hover = rect.Contains(Event.current.mousePosition);
            EditorGUI.DrawRect(rect,
                hover ? new Color(0.28f, 0.65f, 1f, 0.5f) : new Color(0.25f, 0.25f, 0.25f));

            float cx = rect.x + rect.width * 0.5f;
            float cy = rect.y + rect.height * 0.5f;
            for (int i = -1; i <= 1; ++i)
            {
                EditorGUI.DrawRect(new Rect(cx - 14f, cy + i * 2.5f - 0.5f, 28f, 1f),
                    new Color(0.6f, 0.6f, 0.6f));
            }

            int id = GUIUtility.GetControlID(key.GetHashCode(), FocusType.Passive, rect);
            Event ev = Event.current;
            EventType evt = ev.GetTypeForControl(id);

            if (evt == EventType.MouseDown && rect.Contains(ev.mousePosition))
            {
                GUIUtility.hotControl = id;
                ev.Use();
            }
            else if (evt == EventType.MouseDrag && GUIUtility.hotControl == id)
            {
                float previewY = rect.y - EditorPrefs.GetFloat(key, _MIN_PREVIEW_HEIGHT);
                float newHeight = Mathf.Clamp(ev.mousePosition.y - previewY, _MIN_PREVIEW_HEIGHT, _MIN_PREVIEW_HEIGHT);

                EditorPrefs.SetFloat(key, newHeight);
                GUI.changed = true;
                ev.Use();
            }
            else if (evt == EventType.MouseUp && GUIUtility.hotControl == id)
            {
                GUIUtility.hotControl = 0;
                ev.Use();
            }

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeVertical);
        }
        #endregion

        #region Helpers
        private bool HasPreview(SerializedProperty property)
        {
            Object obj = property.objectReferenceValue;
            if (obj == null) return false;
            return obj is Texture2D || obj is Sprite || obj is Material;
        }

        private Texture2D GetTexture(SerializedProperty property)
        {
            Object obj = property.objectReferenceValue;
            if (obj == null) return null;

            if (obj is Texture2D tex) return tex;
            if (obj is Sprite sprite) return sprite.texture;
            if (obj is Material mat) return mat.mainTexture as Texture2D;

            return null;
        }

        private Material GetMaterial(SerializedProperty property)
        {
            return property.objectReferenceValue as Material;
        }

        private Rect FitRect(Rect container, int texW, int texH)
        {
            if (texW == 0 || texH == 0) return container;

            float ratio = (float)texW / texH;
            float maxW = container.width;
            float maxH = container.height;

            float w = maxW;
            float h = w / ratio;

            if (h > maxH)
            {
                h = maxH;
                w = h * ratio;
            }

            return new Rect(
                container.x + (maxW - w) * 0.5f,
                container.y + (maxH - h) * 0.5f,
                w, h );
        }

        private string PrefsKey(SerializedProperty property)
        {
            return "TexPreview_"
                + property.serializedObject.targetObject.GetInstanceID()
                + "_" + property.propertyPath;
        }
        #endregion
    }
}
