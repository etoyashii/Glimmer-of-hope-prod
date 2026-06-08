using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor.Dialogue
{
    public class DialogueImportWindow : EditorWindow
    {
        #region Private Fields

        private string _csvPath = "";
        private Vector2 _scrollPosition;
        private DialogueCSVImporter.ImportResult _lastResult;
        private bool _showPreview;
        private string[] _previewLines;
        private int _previewLineCount;
        private int _previewConvCount;

        #endregion

        #region Menu Item

        [MenuItem("Glimmer/Dialogue/Import CSV", priority = 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<DialogueImportWindow>();
            window.titleContent = new GUIContent("CSV Import");
            window.minSize = new Vector2(450, 400);
            window.Show();
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            DrawFilePicker();

            if (_showPreview)
            {
                DrawPreview();
            }

            DrawImportButton();
            DrawResults();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Dialogue CSV Importer", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Import dialogue lines from a CSV file (Google Sheets export).\n" +
                "This will create/update DialogueLineSO assets and generate localization JSON files.\n" +
                "Assets are matched by id, so you can sort them into subfolders freely — re-imports keep them in place. " +
                "Optional last column 'folder' sets the subfolder for brand-new assets (e.g. Zone1 or Zone1/NPCs).",
                MessageType.Info);

            EditorGUILayout.Space(10);
        }

        private void DrawFilePicker()
        {
            EditorGUILayout.LabelField("CSV File", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            _csvPath = EditorGUILayout.TextField(_csvPath);

            if (GUILayout.Button("Browse...", GUILayout.Width(80)))
            {
                var path = EditorUtility.OpenFilePanel("Select CSV File", "", "csv");
                if (!string.IsNullOrEmpty(path))
                {
                    _csvPath = path;
                    LoadPreview();
                }
            }

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(_csvPath) && !_showPreview)
            {
                if (GUILayout.Button("Load Preview"))
                {
                    LoadPreview();
                }
            }

            EditorGUILayout.Space(10);
        }

        private void DrawPreview()
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField($"Lines: {_previewLineCount}");
            EditorGUILayout.LabelField($"Conversations: {_previewConvCount}");
            EditorGUILayout.LabelField($"Languages: fr, en, es");

            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("First rows:", EditorStyles.miniLabel);

            if (_previewLines != null)
            {
                foreach (var line in _previewLines)
                {
                    EditorGUILayout.LabelField(line, EditorStyles.wordWrappedMiniLabel);
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        private void DrawImportButton()
        {
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(_csvPath) || !File.Exists(_csvPath));

            var style = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                fixedHeight = 40
            };

            if (GUILayout.Button("Import CSV", style))
            {
                ImportCSV();
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space(10);
        }

        private void DrawResults()
        {
            if (_lastResult == null)
                return;

            EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);

            if (_lastResult.Success)
            {
                EditorGUILayout.HelpBox(
                    $"Import successful!\n" +
                    $"Lines created: {_lastResult.LinesCreated}\n" +
                    $"Lines updated: {_lastResult.LinesUpdated}\n" +
                    $"Conversations: {_lastResult.ConversationsCreated}\n" +
                    $"Localization files: {_lastResult.LocalizationFilesCreated}",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("Import completed with errors.", MessageType.Warning);
            }

            if (_lastResult.Errors.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Errors:", EditorStyles.boldLabel);

                foreach (var error in _lastResult.Errors)
                {
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                }
            }

            if (_lastResult.Warnings.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Warnings:", EditorStyles.boldLabel);

                foreach (var warning in _lastResult.Warnings)
                {
                    EditorGUILayout.HelpBox(warning, MessageType.Warning);
                }
            }
        }

        #endregion

        #region Import Logic

        private void LoadPreview()
        {
            if (string.IsNullOrEmpty(_csvPath) || !File.Exists(_csvPath))
            {
                _showPreview = false;
                return;
            }

            try
            {
                var lines = File.ReadAllLines(_csvPath, Encoding.UTF8);

                _previewLineCount = lines.Length - 1;

                var conversations = new System.Collections.Generic.HashSet<string>();
                for (int i = 1; i < lines.Length; i++)
                {
                    var cols = lines[i].Split(',');
                    if (cols.Length > 1 && !string.IsNullOrEmpty(cols[1]))
                    {
                        conversations.Add(cols[1].Trim().Trim('"'));
                    }
                }
                _previewConvCount = conversations.Count;

                _previewLines = new string[System.Math.Min(5, lines.Length)];
                for (int i = 0; i < _previewLines.Length; i++)
                {
                    var line = lines[i];
                    _previewLines[i] = line.Length > 80 ? line.Substring(0, 80) + "..." : line;
                }

                _showPreview = true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load preview: {ex.Message}");
                _showPreview = false;
            }
        }

        private void ImportCSV()
        {
            if (string.IsNullOrEmpty(_csvPath))
                return;

            EditorUtility.DisplayProgressBar("Importing CSV", "Parsing dialogue data...", 0.2f);

            try
            {
                var importer = new DialogueCSVImporter();
                _lastResult = importer.Import(_csvPath);

                if (_lastResult.Success)
                {
                    Debug.Log($"[DialogueImport] Success! Created {_lastResult.LinesCreated} lines, updated {_lastResult.LinesUpdated}");
                }
                else
                {
                    Debug.LogWarning($"[DialogueImport] Completed with {_lastResult.Errors.Count} errors");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        #endregion
    }
}
