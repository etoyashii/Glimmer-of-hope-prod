using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Codice.Client.Common.GameUI;
using PlasticPipe.PlasticProtocol.Messages;

namespace GlimmerOfHope.Editor
{
    [CustomPropertyDrawer(typeof(AudioPlayerAttribute))]
    public class AudioplayerDrawer : PropertyDrawer
    {
        private const float _SPACING = 4f;
        private const float _WAVEFORM_DEFAULT = 80f;
        private const float _WAVEFORM_MIN = 40f;
        private const float _WAVEFORM_MAX = 200f;
        private const float _HANDLE_HEIGHT = 8f;
        private const float _ROW_HEIGHT = 22f;
        private const float _BTN_WIDTH = 48f;
        private const float _MARKER_WIDTH = 2f;

        private float ExtraHeight => _HANDLE_HEIGHT + _ROW_HEIGHT + _ROW_HEIGHT + _ROW_HEIGHT + _ROW_HEIGHT + 6f;
        //                              HANDLE          CONTROLS       VOLUME        PITCH          TRIM

        private static AudioSource _source;
        private static int _playingInstanceID = -1;
        private static bool _updateRegistered = false;

        //Trim
        private static Dictionary<string, float> _trimStart = new Dictionary<string, float>();
        private static Dictionary<string, float> _trimEnd = new Dictionary<string, float>();

        private static Dictionary<string, int> _prevClipID = new Dictionary<string, int>();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float waveH = EditorPrefs.GetFloat(WaveKey(property), _WAVEFORM_DEFAULT);

            if (property.objectReferenceValue == null)
                return EditorGUIUtility.singleLineHeight;

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

            Rect fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(fieldRect, property, label);

            AudioClip clip = property.objectReferenceValue as AudioClip;
            
            if (clip != null)
            {
                string path = property.propertyPath;
                int clipID = clip.GetInstanceID();
                if (_prevClipID.TryGetValue(path, out int prev) && prev != clipID)
                {
                    WaveformRenderer.Invalidate(prev);
                }

                _prevClipID[path] = clipID;

                float waveH = EditorPrefs.GetFloat(WaveKey(property), _WAVEFORM_DEFAULT);
                float y = position.y + EditorGUIUtility.singleLineHeight + _SPACING;

                //Waveform
                Rect waveRect = new Rect(position.x, y, position.width, waveH);
                DrawWaveform(waveRect, property, clip);
                y += waveH;

                //Handle resize
                Rect handleRect = new Rect(position.x, y, position.width, _HANDLE_HEIGHT);
                DrawResizeHandle(handleRect, property);
                y += _HANDLE_HEIGHT;

                //Controls
                Rect ctrlRect = new Rect(position.x, y, position.width, _ROW_HEIGHT);
                DrawControls(ctrlRect, property, clip);
                y += _ROW_HEIGHT;

                //Volume
                Rect volRect = new Rect(position.x, y, position.width, _ROW_HEIGHT);
                DrawVolumeSlider(volRect, property);
                y += _ROW_HEIGHT;

                //Pitch
                Rect pitchRect = new Rect(position.x, y, position.width, _ROW_HEIGHT);
                DrawPitchSlider(pitchRect, property);
                y += _ROW_HEIGHT;

                //Trim display
                Rect trimRect = new Rect(position.x, y, position.width, _ROW_HEIGHT);
                DrawTrimInfo(trimRect, property, clip);
            }

            EditorGUI.EndProperty();
        }

