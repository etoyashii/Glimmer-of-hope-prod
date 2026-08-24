using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor.NewDialogue
{
    /// <summary>
    /// Draws the Choices list for a DialogueLineNode in the Inspector. Works directly on the
    /// node's raw List rather than through SerializedProperty array
    /// </summary>
    public class DialogueChoiceListDrawer
    {
        #region Private Fields
        private readonly DialogueGraph _graph;
        private readonly DialogueNodeLinkPicker _linkPicker;
        #endregion

        #region Public Methods
        public DialogueChoiceListDrawer(DialogueGraph graph, DialogueNodeLinkPicker linkPicker)
        {
            _graph = graph;
            _linkPicker = linkPicker;
        }

        public void DrawMultipleChoices(DialogueNodeBase ownerNode, string selfId)
        {
            EditorGUILayout.LabelField("Choices", EditorStyles.boldLabel);

            DialogueChoice toRemove = null;

            foreach (var choice in ownerNode.choices)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUI.BeginChangeCheck();
                string resolvedText = DialogueLocalizationSync.GetSourceValue(choice.localizedChoiceText, choice.choiceText);
                string newText = EditorGUILayout.TextField(resolvedText);
                if (EditorGUI.EndChangeCheck())
                {
                    choice.choiceText = newText;
                    DialogueLocalizationSync.UpdateSourceValue(choice.localizedChoiceText, newText);
                    EditorUtility.SetDirty(_graph);
                }

                _linkPicker.DrawNextDropdownRaw(choice, "→", GUILayout.Width(260));

                if (GUILayout.Button("x", GUILayout.Width(22)))
                    toRemove = choice;

                EditorGUILayout.EndHorizontal();
            }

            if (toRemove != null)
            {
                Undo.RecordObject(_graph, "Remove Choice");
                DialogueLocalizationSync.RemoveEntry(toRemove.localizedChoiceText);
                ownerNode.choices.Remove(toRemove);
                EditorUtility.SetDirty(_graph);
            }

            if (GUILayout.Button("+ Choice", GUILayout.Width(100)))
            {
                Undo.RecordObject(_graph, "Add Choice");
                var newChoice = new DialogueChoice { choiceText = "", nextNodeId = "" };
                DialogueLocalizationSync.CreateEntry(out newChoice.localizedChoiceText, $"choice_{ownerNode.nodeId}_{ownerNode.choices.Count}");
                ownerNode.choices.Add(newChoice);
                EditorUtility.SetDirty(_graph);
            }
        }

        public void DrawSingleContinuation(DialogueNodeBase ownerNode, string selfId)
        {
            if (ownerNode.choices.Count == 0)
                ownerNode.choices.Add(new DialogueChoice { choiceText = "", nextNodeId = "" });
            while (ownerNode.choices.Count > 1)
                ownerNode.choices.RemoveAt(ownerNode.choices.Count - 1);

            ownerNode.choices[0].choiceText = "";
            _linkPicker.DrawNextDropdownRaw(ownerNode.choices[0], "Next");
        }
        #endregion
    }
}