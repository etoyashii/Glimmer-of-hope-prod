using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
namespace GlimmerOfHope.Editor.NewDialogue
{
    public class ConditionFieldBuilder : DialogueNodeFieldBuilderBase
    {
        #region Private Fields
        private TextField _flagNameField;
        private Toggle _flagValueField;
        private TextField _scriptIdField;
        #endregion

        #region Public Methods
        public ConditionFieldBuilder(DialogueNodeBase node, VisualElement container, System.Action markDirty, System.Action<string> setTitle)
            : base(node, container, markDirty, setTitle) { }
        /// <summary>
        /// Builds and appends the UI fields of the Condition node type into Container.
        /// </summary>
        public override void Build()
        {
            var node = (ConditionNode)Node;
            var typeField = new EnumField("Condition Type", node.conditionType);
            Container.Add(typeField);
            // Fields used when conditionType == Flag
            _flagNameField = new TextField("Flag Name") { value = node.conditionFlagName };
            _flagNameField.RegisterValueChangedCallback(evt => { node.conditionFlagName = evt.newValue; MarkDirty(); });
            _flagValueField = new Toggle("Expected Value") { value = node.conditionExpectedValue };
            _flagValueField.RegisterValueChangedCallback(evt => { node.conditionExpectedValue = evt.newValue; MarkDirty(); });
            // Field used for other condition types
            _scriptIdField = new TextField("Condition ID") { value = node.conditionScriptId };
            _scriptIdField.RegisterValueChangedCallback(evt => { node.conditionScriptId = evt.newValue; MarkDirty(); });
            Container.Add(_flagNameField);
            Container.Add(_flagValueField);
            Container.Add(_scriptIdField);
            UpdateVisibility(node.conditionType);
            // Update node type and refresh visibility on change
            typeField.RegisterValueChangedCallback(evt =>
            {
                var newType = (DialogueConditionType)evt.newValue;
                node.conditionType = newType;
                UpdateVisibility(newType);
                MarkDirty();
            });
        }
        #endregion

        #region private Methods
        // Shows flag fields for Flag, script field otherwise
        private void UpdateVisibility(DialogueConditionType type)
        {
            bool isFlag = type == DialogueConditionType.Flag;
            _flagNameField.style.display = isFlag ? DisplayStyle.Flex : DisplayStyle.None;
            _flagValueField.style.display = isFlag ? DisplayStyle.Flex : DisplayStyle.None;
            _scriptIdField.style.display = isFlag ? DisplayStyle.None : DisplayStyle.Flex;
        }
        #endregion
    }
}