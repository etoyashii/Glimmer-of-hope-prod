using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GlimmerOfHope.Editor.NewDialogue
{
    public class GateFieldBuilder : DialogueNodeFieldBuilderBase
    {
        public GateFieldBuilder(DialogueNode node, VisualElement container, System.Action markDirty, System.Action<string> setTitle)
            : base(node, container, markDirty, setTitle) { }

        public override void Build()
        {
            var typeField = new EnumField("Unlocked By", Node.gateTriggerType);
            Container.Add(typeField);

            var eventIdField = new TextField("Event ID") { value = Node.gateEventId };
            eventIdField.RegisterValueChangedCallback(evt => { Node.gateEventId = evt.newValue; MarkDirty(); });

            var timerField = new FloatField("Seconds") { value = Node.gateTimerSeconds };
            timerField.RegisterValueChangedCallback(evt => { Node.gateTimerSeconds = evt.newValue; MarkDirty(); });

            var flagNameField = new TextField("Flag Name") { value = Node.gateFlagName };
            flagNameField.RegisterValueChangedCallback(evt => { Node.gateFlagName = evt.newValue; MarkDirty(); });

            var flagValueField = new Toggle("Expected Value") { value = Node.gateFlagExpectedValue };
            flagValueField.RegisterValueChangedCallback(evt => { Node.gateFlagExpectedValue = evt.newValue; MarkDirty(); });

            Container.Add(eventIdField);
            Container.Add(timerField);
            Container.Add(flagNameField);
            Container.Add(flagValueField);

            void UpdateVisibility(DialogueGateTriggerType type)
            {
                eventIdField.style.display = type == DialogueGateTriggerType.ScriptEvent ? DisplayStyle.Flex : DisplayStyle.None;
                timerField.style.display = type == DialogueGateTriggerType.Timer ? DisplayStyle.Flex : DisplayStyle.None;
                bool isFlag = type == DialogueGateTriggerType.Flag;
                flagNameField.style.display = isFlag ? DisplayStyle.Flex : DisplayStyle.None;
                flagValueField.style.display = isFlag ? DisplayStyle.Flex : DisplayStyle.None;
            }

            UpdateVisibility(Node.gateTriggerType);

            typeField.RegisterValueChangedCallback(evt =>
            {
                var newType = (DialogueGateTriggerType)evt.newValue;
                Node.gateTriggerType = newType;
                UpdateVisibility(newType);
                MarkDirty();
            });
        }
    }
}
