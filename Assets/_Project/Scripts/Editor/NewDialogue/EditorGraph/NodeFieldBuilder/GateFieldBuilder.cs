using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
namespace GlimmerOfHope.Editor.NewDialogue
{
    public class GateFieldBuilder : DialogueNodeFieldBuilderBase
    {
        #region Private Fields
        private TextField _eventIdField;
        private FloatField _timerField;
        private TextField _flagNameField;
        private Toggle _flagValueField;
        #endregion

        #region Public Methods
        public GateFieldBuilder(DialogueNodeBase node, VisualElement container, System.Action markDirty, System.Action<string> setTitle)
            : base(node, container, markDirty, setTitle) { }
        
        /// <summary>
        /// Builds and appends the UI fields of the Gate node type into Container.
        /// </summary>
        public override void Build()
        {
            var node = (GateNode)Node;
            var typeField = new EnumField("Unlocked By", node.gateTriggerType);
            Container.Add(typeField);
            
            // Field used when gateTriggerType == ScriptEvent
            _eventIdField = new TextField("Event ID") { value = node.gateEventId };
            _eventIdField.RegisterValueChangedCallback(evt => { node.gateEventId = evt.newValue; MarkDirty(); });
            
            // Field used when gateTriggerType == Timer
            _timerField = new FloatField("Seconds") { value = node.gateTimerSeconds };
            _timerField.RegisterValueChangedCallback(evt => { node.gateTimerSeconds = evt.newValue; MarkDirty(); });
           
            // Fields used when gateTriggerType == Flag
            _flagNameField = new TextField("Flag Name") { value = node.gateFlagName };
            _flagNameField.RegisterValueChangedCallback(evt => { node.gateFlagName = evt.newValue; MarkDirty(); });
            _flagValueField = new Toggle("Expected Value") { value = node.gateFlagExpectedValue };
            _flagValueField.RegisterValueChangedCallback(evt => { node.gateFlagExpectedValue = evt.newValue; MarkDirty(); });
           
            Container.Add(_eventIdField);
            Container.Add(_timerField);
            Container.Add(_flagNameField);
            Container.Add(_flagValueField);
            
            UpdateVisibility(node.gateTriggerType);
            
            // Update node type and refresh visibility on change
            typeField.RegisterValueChangedCallback(evt =>
            {
                var newType = (DialogueGateTriggerType)evt.newValue;
                node.gateTriggerType = newType;
                UpdateVisibility(newType);
                MarkDirty();
            });
        }
        #endregion

        #region private Methods
        // Shows only the field(s) relevant to the selected trigger type
        private void UpdateVisibility(DialogueGateTriggerType type)
        {
            _eventIdField.style.display = type == DialogueGateTriggerType.ScriptEvent ? DisplayStyle.Flex : DisplayStyle.None;
            _timerField.style.display = type == DialogueGateTriggerType.Timer ? DisplayStyle.Flex : DisplayStyle.None;
            bool isFlag = type == DialogueGateTriggerType.Flag;
            _flagNameField.style.display = isFlag ? DisplayStyle.Flex : DisplayStyle.None;
            _flagValueField.style.display = isFlag ? DisplayStyle.Flex : DisplayStyle.None;
        }
        #endregion
    }
}