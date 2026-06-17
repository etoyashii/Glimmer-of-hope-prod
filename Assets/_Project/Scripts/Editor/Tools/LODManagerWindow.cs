using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor.Tools
{
    /// <summary>
    /// Editor window for the LOD tool. Scan the selection or the scene, review or override
    /// the strategy per object in the table, then Apply. Open it from Tools > GlimmerOfHope > LOD Manager.
    /// </summary>
    public class LODManagerWindow : EditorWindow
    {
        private const float COL_TRIS = 64f;
        private const float COL_BOUNDS = 64f;
        private const float COL_INSTANCES = 56f;
        private const float COL_STRATEGY = 130f;
        private const float COL_STATUS = 170f;

        private readonly List<LODCandidate> _candidates = new List<LODCandidate>();
        private Vector2 _scroll;
        private bool _preferPrefabAsset = true;

        [MenuItem("Tools/GlimmerOfHope/LOD Manager")]
        public static void ShowWindow()
        {
            var window = GetWindow<LODManagerWindow>();
            window.titleContent = new GUIContent("LOD Manager");
            window.minSize = new Vector2(760f, 380f);
            window.Show();
        }

        // OnGUI runs every repaint. Flow: toolbar (scan) -> empty hint, or summary + table + apply bar.
        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space();

            if (_candidates.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "Sélectionne des prefabs/objets puis Scan Selection, ou Scan Active Scene.",
                    MessageType.Info);
                return;
            }

            DrawSummary();
            DrawHeader();
            DrawRows();
            EditorGUILayout.Space();
            DrawApplyBar();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Scan Selection", EditorStyles.toolbarButton, GUILayout.Width(120f)))
                    ScanSelection();
                if (GUILayout.Button("Scan Active Scene", EditorStyles.toolbarButton, GUILayout.Width(140f)))
                    ScanScene();
                GUILayout.FlexibleSpace();
                _preferPrefabAsset = GUILayout.Toggle(
                    _preferPrefabAsset, "Éditer le prefab source", EditorStyles.toolbarButton, GUILayout.Width(160f));
            }
        }

        private void ScanSelection()
        {
            var renderers = new List<Renderer>();
            foreach (var go in Selection.gameObjects)
                renderers.AddRange(go.GetComponentsInChildren<Renderer>(true));
            Populate(renderers);
        }

        private void ScanScene()
        {
            // Unsorted is the cheapest option; row order in the table does not matter.
            var renderers = new List<Renderer>(FindObjectsByType<Renderer>(FindObjectsSortMode.None));
            Populate(renderers);
        }

        private void Populate(List<Renderer> renderers)
        {
            _candidates.Clear();
            _candidates.AddRange(LODClassifier.Scan(renderers));
        }

        private void DrawSummary()
        {
            int group = 0, visible = 0, skip = 0;
            foreach (var c in _candidates)
            {
                if (c.Chosen == LODStrategy.LODGroup) group++;
                else if (c.Chosen == LODStrategy.AlwaysVisible) visible++;
                else skip++;
            }

            EditorGUILayout.LabelField(
                $"{_candidates.Count} objets — LODGroup {group} · AlwaysVisible {visible} · Skip {skip}",
                EditorStyles.boldLabel);
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Space(22f);
                EditorGUILayout.LabelField("Objet", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Tris", EditorStyles.boldLabel, GUILayout.Width(COL_TRIS));
                EditorGUILayout.LabelField("Taille", EditorStyles.boldLabel, GUILayout.Width(COL_BOUNDS));
                EditorGUILayout.LabelField("Inst.", EditorStyles.boldLabel, GUILayout.Width(COL_INSTANCES));
                EditorGUILayout.LabelField("Stratégie", EditorStyles.boldLabel, GUILayout.Width(COL_STRATEGY));
                EditorGUILayout.LabelField("Statut", EditorStyles.boldLabel, GUILayout.Width(COL_STATUS));
            }
        }

        private void DrawRows()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var c in _candidates)
                DrawRow(c);
            EditorGUILayout.EndScrollView();
        }

        // One editable row per candidate: the EnumPopup lets the user override the recommended strategy before Apply.
        private void DrawRow(LODCandidate c)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                c.Enabled = EditorGUILayout.Toggle(c.Enabled, GUILayout.Width(18f));
                EditorGUILayout.ObjectField(c.Renderer, typeof(Renderer), true);
                EditorGUILayout.LabelField(c.TriangleCount.ToString(), GUILayout.Width(COL_TRIS));
                EditorGUILayout.LabelField(c.BoundsSize.ToString("0.0"), GUILayout.Width(COL_BOUNDS));
                EditorGUILayout.LabelField(c.InstanceCount.ToString(), GUILayout.Width(COL_INSTANCES));
                c.Chosen = (LODStrategy)EditorGUILayout.EnumPopup(c.Chosen, GUILayout.Width(COL_STRATEGY));
                EditorGUILayout.LabelField(BuildStatus(c), GUILayout.Width(COL_STATUS));
            }
        }

        private string BuildStatus(LODCandidate c)
        {
            if (c.Blocked) return "Bloqué: " + c.Reason;
            if (!c.IsReadable && c.Chosen != LODStrategy.Skip) return "Read/Write requis";
            if (c.AlreadyProcessed) return "Déjà traité (rebuild)";
            return c.Reason;
        }

        private void DrawApplyBar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Appliquer", GUILayout.Height(28f)))
                    Apply();
                if (GUILayout.Button("Vider", GUILayout.Width(80f), GUILayout.Height(28f)))
                    _candidates.Clear();
            }
        }

        private void Apply()
        {
            if (!Confirm()) return;

            int applied = LODApplier.Apply(_candidates, _preferPrefabAsset);
            EditorUtility.DisplayDialog("LOD Manager", applied + " objets traités.", "OK");
            ScanScene(); // re-scan so the table reflects the new LODGroups (rows flip to "already processed")
        }

        private bool Confirm()
        {
            return EditorUtility.DisplayDialog(
                "Appliquer le LOD",
                "Génère les meshes décimés et construit les LODGroups.\n\n" +
                "L'édition des prefabs sources n'est pas annulable (Ctrl+Z) — commit avant.\n\nContinuer ?",
                "Appliquer", "Annuler");
        }
    }
}
