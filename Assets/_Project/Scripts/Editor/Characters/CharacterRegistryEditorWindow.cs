using GlimmerOfHope.Gameplay.Characters;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor.Characters
{
    public class CharacterRegistryEditorWindow : EditorWindow
    {
        #region Constants
        private const string MENU_PATH = "Tools/GlimmerOfHope/Character Registry";
        private const float LABEL_W = 160f;
        #endregion

        #region Private Fields
        private CharacterRegistrySO _registry;
        private Vector2 _scrollPosition;
        private bool[] _categoryFoldouts;
        #endregion

        #region Editor Window Lifecycle
        [MenuItem(MENU_PATH)]
        public static void ShowWindow()
        {
            var window = GetWindow<CharacterRegistryEditorWindow>("Character Registry");
            window.minSize = new Vector2(420f, 300f);
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawRegistryPicker();

            if (_registry == null)
            {
                EditorGUILayout.HelpBox("Assigne un CharacterRegistrySO pour inspecter le système.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space(8f);
            DrawStats();
            EditorGUILayout.Space(8f);
            DrawCategories();
        }
        #endregion

        #region Draw Methods
        private void DrawHeader()
        {
            EditorGUILayout.Space(8f);
            GUILayout.Label("Character Registry Inspector", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }

        private void DrawRegistryPicker()
        {
            EditorGUI.BeginChangeCheck();

            _registry = (CharacterRegistrySO)EditorGUILayout.ObjectField(
                "Registry",
                _registry,
                typeof(CharacterRegistrySO),
                allowSceneObjects: false
            );

            if (EditorGUI.EndChangeCheck())
                _categoryFoldouts = null;
        }

        private void DrawStats()
        {
            var categories = _registry.Categories;
            int totalParts = 0;

            foreach (var cat in categories)
                if (cat != null) totalParts += cat.Parts.Count;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Catégories", categories.Count.ToString(), GUILayout.Width(LABEL_W + 60f));
            EditorGUILayout.LabelField("Parts totales", totalParts.ToString());
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCategories()
        {
            var categories = _registry.Categories;

            if (_categoryFoldouts == null || _categoryFoldouts.Length != categories.Count)
                _categoryFoldouts = new bool[categories.Count];

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            for (int i = 0; i < categories.Count; i++)
            {
                var category = categories[i];

                if (category == null)
                {
                    EditorGUILayout.HelpBox($"Catégorie [{i}] est null.", MessageType.Warning);
                    continue;
                }

                _categoryFoldouts[i] = EditorGUILayout.Foldout(
                    _categoryFoldouts[i],
                    $"{category.DisplayName} ({category.Parts.Count} parts)",
                    toggleOnLabelClick: true
                );

                if (!_categoryFoldouts[i])
                    continue;

                EditorGUI.indentLevel++;

                foreach (var part in category.Parts)
                {
                    if (part == null)
                    {
                        EditorGUILayout.HelpBox("Part null dans cette catégorie.", MessageType.Warning);
                        continue;
                    }

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(part.DisplayName, GUILayout.Width(LABEL_W));
                    EditorGUILayout.LabelField(part.PartType.ToString(), GUILayout.Width(80f));

                    if (GUILayout.Button("Ping", GUILayout.Width(50f)))
                        EditorGUIUtility.PingObject(part);

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUI.indentLevel--;
                EditorGUILayout.Space(4f);
            }

            EditorGUILayout.EndScrollView();
        }
        #endregion
    }
}
