using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor.Tools
{
    public class PuzzleLucioleWindow : EditorWindow
    {
        private const float COL_STATUS = 130f;
        private const float COL_ZONE = 80f;
        private const float COL_BUTTON = 60f;
        private const float COL_TP = 90f;
        private const float TP_FIREFLY_DISTANCE = 1f;
        private const float TP_LANTERN_DISTANCE = 0.6f;

        private readonly List<PuzzleLucioleSpot> _spots = new List<PuzzleLucioleSpot>();
        private readonly List<bool> _selected = new List<bool>();

        private Vector2 _scroll;
        private bool _wireGrayZone = true;
        private bool _hasScanned;

        [MenuItem("Tools/GlimmerOfHope/Puzzle Luciole Setup")]
        public static void ShowWindow()
        {
            PuzzleLucioleWindow window = GetWindow<PuzzleLucioleWindow>();

            window.titleContent = new GUIContent("Puzzle Luciole");
            window.minSize = new Vector2(720f, 320f);
            window.Show();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawToolbar();

            if (!_hasScanned)
            {
                EditorGUILayout.HelpBox("Lance un scan pour lister les emplacements de la scene ouverte.", MessageType.Info);
                return;
            }

            if (_spots.Count == 0)
            {
                EditorGUILayout.HelpBox("Aucun emplacement trouve. Un emplacement = un objet ayant a la fois un enfant Luciole et un enfant Lanterne_Casser.", MessageType.Warning);
                return;
            }

            DrawTable();
            DrawFooter();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("Le scan ne modifie rien. Seul le bouton Appliquer ecrit dans la scene, et tout est annulable avec Ctrl+Z.", MessageType.None);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Scanner la scene", GUILayout.Height(24f)))
            {
                Rescan();
            }

            _wireGrayZone = EditorGUILayout.ToggleLeft("Cabler GrayZone.Repaint()", _wireGrayZone, GUILayout.Width(200f));

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);
        }

        private void DrawTable()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            for (int i = 0; i < _spots.Count; i++)
            {
                DrawRow(i);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawRow(int index)
        {
            PuzzleLucioleSpot spot = _spots[index];

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            using (new EditorGUI.DisabledScope(spot.IsWired))
            {
                _selected[index] = EditorGUILayout.Toggle(_selected[index], GUILayout.Width(18f));
            }

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(spot.Root.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(spot.Path, EditorStyles.miniLabel);
            EditorGUILayout.LabelField("id : " + spot.SuggestedId, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.LabelField(spot.Status, GUILayout.Width(COL_STATUS));
            EditorGUILayout.LabelField(spot.GrayZone == null ? "-" : "GrayZone", GUILayout.Width(COL_ZONE));

            if (GUILayout.Button("Voir", GUILayout.Width(COL_BUTTON)))
            {
                Selection.activeGameObject = spot.Root.gameObject;
                EditorGUIUtility.PingObject(spot.Root.gameObject);
            }

            if (Application.isPlaying)
            {
                DrawTeleportButtons(spot);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTeleportButtons(PuzzleLucioleSpot spot)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(COL_TP));

            if (GUILayout.Button("TP Luciole"))
            {
                PuzzleLucioleTeleport.TeleportNear(spot.Firefly, TP_FIREFLY_DISTANCE);
            }

            if (GUILayout.Button("TP Lanterne"))
            {
                PuzzleLucioleTeleport.TeleportNear(spot.BrokenLantern, TP_LANTERN_DISTANCE);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space(4f);

            if (Application.isPlaying)
            {
                DrawPlayFooter();
                return;
            }

            int pending = CountSelected();

            using (new EditorGUI.DisabledScope(pending == 0))
            {
                if (GUILayout.Button("Appliquer aux " + pending + " emplacement(s) coche(s)", GUILayout.Height(28f)))
                {
                    ApplySelection();
                }
            }

            if (GUILayout.Button("Remettre le reglage taille monde sur les " + _spots.Count + " emplacement(s)", GUILayout.Height(24f)))
            {
                int tuned = PuzzleLucioleTuning.ApplyAll(_spots);

                Debug.Log("[PuzzleLucioleWindow] Reglage applique sur " + tuned + " luciole(s). Ctrl+Z pour annuler.");
            }
        }

        private void DrawPlayFooter()
        {
            EditorGUILayout.HelpBox("Mode Play : le cablage est desactive, tout ce qui bouge ici est annule a l'arret.", MessageType.Info);

            using (new EditorGUI.DisabledScope(Selection.activeTransform == null))
            {
                if (GUILayout.Button("TP le joueur pres de l'objet selectionne", GUILayout.Height(24f)))
                {
                    PuzzleLucioleTeleport.TeleportNear(Selection.activeTransform, TP_LANTERN_DISTANCE);
                }
            }
        }

        private void OnSelectionChange()
        {
            Repaint();
        }

        private void Rescan()
        {
            _spots.Clear();
            _selected.Clear();
            _spots.AddRange(PuzzleLucioleScanner.Scan());

            for (int i = 0; i < _spots.Count; i++)
            {
                _selected.Add(!_spots[i].IsWired);
            }

            _hasScanned = true;
        }

        private int CountSelected()
        {
            int count = 0;

            for (int i = 0; i < _spots.Count; i++)
            {
                if (!_selected[i]) continue;
                if (_spots[i].IsWired) continue;

                count++;
            }

            return count;
        }

        private void ApplySelection()
        {
            List<PuzzleLucioleSpot> batch = new List<PuzzleLucioleSpot>();

            for (int i = 0; i < _spots.Count; i++)
            {
                if (!_selected[i]) continue;
                if (_spots[i].IsWired) continue;

                batch.Add(_spots[i]);
            }

            int applied = PuzzleLucioleApplier.ApplyAll(batch, _wireGrayZone);

            Debug.Log("[PuzzleLucioleWindow] " + applied + " emplacement(s) cable(s). Ctrl+Z pour annuler.");

            Rescan();
        }
    }
}
