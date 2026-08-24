using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEditor;
using UnityEngine;

namespace GlimmerOfHope.Editor.NewDialogue
{
    /// <summary>
    /// Draws a node's fields in the Inspector, dispatched by its actual type. Delegates
    /// anything about "pick the next node" to DialogueNodeLinkPicker.
    /// </summary>
    public class DialogueNodeFieldDrawer
    {
        #region Private Fields
        private readonly DialogueNodeLinkPicker _linkPicker;
        private readonly DialogueChoiceListDrawer _choiceListDrawer;
        #endregion

        #region Public Methods
        public DialogueNodeFieldDrawer(DialogueGraph graph, DialogueNodeLinkPicker linkPicker)
        {
            _linkPicker = linkPicker;
            _choiceListDrawer = new DialogueChoiceListDrawer(graph, linkPicker);
        }

        public void Draw(SerializedProperty node, DialogueNodeBase dataNode, string selfId)
        {
            switch (dataNode)
            {
                case DialogueLineNode lineNode: DrawDialogueFields(node, lineNode, selfId); break;
                case StartNode: DrawStartFields(node, selfId); break;
                case EndNode: DrawEndFields(node); break;
                case GateNode: DrawGateFields(node, selfId); break;
                case ConditionNode: DrawConditionFields(node, selfId); break;
                case ActionNode: DrawActionFields(node, selfId); break;
            }
        }
        #endregion

        #region Private Methods
        private void DrawDialogueFields(SerializedProperty node, DialogueLineNode dataNode, string selfId)
        {
            var speakerId = node.FindPropertyRelative("speakerId");
            var text = node.FindPropertyRelative("text");
            var bubblePrefab = node.FindPropertyRelative("bubblePrefab");
            var followSpeaker = node.FindPropertyRelative("followSpeaker");
            var bubbleOffset = node.FindPropertyRelative("bubbleOffset");
            var useTypewriter = node.FindPropertyRelative("useTypewriter");
            var typewriterSpeed = node.FindPropertyRelative("typewriterCharsPerSecond");
            var hasChoices = node.FindPropertyRelative("hasChoices");

            EditorGUILayout.PropertyField(speakerId, new GUIContent("Speaker ID"));

            EditorGUILayout.LabelField("Text");
            string resolvedText = DialogueLocalizationSync.GetSourceValue(dataNode.localizedText, text.stringValue);
            EditorGUI.BeginChangeCheck();
            string newText = EditorGUILayout.TextArea(resolvedText, GUILayout.MinHeight(40));
            if (EditorGUI.EndChangeCheck())
            {
                text.stringValue = newText;
                DialogueLocalizationSync.UpdateSourceValue(dataNode.localizedText, newText);
            }

            EditorGUILayout.PropertyField(bubblePrefab, new GUIContent("Bubble Prefab"));
            EditorGUILayout.PropertyField(followSpeaker, new GUIContent("Follows Speaker"));
            if (followSpeaker.boolValue)
                EditorGUILayout.PropertyField(bubbleOffset, new GUIContent("Bubble Offset"));

            EditorGUILayout.PropertyField(useTypewriter, new GUIContent("Typewriter Effect"));
            if (useTypewriter.boolValue)
                EditorGUILayout.PropertyField(typewriterSpeed, new GUIContent("Speed (chars/sec)"));

            EditorGUILayout.PropertyField(hasChoices, new GUIContent("Has Choices?"));

            if (hasChoices.boolValue) _choiceListDrawer.DrawMultipleChoices(dataNode, selfId);
            else _choiceListDrawer.DrawSingleContinuation(dataNode, selfId);
        }

        private void DrawStartFields(SerializedProperty node, string selfId)
        {
            var triggerType = node.FindPropertyRelative("triggerType");
            EditorGUILayout.PropertyField(triggerType, new GUIContent("Trigger"));

            var type = (DialogueTriggerType)triggerType.enumValueIndex;
            if (type == DialogueTriggerType.FloatingButton)
                EditorGUILayout.PropertyField(node.FindPropertyRelative("buttonOffset"), new GUIContent("Button Offset"));
            else if (type == DialogueTriggerType.TriggerZone)
                EditorGUILayout.PropertyField(node.FindPropertyRelative("triggerZoneRadius"), new GUIContent("Zone Radius"));

            var choices = node.FindPropertyRelative("choices");
            if (choices.arraySize == 0) choices.InsertArrayElementAtIndex(0);
            _linkPicker.DrawNextDropdown(choices.GetArrayElementAtIndex(0), "First Node", selfId);
        }

