using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor.Tools
{
    public sealed partial class SpriteMaterialWindow : EditorWindow
    {
        private const float COL_ACTION = 74f;
        private const float COL_MATERIAL = 210f;
        private const float COL_TOGGLE = 22f;

        [SerializeField] private Shader _shader;
        [SerializeField] private DefaultAsset _textureFolder;
        [SerializeField] private DefaultAsset _outputFolder;
        [SerializeField] private string _textureProperty = SpriteMaterialSettings.DEFAULT_TEXTURE_PROPERTY;
        [SerializeField] private bool _recursive = true;
        [SerializeField] private bool _overwrite;
        [SerializeField] private string _excludedSuffixes = string.Empty;

        private readonly List<SpriteMaterialEntry> _plan = new List<SpriteMaterialEntry>();
        private Vector2 _scroll;
        private bool _hasPlan;

        [MenuItem(SpriteMaterialSettings.MENU_PATH)]
        public static void ShowWindow()
        {
            var window = GetWindow<SpriteMaterialWindow>();
            window.titleContent = new GUIContent("Sprite Materials");
            window.minSize = new Vector2(620f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            LoadPrefs();
        }

        private void OnDisable()
        {
            SavePrefs();
        }

        private void OnGUI()
        {
            DrawSource();
            DrawOptions();
            EditorGUILayout.Space();
            DrawPropertyStatus();
            DrawActionBar();

            if (!_hasPlan)
                return;

            EditorGUILayout.Space();
            DrawSummary();
            DrawPlan();
        }

        private void DrawSource()
        {
            EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
            _shader = (Shader)EditorGUILayout.ObjectField("Shader Graph", _shader, typeof(Shader), false);
            _textureFolder = DrawFolderField("Dossier textures", _textureFolder);
            _outputFolder = DrawFolderField("Dossier materials", _outputFolder);

            string output = AssetPath(_outputFolder);

            if (!string.IsNullOrEmpty(output)
                && !output.StartsWith(SpriteMaterialSettings.GENERATED_ROOT, StringComparison.Ordinal))
            {
                EditorGUILayout.HelpBox(
                    "L'ADR-009 range les assets produits par un outil sous Assets/_Generated/. "
                    + "Ici la sortie est editee a la main apres coup : a trancher en equipe.",
                    MessageType.None);
            }
        }

        private static DefaultAsset DrawFolderField(string label, DefaultAsset folder)
        {
            return (DefaultAsset)EditorGUILayout.ObjectField(label, folder, typeof(DefaultAsset), false);
        }

        private void DrawOptions()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);

            _textureProperty = EditorGUILayout.TextField(
                new GUIContent("Propriete texture", "Reference de la propriete Texture2D dans le Blackboard du Shader Graph."),
                _textureProperty);

            _recursive = EditorGUILayout.Toggle(
                new GUIContent("Inclure les sous-dossiers", "Decoche pour ne traiter que le dossier choisi."),
                _recursive);

            _excludedSuffixes = EditorGUILayout.TextField(
                new GUIContent("Suffixes exclus", "Separes par des virgules, par exemple _noise,_blur. Vide = tout est traite."),
                _excludedSuffixes);

            _overwrite = EditorGUILayout.Toggle(
                new GUIContent("Mettre a jour l'existant", "Reassigne shader et texture sur le material existant. Le GUID est conserve, les references des VFX survivent."),
                _overwrite);
        }

        private void DrawPropertyStatus()
        {
            if (_shader == null)
                return;

            if (SpriteMaterialShaderProbe.HasTextureProperty(_shader, _textureProperty))
                return;

            List<string> available = SpriteMaterialShaderProbe.ListTextureProperties(_shader);

            EditorGUILayout.HelpBox(
                "Le shader n'expose aucune propriete Texture2D nommee " + _textureProperty + ".\n"
                + "Disponibles : " + (available.Count == 0 ? "aucune" : string.Join(", ", available.ToArray())),
                MessageType.Error);
        }

        private void DrawActionBar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.enabled = _textureFolder != null && _outputFolder != null;

                if (GUILayout.Button("Previsualiser", GUILayout.Height(28f)))
                    BuildPlan();

                GUI.enabled = CanGenerate();

                if (GUILayout.Button("Generer", GUILayout.Height(28f)))
                    Generate();

                GUI.enabled = true;
            }
        }

        private bool CanGenerate()
        {
            return _hasPlan
                && _shader != null
                && SpriteMaterialShaderProbe.HasTextureProperty(_shader, _textureProperty);
        }

        private void DrawSummary()
        {
            int create = 0;
            int update = 0;
            int skip = 0;

            foreach (SpriteMaterialEntry entry in _plan)
            {
                if (!entry.Enabled || entry.Action == SpriteMaterialAction.Skip)
                    skip++;
                else if (entry.Action == SpriteMaterialAction.Update)
                    update++;
                else
                    create++;
            }

            EditorGUILayout.HelpBox(
                _plan.Count + " texture(s) : " + create + " a creer, " + update + " a mettre a jour, " + skip + " ignoree(s).",
                MessageType.Info);
        }

        private void DrawPlan()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (SpriteMaterialEntry entry in _plan)
                DrawRow(entry);

            EditorGUILayout.EndScrollView();
        }

        private static void DrawRow(SpriteMaterialEntry entry)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                bool selectable = entry.Action != SpriteMaterialAction.Skip;

                GUI.enabled = selectable;
                entry.Enabled = EditorGUILayout.Toggle(entry.Enabled, GUILayout.Width(COL_TOGGLE));
                GUI.enabled = true;

                EditorGUILayout.LabelField(entry.Action.ToString(), GUILayout.Width(COL_ACTION));
                EditorGUILayout.LabelField(entry.MaterialName, GUILayout.Width(COL_MATERIAL));
                EditorGUILayout.LabelField(entry.TextureName + "  -  " + entry.Reason, EditorStyles.miniLabel);
            }
        }

        private void BuildPlan()
        {
            _plan.Clear();
            _hasPlan = false;

            SpriteMaterialRequest request = BuildRequest();
            string error = SpriteMaterialScanner.Validate(request);

            if (error != null)
            {
                EditorUtility.DisplayDialog("Sprite Material Generator", error, "OK");
                return;
            }

            var manifest = new SpriteMaterialManifest(request.OutputFolder);
            _plan.AddRange(new SpriteMaterialScanner(request, manifest).Scan());
            _hasPlan = true;
        }

        private void Generate()
        {
            SpriteMaterialRequest request = BuildRequest();
            string error = SpriteMaterialScanner.Validate(request);

            if (error != null)
            {
                EditorUtility.DisplayDialog("Sprite Material Generator", error, "OK");
                return;
            }

            var context = new SpriteMaterialApplyContext
            {
                Shader = _shader,
                TextureProperty = _textureProperty,
                Manifest = new SpriteMaterialManifest(request.OutputFolder)
            };

            SpriteMaterialReport report = SpriteMaterialApplier.Apply(_plan, context);

            ReportResult(report);
            BuildPlan();
        }

        private static void ReportResult(SpriteMaterialReport report)
        {
            foreach (string error in report.Errors)
                Debug.LogWarning(SpriteMaterialSettings.LOG_PREFIX + error);

            EditorUtility.DisplayDialog(
                "Sprite Material Generator",
                (report.Cancelled ? "Interrompu.\n\n" : string.Empty)
                + "Crees : " + report.Created
                + "\nMis a jour : " + report.Updated
                + "\nIgnores : " + report.Skipped
                + "\nEchecs : " + report.Failed,
                "OK");
        }

        private SpriteMaterialRequest BuildRequest()
        {
            return new SpriteMaterialRequest
            {
                TextureFolder = AssetPath(_textureFolder),
                OutputFolder = AssetPath(_outputFolder),
                Recursive = _recursive,
                Overwrite = _overwrite,
                ExcludedSuffixes = SplitSuffixes(_excludedSuffixes)
            };
        }

        private static string AssetPath(DefaultAsset folder)
        {
            return folder == null ? string.Empty : AssetDatabase.GetAssetPath(folder);
        }

        private static string[] SplitSuffixes(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return new string[0];

            string[] parts = raw.Split(',');
            var cleaned = new List<string>();

            foreach (string part in parts)
            {
                string trimmed = part.Trim();

                if (trimmed.Length > 0)
                    cleaned.Add(trimmed);
            }

            return cleaned.ToArray();
        }
    }
}
