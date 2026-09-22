using System.IO;
using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor.NewDialogue
{
    public class DialogueScriptImporterWindow : EditorWindow
    {
        #region Private Fields
        private enum Mode { SingleFile, Folder }

        private Mode _mode = Mode.SingleFile;

        // Single file mode
        private string _csvPath;
        private DialogueGraph _targetGraph;

        // Folder mode
        private string _sourceFolder;
        private string _outputFolder;

        #endregion

        #region Public Methods

        [MenuItem("Window/Dialogue System/Import From CSV (Script Format)...")]
        public static void Open()
        {
            var window = GetWindow<DialogueScriptImporterWindow>();
            window.titleContent = new GUIContent("Import Dialogue CSV");
            window.minSize = new Vector2(420, 220);
        }
        #endregion

        #region Unity Lifecycle

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Import Dialogue From CSV", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _mode = (Mode)GUILayout.Toolbar((int)_mode, new[] { "Single File", "Folder (one graph per feuille)" });
            EditorGUILayout.Space();

            if (_mode == Mode.SingleFile)
                DrawSingleFileGUI();
            else
                DrawFolderGUI();
        }
        #endregion

        #region Private Methods

        private void DrawSingleFileGUI()
        {
            EditorGUILayout.HelpBox("Export a single tab of your Google Sheet as CSV (File > Download > CSV) before importing. One row per dialogue line, top to bottom.", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("CSV file", string.IsNullOrEmpty(_csvPath) ? "(not selected)" : _csvPath);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                var picked = EditorUtility.OpenFilePanel("Select Dialogue CSV", "", "csv");
                if (!string.IsNullOrEmpty(picked)) _csvPath = picked;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            _targetGraph = (DialogueGraph)EditorGUILayout.ObjectField("Target Graph (optional)", _targetGraph, typeof(DialogueGraph), false);
            EditorGUILayout.HelpBox("Leave empty to create a brand new DialogueGraph asset. If set, its content is fully REPLACED.", MessageType.Warning);

            EditorGUILayout.Space();
            GUI.enabled = !string.IsNullOrEmpty(_csvPath);
            if (GUILayout.Button("Import", GUILayout.Height(28)))
                DialogueScriptImporter.Import(_csvPath, _targetGraph);
            GUI.enabled = true;
        }

        private void DrawFolderGUI()
        {
            EditorGUILayout.HelpBox("A CSV can only hold one tab, so export each feuille of your Google Sheet as its own CSV and put them all in one folder. Every .csv found directly in that folder becomes its own DialogueGraph, named after the file.", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Source folder", string.IsNullOrEmpty(_sourceFolder) ? "(not selected)" : _sourceFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                var picked = EditorUtility.OpenFolderPanel("Select Folder Containing Dialogue CSVs", "", "");
                if (!string.IsNullOrEmpty(picked)) _sourceFolder = picked;
            }
            EditorGUILayout.EndHorizontal();

            int csvCount = 0;
            if (!string.IsNullOrEmpty(_sourceFolder) && Directory.Exists(_sourceFolder))
                csvCount = Directory.GetFiles(_sourceFolder, "*.csv", SearchOption.TopDirectoryOnly).Length;
            if (!string.IsNullOrEmpty(_sourceFolder))
                EditorGUILayout.LabelField(" ", $"{csvCount} CSV file(s) found � {csvCount} graph(s) will be created.");

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Output folder (in project)", string.IsNullOrEmpty(_outputFolder) ? "(not selected)" : _outputFolder);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                var picked = EditorUtility.OpenFolderPanel("Select Output Folder (inside Assets)", Application.dataPath, "");
                if (!string.IsNullOrEmpty(picked)) _outputFolder = picked;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("Each import creates NEW graph assets (uniquely named if one already exists) � nothing existing gets overwritten.", MessageType.Warning);

            EditorGUILayout.Space();
            GUI.enabled = csvCount > 0 && !string.IsNullOrEmpty(_outputFolder);
            if (GUILayout.Button($"Import {csvCount} Feuille(s)", GUILayout.Height(28)))
                DialogueScriptFolderImporter.ImportFolder(_sourceFolder, _outputFolder);
            GUI.enabled = true;
        }
        #endregion
    }
}