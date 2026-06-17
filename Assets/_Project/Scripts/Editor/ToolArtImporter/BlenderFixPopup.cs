using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Popup that opens automatically right after the import of one or more
/// FBXs, offering to apply the Scale 50 (visible, not hidden) on the
/// root Transform of each listed model
/// </summary>
namespace GlimmerOfHope.Editor
{

    public class BlenderFixPopup : EditorWindow
    {
        #region Private Fields

        List<string> _assetPaths = new List<string>();
        Vector2 _scroll;
        #endregion

        #region Unity LifeCycle
        void OnGUI()
        {
            GUILayout.Space(8);
            EditorGUILayout.LabelField("Nouveaux modèles FBX importés", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"Applique un Scale réel de {BlenderFbxImportFix.kTargetScale} sur le Transform racine " +
                "(visible dans l'Inspector, X/Y/Z) et corrige la conversion d'axes Z-up/Y-up.",
                MessageType.Info);

            GUILayout.Space(6);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            for (int i = 0; i < _assetPaths.Count; i++)
            {
                string path = _assetPaths[i];
                if (string.IsNullOrEmpty(path)) continue;

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField(Path.GetFileName(path), GUILayout.ExpandWidth(true));

                if (GUILayout.Button($"🔧 Appliquer (Scale {BlenderFbxImportFix.kTargetScale})", GUILayout.Width(170)))
                {
                    BlenderFbxImportFix.ApplyFixToAsset(path);
                    _assetPaths.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(8);

            EditorGUILayout.BeginHorizontal();
            if (_assetPaths.Count > 1 &&
                GUILayout.Button($"🔧 Appliquer à TOUS ({_assetPaths.Count})"))
            {
                foreach (var path in _assetPaths.ToList())
                {
                    BlenderFbxImportFix.ApplyFixToAsset(path);
                }
                _assetPaths.Clear();
            }

            if (GUILayout.Button("Ignorer", GUILayout.Width(80)))
            {
                Close();
            }
            EditorGUILayout.EndHorizontal();

            if (_assetPaths.Count == 0)
            {
                GUILayout.Space(8);
                EditorGUILayout.HelpBox("Tous les modèles ont été traités.", MessageType.Info);
                if (GUILayout.Button("Fermer"))
                {
                    Close();
                }
            }
        }
        #endregion

        #region Public Methods
        public static void ShowForAssets(List<string> assetPaths)
        {
            if (assetPaths == null || assetPaths.Count == 0) return;

            var window = GetWindow<BlenderFixPopup>(true, "Blender Fix — Nouveaux imports", true);

            // Merges with a possible list already pending in the window,
            // instead of overwriting it (case where multiple imports arrive before
            // the user has processed the previous batch)
            if (window._assetPaths == null) window._assetPaths = new List<string>();
            foreach (var p in assetPaths)
            {
                if (!window._assetPaths.Contains(p))
                    window._assetPaths.Add(p);
            }

            window.minSize = new Vector2(420, 160);
            window.maxSize = new Vector2(420, 480);
            window.Show();
        }

        #endregion
    }
}