        private void DrawEndFields(SerializedProperty node)
        {
            EditorGUILayout.PropertyField(node.FindPropertyRelative("outcomeId"), new GUIContent("Outcome ID (optional)"));
        }

        private void DrawGateFields(SerializedProperty node, string selfId)
        {
            var triggerType = node.FindPropertyRelative("gateTriggerType");
            EditorGUILayout.PropertyField(triggerType, new GUIContent("Unlocked By"));

            var type = (DialogueGateTriggerType)triggerType.enumValueIndex;
            switch (type)
            {
                case DialogueGateTriggerType.ScriptEvent:
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("gateEventId"), new GUIContent("Event ID"));
                    break;
                case DialogueGateTriggerType.Timer:
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("gateTimerSeconds"), new GUIContent("Seconds"));
                    break;
                case DialogueGateTriggerType.Flag:
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("gateFlagName"), new GUIContent("Flag Name"));
                    EditorGUILayout.PropertyField(node.FindPropertyRelative("gateFlagExpectedValue"), new GUIContent("Expected Value"));
                    break;
            }

            var choices = node.FindPropertyRelative("choices");
            if (choices.arraySize == 0) choices.InsertArrayElementAtIndex(0);
            _linkPicker.DrawNextDropdown(choices.GetArrayElementAtIndex(0), "Next (once unlocked)", selfId);
        }

        private void DrawConditionFields(SerializedProperty node, string selfId)
        {
            var conditionType = node.FindPropertyRelative("conditionType");
            EditorGUILayout.PropertyField(conditionType, new GUIContent("Condition Type"));

            var type = (DialogueConditionType)conditionType.enumValueIndex;
            if (type == DialogueConditionType.Flag)
            {
                EditorGUILayout.PropertyField(node.FindPropertyRelative("conditionFlagName"), new GUIContent("Flag Name"));
                EditorGUILayout.PropertyField(node.FindPropertyRelative("conditionExpectedValue"), new GUIContent("Expected Value"));
            }
            else
            {
                EditorGUILayout.PropertyField(node.FindPropertyRelative("conditionScriptId"), new GUIContent("Condition ID"));
            }

            var choices = node.FindPropertyRelative("choices");
            while (choices.arraySize < 2) choices.InsertArrayElementAtIndex(choices.arraySize);
            while (choices.arraySize > 2) choices.DeleteArrayElementAtIndex(choices.arraySize - 1);

            _linkPicker.DrawNextDropdown(choices.GetArrayElementAtIndex(0), "If True", selfId);
            _linkPicker.DrawNextDropdown(choices.GetArrayElementAtIndex(1), "If False", selfId);
        }

        private void DrawActionFields(SerializedProperty node, string selfId)
        {
            var actionType = node.FindPropertyRelative("actionType");
            EditorGUILayout.PropertyField(actionType, new GUIContent("Action Type"));

            var type = (DialogueActionType)actionType.enumValueIndex;
            if (type == DialogueActionType.SetFlag)
            {
                EditorGUILayout.PropertyField(node.FindPropertyRelative("actionFlagName"), new GUIContent("Flag Name"));
                EditorGUILayout.PropertyField(node.FindPropertyRelative("actionFlagValue"), new GUIContent("Value To Set"));
            }
            else
            {
                EditorGUILayout.PropertyField(node.FindPropertyRelative("actionScriptId"), new GUIContent("Action ID"));
            }

            var choices = node.FindPropertyRelative("choices");
            if (choices.arraySize == 0) choices.InsertArrayElementAtIndex(0);
            _linkPicker.DrawNextDropdown(choices.GetArrayElementAtIndex(0), "Next", selfId);
        }
        #endregion
    }
}