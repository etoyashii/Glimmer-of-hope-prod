using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor
{
    /// <summary>
    /// PropertyDrawer pour AudioPlayerAttribute.
    /// Affiche une Waveform interactive avec contrôles Play/Stop.
    /// Lecture des paramètres de l'AudioSource du GameObject.
    /// </summary>
    [CustomPropertyDrawer(typeof(AudioPlayerAttribute))]
    public class AudioplayerDrawer : PropertyDrawer
    {
        #region Constantes Layout
        private const float _SPACING = 4f;
        private const float _WAVEFORM_DEFAULT = 80f;
        private const float _WAVEFORM_MIN = 40f;
        private const float _WAVEFORM_MAX = 200f;
        private const float _HANDLE_HEIGHT = 8f;
        private const float _ROW_HEIGHT = 22f;
        private const float _BTN_WIDTH = 48f;
        private const float _MARKER_WIDTH = 2f;

        //                               Handle     +   Controls  +   ASInfo    +    Trim     + padding
        private float ExtraHeight => _HANDLE_HEIGHT + _ROW_HEIGHT + _ROW_HEIGHT + _ROW_HEIGHT + 6f;
        #endregion

        #region État statique
        private static bool _updateRegistered = false;
        private static AudioSource _tempSource = null;
        private static AudioSource _activeSource = null;

        private static System.Collections.Generic.Dictionary<string, int> _prevClipID
            = new System.Collections.Generic.Dictionary<string, int>();
        #endregion

        #region AudioUtil
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

        private static void PlayClip(AudioClip clip, int startSample, AudioSource playModeSrc)
        {
            if (Application.isPlaying)
            {
                _activeSource = playModeSrc;
                _activeSource.clip = clip;
                _activeSource.timeSamples = startSample;
                _activeSource.Play();
            }
            else
            {
                var m = AudioUtil?.GetMethod("PlayPreviewClip",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                    null, new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
                m?.Invoke(null, new object[] { clip, startSample, false });
            }
        }

        private static void StopClip()
        {
            if (Application.isPlaying)
            {
                if (_activeSource != null) _activeSource.Stop();
            }
            else
            {
                var m = AudioUtil?.GetMethod("StopAllPreviewClips",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                m?.Invoke(null, null);
            }
        }

        private static bool IsPlayingClip()
        {
            if (Application.isPlaying)
                return _activeSource != null && _activeSource.isPlaying;

            var m = AudioUtil?.GetMethod("IsPreviewClipPlaying",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            return m != null && (bool)m.Invoke(null, null);
        }

        private static float GetClipPosition()
        {
            if (Application.isPlaying)
                return _activeSource != null ? _activeSource.time : 0f;

            var m = AudioUtil?.GetMethod("GetPreviewClipPosition",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            return m != null ? (float)m.Invoke(null, null) : 0f;
        }

        private static void SeekClip(AudioClip clip, float normalizedTime)
        {
            if (Application.isPlaying)
            {
                if (_activeSource != null)
                    _activeSource.time = normalizedTime * clip.length;
                return;
            }

            int sample = Mathf.RoundToInt(normalizedTime * clip.samples);
            var m = AudioUtil?.GetMethod("SetPreviewClipSamplePosition",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                null, new System.Type[] { typeof(AudioClip), typeof(int) }, null);
            m?.Invoke(null, new object[] { clip, sample });
        }
        #endregion

        #region Editor Update
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
        #endregion

        #region AudioSource Helper
        private static AudioSource GetAudioSource(SerializedProperty property)
        {
            var mono = property.serializedObject.targetObject as MonoBehaviour;
            return mono != null ? mono.GetComponent<AudioSource>() : null;
        }

        private static AudioSource GetOrCreatePlayModeSource(SerializedProperty property)
        {
            AudioSource src = GetAudioSource(property);
            if (src != null) return src;

            if (_tempSource == null || _tempSource.gameObject == null)
            {
                var go = new GameObject("__AudioPlayerPreview")
                { hideFlags = HideFlags.HideAndDontSave };
                _tempSource = go.AddComponent<AudioSource>();
                _tempSource.playOnAwake = false;
            }
            return _tempSource;
        }
        #endregion

        #region PropertyDrawer
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.objectReferenceValue == null)
                return EditorGUIUtility.singleLineHeight;

            float waveH = EditorPrefs.GetFloat(WaveKey(property), _WAVEFORM_DEFAULT);
            return EditorGUIUtility.singleLineHeight + _SPACING + waveH + ExtraHeight;
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

                float waveH = EditorPrefs.GetFloat(WaveKey(property), _WAVEFORM_DEFAULT);
                float y = position.y + EditorGUIUtility.singleLineHeight + _SPACING;

                Rect waveRect = new Rect(position.x, y, position.width, waveH);
                DrawWaveform(waveRect, property, clip);
                y += waveH;

                Rect handleRect = new Rect(position.x, y, position.width, _HANDLE_HEIGHT);
                DrawResizeHandle(handleRect, property);
                y += _HANDLE_HEIGHT;

                Rect ctrlRect = new Rect(position.x, y, position.width, _ROW_HEIGHT);
                DrawControls(ctrlRect, property, clip);
                y += _ROW_HEIGHT;

                Rect srcRect = new Rect(position.x, y, position.width, _ROW_HEIGHT);
                DrawAudioSourceInfo(srcRect, property);
                y += _ROW_HEIGHT;

                Rect trimRect = new Rect(position.x, y, position.width, _ROW_HEIGHT);
                DrawTrimInfo(trimRect, property, clip);
            }

            EditorGUI.EndProperty();
        }
        #endregion

        #region Draw Waveform
        private void DrawWaveform(Rect rect, SerializedProperty property, AudioClip clip)
        {
            int w = Mathf.Max(1, (int)rect.width);
            int h = Mathf.Max(1, (int)rect.height);
            Texture2D wave = WaveformRenderer.Get(clip, w, h);

            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));

            if (WaveformRenderer.IsStreaming(clip))
                DrawWarning(rect, "Waveform indisponible : clip en mode Streaming.\n" +
                                   "Changez Load Type en Decompress On Load.");
            else if (WaveformRenderer.IsPreloadDisabled(clip))
                DrawWarning(rect, "Waveform indisponible : cochez \"Preload Audio Data\" " +
                                   "dans l'Inspector du clip, puis Apply.");
            else if (wave != null)
                GUI.DrawTexture(rect, wave);
            else
                DrawWarning(rect, "Waveform indisponible (GetData a échoué).");

            //Overlay trim
            float tStart = GetTrimStart(property);
            float tEnd = GetTrimEnd(property);
            Color overlay = new Color(0f, 0f, 0f, 0.55f);

            float leftW = tStart * rect.width;
            float rightX = rect.x + tEnd * rect.width;
            float rightW = (1f - tEnd) * rect.width;
            if (leftW > 0) EditorGUI.DrawRect(new Rect(rect.x, rect.y, leftW, rect.height), overlay);
            if (rightW > 0) EditorGUI.DrawRect(new Rect(rightX, rect.y, rightW, rect.height), overlay);

            //Marqueurs trim jaunes
            Color markerColor = new Color(1f, 0.85f, 0.2f);
            float startX = rect.x + tStart * rect.width;
            float endX = rect.x + tEnd * rect.width;
            EditorGUI.DrawRect(new Rect(startX - _MARKER_WIDTH * 0.5f, rect.y, _MARKER_WIDTH, rect.height), markerColor);
            EditorGUI.DrawRect(new Rect(endX - _MARKER_WIDTH * 0.5f, rect.y, _MARKER_WIDTH, rect.height), markerColor);

            //Ligne volume lue depuis l'AudioSource
            AudioSource src = GetAudioSource(property);
            float vol = src != null ? src.volume : 1f;
            float volY = rect.yMax - vol * rect.height;
            EditorGUI.DrawRect(new Rect(rect.x, volY, rect.width, 1f), new Color(1f, 1f, 1f, 0.4f));

            //Barre de progression
            if (IsPlayingClip() && clip.length > 0)
            {
                float progress = Mathf.Clamp01(GetClipPosition() / clip.length);
                EditorGUI.DrawRect(new Rect(rect.x + progress * rect.width - 1f, rect.y, 2f, rect.height),
                                    new Color(1f, 1f, 1f, 0.9f));
            }

            HandleWaveformInput(rect, property, clip);
        }
        #endregion

        #region Input Waveform
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
                if (_dragMode == DragMode.TrimStart) SetTrimStart(property, Mathf.Clamp(t, 0f, GetTrimEnd(property) - 0.01f));
                else if (_dragMode == DragMode.TrimEnd) SetTrimEnd(property, Mathf.Clamp(t, GetTrimStart(property) + 0.01f, 1f));
                else if (_dragMode == DragMode.Seek && IsPlayingClip()) SeekClip(clip, t);
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
        #endregion

        #region Draw Controls
        private void DrawControls(Rect rect, SerializedProperty property, AudioClip clip)
        {
            float x = rect.x;

            Rect playRect = new Rect(x, rect.y + 2f, _BTN_WIDTH, _ROW_HEIGHT - 4f);
            if (DrawButton(playRect, "Play", IsPlayingClip()))
            {
                int startSample = Mathf.RoundToInt(GetTrimStart(property) * clip.samples);
                AudioSource srcToUse = Application.isPlaying ? GetOrCreatePlayModeSource(property) : null;
                EditorApplication.delayCall += () =>
                {
                    StopClip();
                    PlayClip(clip, startSample, srcToUse);
                };
            }
            x += _BTN_WIDTH + 4f;

            // Stop
            Rect stopRect = new Rect(x, rect.y + 2f, _BTN_WIDTH, _ROW_HEIGHT - 4f);
            if (DrawButton(stopRect, "Stop", false))
                StopClip();
            x += _BTN_WIDTH + 8f;

            // Temps
            string timeStr = FormatTime(IsPlayingClip() ? GetClipPosition() : 0f)
                           + " / " + FormatTime(clip.length);
            GUI.Label(new Rect(x, rect.y, rect.width - x + rect.x, _ROW_HEIGHT), timeStr,
                new GUIStyle(EditorStyles.miniLabel)
                { normal = { textColor = new Color(0.7f, 0.7f, 0.7f) } });
        }
        #endregion

        #region Draw AudioSource Info
        private void DrawAudioSourceInfo(Rect rect, SerializedProperty property)
        {
            AudioSource src = GetAudioSource(property);

            if (src == null)
            {
                // Pas d'AudioSource : bouton pour en ajouter un
                float btnW = 130f;
                Rect msgRect = new Rect(rect.x, rect.y, rect.width - btnW - 4f, rect.height);
                Rect btnRect = new Rect(rect.xMax - btnW, rect.y + 2f, btnW, rect.height - 4f);

                GUI.Label(msgRect, "Aucun AudioSource sur ce GameObject.",
                    new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = new Color(1f, 0.6f, 0.2f) } });

                if (DrawButton(btnRect, "+ Add AudioSource", false))
                {
                    var mono = property.serializedObject.targetObject as MonoBehaviour;
                    if (mono != null)
                        Undo.AddComponent<AudioSource>(mono.gameObject);
                }
            }
            else
            {
                //AudioSource trouvé : affiche volume et pitch en lecture seule
                float colW = rect.width;
                Rect left = new Rect(rect.x, rect.y, colW, rect.height);

                GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
                { normal = { textColor = new Color(0.65f, 0.65f, 0.65f) } };

                GUI.Label(left, "(Volume, Pitch, etc.. Play Mode uniquement)", style);
            }
        }
        #endregion

        #region Draw Trim / Warning / Handle
        private void DrawTrimInfo(Rect rect, SerializedProperty property, AudioClip clip)
        {
            float tStart = GetTrimStart(property) * clip.length;
            float tEnd = GetTrimEnd(property) * clip.length;
            GUI.Label(rect,
                "Trim In: " + FormatTime(tStart) + " -> Out: " + FormatTime(tEnd)
                + "  (" + FormatTime(tEnd - tStart) + ")",
                new GUIStyle(EditorStyles.miniLabel)
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
            { GUIUtility.hotControl = id; ev.Use(); }
            else if (evt == EventType.MouseDrag && GUIUtility.hotControl == id)
            {
                float newH = Mathf.Clamp(
                    EditorPrefs.GetFloat(key, _WAVEFORM_DEFAULT) + ev.delta.y,
                    _WAVEFORM_MIN, _WAVEFORM_MAX);
                EditorPrefs.SetFloat(key, newH);
                GUI.changed = true;
                ev.Use();
            }
            else if (evt == EventType.MouseUp && GUIUtility.hotControl == id)
            { GUIUtility.hotControl = 0; ev.Use(); }

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeVertical);
        }
        #endregion

        #region Boutons
        private bool DrawButton(Rect rect, string label, bool active)
        {
            Color bg = active ? new Color(0.28f, 0.65f, 1f) : new Color(0.22f, 0.22f, 0.22f);
            Color bord = active ? new Color(0.28f, 0.65f, 1f) : new Color(0.12f, 0.12f, 0.12f);
            Color txt = active ? Color.black : new Color(0.88f, 0.88f, 0.88f);

            EditorGUI.DrawRect(rect, bord);
            EditorGUI.DrawRect(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2), bg);
            GUI.Label(rect, label, new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = txt }
            });

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            { Event.current.Use(); return true; }
            return false;
        }
        #endregion

        #region Trim State
        private float GetTrimStart(SerializedProperty p) => EditorPrefs.GetFloat(TrimKey(p, "S"), 0f);
        private float GetTrimEnd(SerializedProperty p) => EditorPrefs.GetFloat(TrimKey(p, "E"), 1f);
        private void SetTrimStart(SerializedProperty p, float v) => EditorPrefs.SetFloat(TrimKey(p, "S"), v);
        private void SetTrimEnd(SerializedProperty p, float v) => EditorPrefs.SetFloat(TrimKey(p, "E"), v);
        #endregion

        #region Helpers
        private string FormatTime(float s) => (int)(s / 60f) + ":" + (s % 60f).ToString("00.00");

        private string WaveKey(SerializedProperty p) =>
            "Wave_" + p.serializedObject.targetObject.GetInstanceID() + "_" + p.propertyPath;
        private string TrimKey(SerializedProperty p, string side) =>
            "Trim" + side + "_" + p.serializedObject.targetObject.GetInstanceID() + "_" + p.propertyPath;
        #endregion    
    }
}