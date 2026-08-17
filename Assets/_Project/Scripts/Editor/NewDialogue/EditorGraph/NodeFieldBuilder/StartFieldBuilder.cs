using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GlimmerOfHope.Editor.NewDialogue
{
    public class StartFieldBuilder : DialogueNodeFieldBuilderBase
    {
        public StartFieldBuilder(DialogueNode node, VisualElement container, System.Action markDirty, System.Action<string> setTitle)
            : base(node, container, markDirty, setTitle) { }

        public override void Build()
        {
            var typeField = new EnumField("Trigger", Node.triggerType);
            Container.Add(typeField);

            var buttonOffsetField = new Vector3Field("Button Offset") { value = Node.buttonOffset };
            buttonOffsetField.RegisterValueChangedCallback(evt => { Node.buttonOffset = evt.newValue; MarkDirty(); });

            var zoneRadiusField = new FloatField("Zone Radius") { value = Node.triggerZoneRadius };
            zoneRadiusField.RegisterValueChangedCallback(evt => { Node.triggerZoneRadius = evt.newValue; MarkDirty(); });

            Container.Add(buttonOffsetField);
            Container.Add(zoneRadiusField);

            void UpdateVisibility(DialogueTriggerType type)
            {
                buttonOffsetField.style.display = type == DialogueTriggerType.FloatingButton ? DisplayStyle.Flex : DisplayStyle.None;
                zoneRadiusField.style.display = type == DialogueTriggerType.TriggerZone ? DisplayStyle.Flex : DisplayStyle.None;
            }

            UpdateVisibility(Node.triggerType);

            typeField.RegisterValueChangedCallback(evt =>
            {
                var newType = (DialogueTriggerType)evt.newValue;
                Node.triggerType = newType;
                UpdateVisibility(newType);
                MarkDirty();
            });
        }
    }
}
