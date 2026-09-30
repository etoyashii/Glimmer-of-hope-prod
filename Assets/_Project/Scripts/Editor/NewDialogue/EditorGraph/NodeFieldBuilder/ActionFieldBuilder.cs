using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GlimmerOfHope.Editor.NewDialogue
{
    public class ActionFieldBuilder : DialogueNodeFieldBuilderBase
    {
        #region Private Fields
        private TextField _flagNameField;
        private Toggle _flagValueField;
        private TextField _scriptIdField;
        #endregion

        #region Public Methods
        public ActionFieldBuilder(DialogueNodeBase node, VisualElement container, System.Action markDirty, System.Action<string> setTitle)
            : base(node, container, markDirty, setTitle) { }


        /// <summary>
        /// Builds and appends the UI fields of the Action node type into Container />.
        /// </summary>
        public override void Build()
        {
            var node = (ActionNode)Node;

            var typeField = new EnumField("Action Type", node.actionType);
            Container.Add(typeField);

            // Fields used when actionType == SetFlag
            _flagNameField = new TextField("Flag Name") { value = node.actionFlagName };
            _flagNameField.RegisterValueChangedCallback(evt => { node.actionFlagName = evt.newValue; MarkDirty(); });

            _flagValueField = new Toggle("Value To Set") { value = node.actionFlagValue };
            _flagValueField.RegisterValueChangedCallback(evt => { node.actionFlagValue = evt.newValue; MarkDirty(); });

            // Field used for other action types
            _scriptIdField = new TextField("Action ID") { value = node.actionScriptId };
            _scriptIdField.RegisterValueChangedCallback(evt => { node.actionScriptId = evt.newValue; MarkDirty(); });

            Container.Add(_flagNameField);
            Container.Add(_flagValueField);
            Container.Add(_scriptIdField);

            UpdateVisibility(node.actionType);

            // Update node type and refresh visibility on change
            typeField.RegisterValueChangedCallback(evt =>
            {
                var newType = (DialogueActionType)evt.newValue;
                node.actionType = newType;
                UpdateVisibility(newType);
                MarkDirty();
            });
        }
        #endregion

        #region private Methods
        // Shows flag fields for SetFlag, script field otherwise
        private void UpdateVisibility(DialogueActionType type)
        {
            bool isSetFlag = type == DialogueActionType.SetFlag;
            _flagNameField.style.display = isSetFlag ? DisplayStyle.Flex : DisplayStyle.None;
            _flagValueField.style.display = isSetFlag ? DisplayStyle.Flex : DisplayStyle.None;
            _scriptIdField.style.display = isSetFlag ? DisplayStyle.None : DisplayStyle.Flex;
        }
        #endregion
    }
}