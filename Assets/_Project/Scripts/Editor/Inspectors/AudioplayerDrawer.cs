using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor
{
    [CustomPropertyDrawer(typeof(AudioPlayerAttribute))]
    public class AudioplayerDrawer : PropertyDrawer
    {
        private const float Spacing = 4f;
        private const float WaveformDefault = 80f;
        private const float WaveformMin = 40f;
        private const float WaveformMax = 200f;
        private const float HandleHeight = 8f;
        private const float RowHeight = 22f;
        private const float BtnWidth = 48f;
        private const float MarkerWidth = 2f;

        private float ExtraHeight => HandleHeight + RowHeight + RowHeight + RowHeight + RowHeight + 6f;

        private static bool _updateRegistered = false;

        private static System.Collections.Generic.Dictionary<string, int> _prevClipID
            = new System.Collections.Generic.Dictionary<string, int>();

        private static System.Type _audioUtil;
        private static System.Type AudioUtil
        {
            get
            {
                if (_audioUtil == null)
                    _audioUtil = System.Type.GetType("UnityEditor.AudioUtil,UnityEditor");
                return _audioUtil;
            }
        }

        private static void PlayClip(AudioClip clip, int startSample)
        {
            var m = AudioUtil?.GetMethod("PlayPreviewClip",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                null, new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            m?.Invoke(null, new object[] { clip, startSample, false });
        }

        private static void StopClip()
        {
            var m = AudioUtil?.GetMethod("StopAllPreviewClips",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            m?.Invoke(null, null);
        }

        private static bool IsPlayingClip()
        {
            var m = AudioUtil?.GetMethod("IsPreviewClipPlaying",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            return m != null && (bool)m.Invoke(null, null);
        }

        private static float GetClipPosition()
        {
            var m = AudioUtil?.GetMethod("GetPreviewClipPosition",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            return m != null ? (float)m.Invoke(null, null) : 0f;
        }

        private static void SeekClip(AudioClip clip, float normalizedTime)
        {
            int sample = Mathf.RoundToInt(normalizedTime * clip.samples);
            var m = AudioUtil?.GetMethod("SetPreviewClipSamplePosition",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                null, new System.Type[] { typeof(AudioClip), typeof(int) }, null);
            m?.Invoke(null, new object[] { clip, sample });
        }

        private static void EnsureUpdate()
        {
            if (_updateRegistered) return;
            EditorApplication.update += OnEditorUpdate;
            _updateRegistered = true;
        }

        private static void OnEditorUpdate()
        {
            if (IsPlayingClip())
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.objectReferenceValue == null)
                return EditorGUIUtility.singleLineHeight;

            float waveH = EditorPrefs.GetFloat(WaveKey(property), WaveformDefault);
            return EditorGUIUtility.singleLineHeight + Spacing + waveH + ExtraHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.HelpBox(position, "[AudioPlayer] : AudioClip uniquement.", MessageType.Error);
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            Rect fieldRect = new Rect(position.x, position.y,
                                       position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(fieldRect, property, label);

            AudioClip clip = property.objectReferenceValue as AudioClip;

            if (clip != null)
            {
                EnsureUpdate();

                string path = property.propertyPath;
                int clipID = clip.GetInstanceID();
                if (_prevClipID.TryGetValue(path, out int prev) && prev != clipID)
                    WaveformRenderer.Invalidate(prev);
                _prevClipID[path] = clipID;

                float waveH = EditorPrefs.GetFloat(WaveKey(property), WaveformDefault);
                float y = position.y + EditorGUIUtility.singleLineHeight + Spacing;

                Rect waveRect = new Rect(position.x, y, position.width, waveH);
                DrawWaveform(waveRect, property, clip);
                y += waveH;

                Rect handleRect = new Rect(position.x, y, position.width, HandleHeight);
                DrawResizeHandle(handleRect, property);
                y += HandleHeight;

                Rect ctrlRect = new Rect(position.x, y, position.width, RowHeight);
                DrawControls(ctrlRect, property, clip);
                y += RowHeight;

                Rect volRect = new Rect(position.x, y, position.width, RowHeight);
                DrawVolumeSlider(volRect, property);
                y += RowHeight;

                Rect pitchRect = new Rect(position.x, y, position.width, RowHeight);
                DrawPitchSlider(pitchRect, property);
                y += RowHeight;

                Rect trimRect = new Rect(position.x, y, position.width, RowHeight);
                DrawTrimInfo(trimRect, property, clip);
            }

            EditorGUI.EndProperty();
        }

        //Waveform
        private void DrawWaveform(Rect rect, SerializedProperty property, AudioClip clip)
        {
            int w = Mathf.Max(1, (int)rect.width);
            int h = Mathf.Max(1, (int)rect.height);
            Texture2D wave = WaveformRenderer.Get(clip, w, h);

            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));

            if (WaveformRenderer.IsStreaming(clip))
            {
                DrawWarning(rect, "Waveform indisponible : clip en mode Streaming.\n" +
                                   "Changez Load Type en Decompress On Load.");
            }
            else if (WaveformRenderer.IsPreloadDisabled(clip))
            {
                DrawWarning(rect, "Waveform indisponible : cochez \"Preload Audio Data\" " +
                                   "dans l'Inspector du clip, puis Apply.");
            }
            else if (wave != null)
            {
                GUI.DrawTexture(rect, wave);
            }
            else
            {
                DrawWarning(rect, "Waveform indisponible (GetData a échoué).");
            }

            //Overlay trim
            float tStart = GetTrimStart(property);
            float tEnd = GetTrimEnd(property);
            float leftW = tStart * rect.width;
            float rightX = rect.x + tEnd * rect.width;
            float rightW = (1f - tEnd) * rect.width;
            Color overlay = new Color(0f, 0f, 0f, 0.55f);

            if (leftW > 0) EditorGUI.DrawRect(new Rect(rect.x, rect.y, leftW, rect.height), overlay);
            if (rightW > 0) EditorGUI.DrawRect(new Rect(rightX, rect.y, rightW, rect.height), overlay);

            //Marqueurs trim jaunes
            float startX = rect.x + tStart * rect.width;
            float endX = rect.x + tEnd * rect.width;
            Color markerColor = new Color(1f, 0.85f, 0.2f);
            EditorGUI.DrawRect(new Rect(startX - MarkerWidth * 0.5f, rect.y, MarkerWidth, rect.height), markerColor);
            EditorGUI.DrawRect(new Rect(endX - MarkerWidth * 0.5f, rect.y, MarkerWidth, rect.height), markerColor);

            //Ligne volume
            float vol = EditorPrefs.GetFloat(VolKey(property), 1f);
            float volY = rect.yMax - vol * rect.height;
            EditorGUI.DrawRect(new Rect(rect.x, volY, rect.width, 1f), new Color(1f, 1f, 1f, 0.4f));

            //Barre de progression
            if (IsPlayingClip() && clip.length > 0)
            {
                float progress = Mathf.Clamp01(GetClipPosition() / clip.length);
                float progX = rect.x + progress * rect.width;
                EditorGUI.DrawRect(new Rect(progX - 1f, rect.y, 2f, rect.height),
                                    new Color(1f, 1f, 1f, 0.9f));
            }

            HandleWaveformInput(rect, property, clip);
        }

        private enum DragMode { None, TrimStart, TrimEnd, Seek }
        private static DragMode _dragMode = DragMode.None;

        private void HandleWaveformInput(Rect rect, SerializedProperty property, AudioClip clip)
        {
            Event ev = Event.current;
            float t = Mathf.Clamp01((ev.mousePosition.x - rect.x) / rect.width);
            bool over = rect.Contains(ev.mousePosition);

            float startX = rect.x + GetTrimStart(property) * rect.width;
            float endX = rect.x + GetTrimEnd(property) * rect.width;
            float zone = 6f;
            bool nearS = Mathf.Abs(ev.mousePosition.x - startX) < zone;
            bool nearE = Mathf.Abs(ev.mousePosition.x - endX) < zone;

            if (over)
                EditorGUIUtility.AddCursorRect(rect,
                    (nearS || nearE) ? MouseCursor.ResizeHorizontal : MouseCursor.ArrowPlus);

            int id = GUIUtility.GetControlID("WaveInput".GetHashCode(), FocusType.Passive, rect);

            if (ev.GetTypeForControl(id) == EventType.MouseDown && over)
            {
                GUIUtility.hotControl = id;
                _dragMode = nearS ? DragMode.TrimStart : nearE ? DragMode.TrimEnd : DragMode.Seek;
                ev.Use();
            }
            else if (ev.GetTypeForControl(id) == EventType.MouseDrag && GUIUtility.hotControl == id)
            {
                if (_dragMode == DragMode.TrimStart)
                    SetTrimStart(property, Mathf.Clamp(t, 0f, GetTrimEnd(property) - 0.01f));
                else if (_dragMode == DragMode.TrimEnd)
                    SetTrimEnd(property, Mathf.Clamp(t, GetTrimStart(property) + 0.01f, 1f));
                else if (_dragMode == DragMode.Seek && IsPlayingClip())
                    SeekClip(clip, t);

                GUI.changed = true;
                ev.Use();
            }
            else if (ev.GetTypeForControl(id) == EventType.MouseUp && GUIUtility.hotControl == id)
            {
                GUIUtility.hotControl = 0;
                _dragMode = DragMode.None;
                ev.Use();
            }
        }

        //Controls
        private void DrawControls(Rect rect, SerializedProperty property, AudioClip clip)
        {
            float x = rect.x;

            Rect playRect = new Rect(x, rect.y + 2f, BtnWidth, RowHeight - 4f);
            if (DrawButton(playRect, "Play", IsPlayingClip()))
            {
                int startSample = Mathf.RoundToInt(GetTrimStart(property) * clip.samples);
                EditorApplication.delayCall += () => PlayClip(clip, startSample);
            }

            x += BtnWidth + 4f;

            Rect stopRect = new Rect(x, rect.y + 2f, BtnWidth, RowHeight - 4f);
            if (DrawButton(stopRect, "Stop", false))
                StopClip();

            x += BtnWidth + 8f;

            string timeStr = FormatTime(IsPlayingClip() ? GetClipPosition() : 0f)
                           + " / " + FormatTime(clip.length);
            GUI.Label(new Rect(x, rect.y, rect.width - x + rect.x, RowHeight), timeStr,
                new GUIStyle(EditorStyles.miniLabel)
                { normal = { textColor = new Color(0.7f, 0.7f, 0.7f) } });
        }

        private void DrawVolumeSlider(Rect rect, SerializedProperty property)
        {
            float vol = EditorPrefs.GetFloat(VolKey(property), 1f);
            float newVol = DrawLabelSlider(rect, "Volume", vol, 0f, 1f);
            if (!Mathf.Approximately(newVol, vol))
                EditorPrefs.SetFloat(VolKey(property), newVol);
        }

        private void DrawPitchSlider(Rect rect, SerializedProperty property)
        {
            float pitch = EditorPrefs.GetFloat(PitchKey(property), 1f);
            float newPitch = DrawLabelSlider(rect, "Pitch", pitch, 0.1f, 3f);
            if (!Mathf.Approximately(newPitch, pitch))
                EditorPrefs.SetFloat(PitchKey(property), newPitch);
        }

        private float DrawLabelSlider(Rect rect, string labelText, float value, float min, float max)
        {
            float labelW = 46f;
            float valW = 34f;

            GUI.Label(new Rect(rect.x, rect.y, labelW, rect.height),
                labelText, new GUIStyle(EditorStyles.miniLabel));

            Rect sliderRect = new Rect(rect.x + labelW,
                rect.y + (rect.height - 14f) * 0.5f,
                rect.width - labelW - valW - 4f, 14f);
            float newVal = GUI.HorizontalSlider(sliderRect, value, min, max);

            GUI.Label(new Rect(rect.xMax - valW, rect.y, valW, rect.height),
                value.ToString("F2"),
                new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight });

            return newVal;
        }

        private void DrawTrimInfo(Rect rect, SerializedProperty property, AudioClip clip)
        {
            float tStart = GetTrimStart(property) * clip.length;
            float tEnd = GetTrimEnd(property) * clip.length;
            string txt = "Trim In: " + FormatTime(tStart)
                         + "  Out: " + FormatTime(tEnd)
                         + "  (" + FormatTime(tEnd - tStart) + ")";

            GUI.Label(rect, txt, new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(1f, 0.85f, 0.2f) } });
        }

        private void DrawWarning(Rect rect, string msg)
        {
            GUI.Label(rect, "! " + msg, new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.75f, 0.2f) }
            });
        }

        private void DrawResizeHandle(Rect rect, SerializedProperty property)
        {
            string key = WaveKey(property);
            bool hover = rect.Contains(Event.current.mousePosition);

            EditorGUI.DrawRect(rect, hover
                ? new Color(0.28f, 0.65f, 1f, 0.5f)
                : new Color(0.22f, 0.22f, 0.22f));

            float cx = rect.x + rect.width * 0.5f;
            float cy = rect.y + rect.height * 0.5f;
            for (int i = -1; i <= 1; i++)
                EditorGUI.DrawRect(new Rect(cx - 14f, cy + i * 2.5f - 0.5f, 28f, 1f),
                                    new Color(0.55f, 0.55f, 0.55f));

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
                float waveH = EditorPrefs.GetFloat(key, WaveformDefault);
                float newH = Mathf.Clamp(waveH + ev.delta.y, WaveformMin, WaveformMax);
                EditorPrefs.SetFloat(key, newH);
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

        private bool DrawButton(Rect rect, string label, bool active)
        {
            Color bg = active ? new Color(0.28f, 0.65f, 1f) : new Color(0.22f, 0.22f, 0.22f);
            Color border = active ? new Color(0.28f, 0.65f, 1f) : new Color(0.12f, 0.12f, 0.12f);
            Color text = active ? Color.black : new Color(0.88f, 0.88f, 0.88f);

            EditorGUI.DrawRect(rect, border);
            EditorGUI.DrawRect(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2), bg);
            GUI.Label(rect, label, new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = text }
            });

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                return true;
            }
            return false;
        }

        private float GetTrimStart(SerializedProperty p) =>
            EditorPrefs.GetFloat(TrimKey(p, "S"), 0f);

        private float GetTrimEnd(SerializedProperty p) =>
            EditorPrefs.GetFloat(TrimKey(p, "E"), 1f);

        private void SetTrimStart(SerializedProperty p, float v) =>
            EditorPrefs.SetFloat(TrimKey(p, "S"), v);

        private void SetTrimEnd(SerializedProperty p, float v) =>
            EditorPrefs.SetFloat(TrimKey(p, "E"), v);

        private string FormatTime(float s)
        {
            int m = (int)(s / 60f);
            return m + ":" + (s % 60f).ToString("00.00");
        }

        private string WaveKey(SerializedProperty p) => "Wave_" + p.serializedObject.targetObject.GetInstanceID() + "_" + p.propertyPath;
        private string VolKey(SerializedProperty p) => "Vol_" + p.serializedObject.targetObject.GetInstanceID() + "_" + p.propertyPath;
        private string PitchKey(SerializedProperty p) => "Pitch_" + p.serializedObject.targetObject.GetInstanceID() + "_" + p.propertyPath;
        private string TrimKey(SerializedProperty p, string side) => "Trim" + side + "_" + p.serializedObject.targetObject.GetInstanceID() + "_" + p.propertyPath;
    }
}