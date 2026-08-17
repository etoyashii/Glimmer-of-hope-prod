using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GlimmerOfHope.Editor.NewDialogue
{
    public class ConditionFieldBuilder : DialogueNodeFieldBuilderBase
    {
        public ConditionFieldBuilder(DialogueNode node, VisualElement container, System.Action markDirty, System.Action<string> setTitle)
            : base(node, container, markDirty, setTitle) { }

        public override void Build()
        {
            var typeField = new EnumField("Condition Type", Node.conditionType);
            Container.Add(typeField);

            var flagNameField = new TextField("Flag Name") { value = Node.conditionFlagName };
            flagNameField.RegisterValueChangedCallback(evt => { Node.conditionFlagName = evt.newValue; MarkDirty(); });

            var flagValueField = new Toggle("Expected Value") { value = Node.conditionExpectedValue };
            flagValueField.RegisterValueChangedCallback(evt => { Node.conditionExpectedValue = evt.newValue; MarkDirty(); });

            var scriptIdField = new TextField("Condition ID") { value = Node.conditionScriptId };
            scriptIdField.RegisterValueChangedCallback(evt => { Node.conditionScriptId = evt.newValue; MarkDirty(); });

            Container.Add(flagNameField);
            Container.Add(flagValueField);
            Container.Add(scriptIdField);

            void UpdateVisibility(DialogueConditionType type)
            {
                bool isFlag = type == DialogueConditionType.Flag;
                flagNameField.style.display = isFlag ? DisplayStyle.Flex : DisplayStyle.None;
                flagValueField.style.display = isFlag ? DisplayStyle.Flex : DisplayStyle.None;
                scriptIdField.style.display = isFlag ? DisplayStyle.None : DisplayStyle.Flex;
            }

            UpdateVisibility(Node.conditionType);

            typeField.RegisterValueChangedCallback(evt =>
            {
                var newType = (DialogueConditionType)evt.newValue;
                Node.conditionType = newType;
                UpdateVisibility(newType);
                MarkDirty();
            });
        }
    }
}
