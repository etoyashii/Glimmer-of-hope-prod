using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEngine.UIElements;
namespace GlimmerOfHope.Editor.NewDialogue
{
    public class StartFieldBuilder : DialogueNodeFieldBuilderBase
    {
        #region Private Fields
        private Vector3Field _buttonOffsetField;
        private FloatField _zoneRadiusField;
        #endregion

        #region Public Methods
        public StartFieldBuilder(DialogueNodeBase node, VisualElement container, System.Action markDirty, System.Action<string> setTitle)
            : base(node, container, markDirty, setTitle) { }
       
        /// <summary>
        /// Builds and appends the UI fields of the Start node type into Container.
        /// </summary>
        public override void Build()
        {
            var node = (StartNode)Node;
            var typeField = new EnumField("Trigger", node.triggerType);
            Container.Add(typeField);
            // Field used when triggerType == FloatingButton
            _buttonOffsetField = new Vector3Field("Button Offset") { value = node.buttonOffset };
            _buttonOffsetField.RegisterValueChangedCallback(evt => { node.buttonOffset = evt.newValue; MarkDirty(); });
            // Field used when triggerType == TriggerZone
            _zoneRadiusField = new FloatField("Zone Radius") { value = node.triggerZoneRadius };
            _zoneRadiusField.RegisterValueChangedCallback(evt => { node.triggerZoneRadius = evt.newValue; MarkDirty(); });
            Container.Add(_buttonOffsetField);
            Container.Add(_zoneRadiusField);
            UpdateVisibility(node.triggerType);
            // Update node type and refresh visibility on change
            typeField.RegisterValueChangedCallback(evt =>
            {
                var newType = (DialogueTriggerType)evt.newValue;
                node.triggerType = newType;
                UpdateVisibility(newType);
                MarkDirty();
            });
        }
        #endregion

        #region private Methods
        // Shows only the field relevant to the selected trigger type
        private void UpdateVisibility(DialogueTriggerType type)
        {
            _buttonOffsetField.style.display = type == DialogueTriggerType.FloatingButton ? DisplayStyle.Flex : DisplayStyle.None;
            _zoneRadiusField.style.display = type == DialogueTriggerType.TriggerZone ? DisplayStyle.Flex : DisplayStyle.None;
        }
        #endregion
    }
}