        //--Draw Waveform--
        private  void DrawWaveform(Rect rect, SerializedProperty property, AudioClip clip)
        {
            int w = Mathf.Max(1, (int)rect.width);
            int h = Mathf.Max(1, (int) rect.height);

            Texture2D wave = WaveformRenderer.Get(clip, w, h);

            EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f));

            if (WaveformRenderer.IsStreaming(clip))
            {
                DrawStreamingWarning(rect, 
                    "Waveform indisponible : clip en mode Streaming.\n" + 
                    "Changez Load Type en Decompress On Load\n" +
                    "dans l'inspector du fichier audio.");
            }
            else if (WaveformRenderer.IsPreloadDisabled(clip))
            {
                DrawStreamingWarning(rect,
                    "Waveform indisponible : Preload Audio Data est désactivé.\n" +
                    "Cochez Preload Audio Data dans l'inspector du fichier Audio.");
            }
            else if (wave != null)
            {
                GUI.DrawTexture(rect, wave);
            }
            else
            {
                DrawStreamingWarning(rect, "Waveform indisponible (GetData a échoué).");
            }

            //Overlay Trim
            float tStart = GetTrimStart(property);
            float tEnd = GetTrimEnd(property);

            float leftW = tStart * rect.width;
            float rightX = rect.x + tEnd * rect.width;
            float rightW = (1f - tEnd) * rect.width;

            Color trimOverlay = new Color(0f, 0f, 0f, 0.55f);
            if (leftW > 0)
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, leftW, rect.height), trimOverlay);
            if (rightW > 0)
                EditorGUI.DrawRect(new Rect(rightX, rect.y, rightW, rect.height), trimOverlay);

            //Barres Trim (jaune)
            Color markerColor = new Color(1f, 0.85f, 0.2f);
            float startX = rect.x + tStart * rect.width;
            float endX = rect.x + tEnd * rect.width;
            EditorGUI.DrawRect(new Rect(startX - _MARKER_WIDTH * 0.5f, rect.y, _MARKER_WIDTH, rect.height), markerColor);
            EditorGUI.DrawRect(new Rect(endX - _MARKER_WIDTH * 0.5f, rect.y, _MARKER_WIDTH, rect.height), markerColor);

            //Volume (ligne blanche)
            float vol = EditorPrefs.GetFloat(VolKey(property), 1f);
            float volY = rect.y + rect.height * 0.5f - (vol * rect.height * 0.5f);
            EditorGUI.DrawRect(new Rect(rect.x, volY, rect.width, 1f), new Color(1f, 1f, 1f, 0.4f));

            //Barre de progression
            if (IsPlayingClip() && clip.length > 0)
            {
                float progress = GetClipPosition() / clip.length;
                float progX = rect.x + progress * rect.width;
                EditorGUI.DrawRect(new Rect(progX - 1f, rect.y, 2f, rect.height), new Color(1f, 1f, 1f, 0.9f));
            }

            HandleWaveformInput(rect, property, clip);
        }

        //--Interaction Waveform--
        private enum WaveDragMode
        {
            None, TrimStart, TrimEnd, Seek
        }
        private static WaveDragMode _dragMode = WaveDragMode.None;

        private void HandleWaveformInput(Rect rect, SerializedProperty property, AudioClip clip)
        {
            Event ev = Event.current;
            float t = Mathf.Clamp01((ev.mousePosition.x - rect.x) / rect.width);
            bool over = rect.Contains(ev.mousePosition);

            float tStart = GetTrimStart(property);
            float tEnd = GetTrimEnd(property);
            float startX = rect.x + tStart * rect.width;
            float endX = rect.x + tEnd * rect.width;
            float zone = 6f;

            bool nearStart = Mathf.Abs(ev.mousePosition.x - startX) < zone;
            bool nearEnd = Mathf.Abs(ev.mousePosition.x - endX) < zone;

            //Curseur
            if (over)
            {
                if (nearStart || nearEnd)
                    EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
                else
                    EditorGUIUtility.AddCursorRect(rect, MouseCursor.ArrowPlus);
            }

            int id = GUIUtility.GetControlID("WaveInput".GetHashCode(), FocusType.Passive, rect);

            if (ev.GetTypeForControl(id) == EventType.MouseDown && over)
            {
                GUIUtility.hotControl = id;
                if (nearStart) _dragMode = WaveDragMode.TrimStart;
                else if (nearEnd) _dragMode = WaveDragMode.TrimEnd;
                else _dragMode = WaveDragMode.Seek;
                ev.Use();
            }
            else if (ev.GetTypeForControl(id) == EventType.MouseDrag && GUIUtility.hotControl == id)
            {
                if (_dragMode == WaveDragMode.TrimStart)
                    SetTrimStart(property, Mathf.Clamp(t, 0f, GetTrimEnd(property) - 0.01f));
                else if (_dragMode == WaveDragMode.TrimEnd)
                    SetTrimEnd(property, Mathf.Clamp(t, GetTrimStart(property) + 0.01f, 1f));
                else if (_dragMode == WaveDragMode.Seek && _source != null && _source.clip == clip)
                    _source.time = t * clip.length;

                GUI.changed = true;
                ev.Use();
            }
            else if (ev.GetTypeForControl(id) == EventType.MouseUp && GUIUtility.hotControl == id)
            {
                GUIUtility.hotControl = 0;
                _dragMode = WaveDragMode.None;
                ev.Use();
            }
        }

        //--Controls Play / Stop--
        private void DrawControls(Rect rect, SerializedProperty property, AudioClip clip)
        {
            float x = rect.x;

            //Play
            Rect playRect = new Rect(x, rect.y + 2f, _BTN_WIDTH, _ROW_HEIGHT - 4f);
            bool isPlaying = _source != null && _source.isPlaying && _source.clip == clip;

            if (DrawButton(playRect, "Play", IsPlayingClip()))
            {
                int startSample = Mathf.RoundToInt(GetTrimStart(property) * clip.samples);
                PlayClip(clip, startSample);
                EnsureUpdate();
            }

            x += _BTN_WIDTH + 4f;

            //Stop
            Rect stopRect = new Rect(x, rect.y + 2f, _BTN_WIDTH, _ROW_HEIGHT - 4f);
            if (DrawButton(stopRect, "Stop", false))
            {
                StopClip();
            }

            x += _BTN_WIDTH + 8f;

            //Temps
            string timeStr = FormatTime(GetPlaybackTime(clip)) + " / " + FormatTime(clip.length);
            GUI.Label(new Rect(x, rect.y, rect.width - x + rect.x, _ROW_HEIGHT), timeStr,
                new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.7f, 0.7f, 0.7f) } });
        }

        //--Sliders Volume / Pitch--
        private void DrawVolumeSlider(Rect rect, SerializedProperty property)
        {
            float vol = EditorPrefs.GetFloat(VolKey(property), 1f);
            float newVol = DrawLabelSlider(rect, "Volume", vol, 0f, 1f);
            if (!Mathf.Approximately(newVol, vol))
            {
                EditorPrefs.SetFloat(VolKey(property), newVol);
                if (_source != null) _source.volume = newVol;
            }
        }

        private void DrawPitchSlider(Rect rect, SerializedProperty property)
        {
            float pitch = EditorPrefs.GetFloat(PitchKey(property), 1f);
            float newPitch = DrawLabelSlider(rect, "Pitch", pitch, 0.1f, 3f);
            if (!Mathf.Approximately(newPitch, pitch))
            {
                EditorPrefs.SetFloat(PitchKey(property), newPitch);
                if (_source != null) _source.pitch = newPitch;
            }
        }

        private float DrawLabelSlider(Rect rect, string labelText, float value, float min, float max)
        {
            float labelW = 46f;
            float valW = 34f;

            GUI.Label(new Rect(rect.x, rect.y, labelW, rect.height), labelText, new GUIStyle(EditorStyles.miniLabel));
            Rect sliderRect = new Rect(rect.x + labelW, rect.y + (rect.height - 14f) * 0.5f, rect.width - labelW - valW - 4f, 14f);
            float newVal = GUI.HorizontalSlider(sliderRect, value, min, max);

            GUI.Label(new Rect(rect.xMax - valW, rect.y, valW, rect.height), value.ToString("F2"), new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight });

            return newVal;
        }

        //--Trim Info--
        private void DrawTrimInfo(Rect rect, SerializedProperty property, AudioClip clip)
        {
            float tStart = GetTrimStart(property) * clip.length;
            float tEnd = GetTrimEnd(property) * clip.length;

            string txt = "Trim In: " + FormatTime(tStart) + " -> Out: " + FormatTime(tEnd) + " (" + FormatTime(tEnd - tStart) + ")";

            GUI.Label(rect, txt, new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(1f, 0.85f, 0.2f) }
            });
        }

        //--Streaming Warning--
        private void DrawStreamingWarning(Rect rect,
                    string msg = "Waveform indisponible : clip en mode Streaming."
                    + "Project Settings -> Audio -> changez Load Type en"
                    + "Decompress On Load ou Compressed In Memory.")
        {
            GUIStyle style = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.75f, 0.2f) }
            };
            GUI.Label(rect, msg, style);
        }

        private void DrawResizeHandle(Rect rect, SerializedProperty property)
        {
            string key = WaveKey(property);
            bool hover = rect.Contains(Event.current.mousePosition);

            EditorGUI.DrawRect(rect, hover ? new Color(0.28f, 0.65f, 1f, 0.5f) : new Color(0.22f, 0.22f, 0.22f));

            float cx = rect.x + rect.width * 0.5f;
            float cy = rect.y + rect.height * 0.5f;
            for (int i = -1; i <= 1; ++i)
            {
                EditorGUI.DrawRect(new Rect(cx - 14f, cy + i * 2.5f - 0.5f, 28f, 1f), new Color(0.55f, 0.55f, 0.55f));
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
                float waveH = EditorPrefs.GetFloat(key, _WAVEFORM_DEFAULT);
                float newH = Mathf.Clamp(waveH + ev.delta.y, _WAVEFORM_MIN, _WAVEFORM_MAX);
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

        //--AudioSource Preview--
        private static AudioSource GetPreviewSource()
        {
            if (_source != null && _source.gameObject != null)
                return _source;

            var go = new GameObject("__EditorAudioPreview")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            _source = go.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            return _source;
        }

        private static void EnsureUpdate()
        {
            if (_updateRegistered) return;
            EditorApplication.update += OnEditorUpdate;
            _updateRegistered = true;
        }

        private static void OnEditorUpdate()
        {
            if (_source == null || !_source.isPlaying) return;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        //--Trim state--
        private float GetTrimStart(SerializedProperty property)
        {
            string k = TrimKey(property, "S");
            return EditorPrefs.GetFloat(k, 0f);
        }

        private float GetTrimEnd(SerializedProperty property)
        {
            string k = TrimKey(property, "E");
            return EditorPrefs.GetFloat(k, 1f);
        }

        private void SetTrimStart(SerializedProperty property, float v)
        {
            EditorPrefs.SetFloat(TrimKey(property, "S"), v);
        }

        private void SetTrimEnd(SerializedProperty property, float v)
        {
            EditorPrefs.SetFloat(TrimKey(property, "E"), v);
        }

        //--Helpers--
        private float GetPlaybackTime(AudioClip clip)
        {
            return IsPlayingClip() ? GetClipPosition() : 0f;
        }

        private string FormatTime(float seconds)
        {
            int m = (int)(seconds / 60f);
            float s = seconds % 60f;
            return m + ":" + s.ToString("00.00");
        }

        private bool DrawButton(Rect rect, string label, bool active)
        {
            Color bg = active ? new Color(0.28f, 0.65f, 1f) : new Color(0.22f, 0.22f, 0.22f);
            Color border = active ? new Color(0.28f, 0.65f, 1f) : new Color(0.12f, 0.12f, 0.12f);
            Color text = active ? Color.black : new Color(0.88f, 0.88f, 0.88f);

            EditorGUI.DrawRect(rect, border);
            EditorGUI.DrawRect(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, rect.height - 2), bg);

            GUI.Label(rect, label, new GUIStyle(EditorStyles.boldLabel)
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

        private static System.Type _audioUtil = null;
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
            System.Type audioUtil = System.Type.GetType("UnityEditor.AudioUtil,UnityEditor");
            if (audioUtil == null) { Debug.LogError("[AudioPlayer] AudioUtil introuvable."); return; }

            //Signature Unity 6 : 3 params
            System.Reflection.MethodInfo m = audioUtil.GetMethod("PlayPreviewClip",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                null, new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            if (m != null) { m.Invoke(null, new object[] { clip, startSample, false }); return; }

            //Signature alternative : 1 param
            m = audioUtil.GetMethod("PlayPreviewClip",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                null, new System.Type[] { typeof(AudioClip) }, null);
            if (m != null) { m.Invoke(null, new object[] { clip }); return; }

            //Aucune signature trouvée -> liste toutes les méthodes disponibles dans la Console
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("[AudioPlayer] PlayPreviewClip introuvable. Méthodes AudioUtil disponibles :");
            foreach (var method in audioUtil.GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public))
            {
                string parms = string.Join(", ", System.Array.ConvertAll(
                    method.GetParameters(), p => p.ParameterType.Name));
                sb.AppendLine("  " + method.Name + "(" + parms + ")");
            }
            Debug.LogWarning(sb.ToString());
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
            if (m == null) return false;
            return (bool)m.Invoke(null, null);
        }

        private static float GetClipPosition()
        {
            var m = AudioUtil?.GetMethod("GetPreviewClipPosition",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (m == null) return 0f;
            return (float)m.Invoke(null, null);
        }

        //Clés EditorPrefs / cache
        private string WaveKey(SerializedProperty p) => "Wave_" + p.serializedObject.targetObject.GetInstanceID() + "_" + p.propertyPath;
        private string VolKey(SerializedProperty p) => "Vol_" + p.serializedObject.targetObject.GetInstanceID() + "_" + p.propertyPath;
        private string PitchKey(SerializedProperty p) => "Pitch_" + p.serializedObject.targetObject.GetInstanceID() + "_" + p.propertyPath;
        private string TrimKey(SerializedProperty p, string side) => "Trim" + side + "_" + p.serializedObject.targetObject.GetInstanceID() + "_" + p.propertyPath;
    }
}
