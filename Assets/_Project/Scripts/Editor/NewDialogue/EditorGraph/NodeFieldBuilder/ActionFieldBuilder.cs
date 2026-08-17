using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace GlimmerOfHope.Editor.NewDialogue
{
    public class ActionFieldBuilder : DialogueNodeFieldBuilderBase
    {
        public ActionFieldBuilder(DialogueNode node, VisualElement container, System.Action markDirty, System.Action<string> setTitle)
            : base(node, container, markDirty, setTitle) { }

        public override void Build()
        {
            var typeField = new EnumField("Action Type", Node.actionType);
            Container.Add(typeField);

            var flagNameField = new TextField("Flag Name") { value = Node.actionFlagName };
            flagNameField.RegisterValueChangedCallback(evt => { Node.actionFlagName = evt.newValue; MarkDirty(); });

            var flagValueField = new Toggle("Value To Set") { value = Node.actionFlagValue };
            flagValueField.RegisterValueChangedCallback(evt => { Node.actionFlagValue = evt.newValue; MarkDirty(); });

            var scriptIdField = new TextField("Action ID") { value = Node.actionScriptId };
            scriptIdField.RegisterValueChangedCallback(evt => { Node.actionScriptId = evt.newValue; MarkDirty(); });

            Container.Add(flagNameField);
            Container.Add(flagValueField);
            Container.Add(scriptIdField);

            void UpdateVisibility(DialogueActionType type)
            {
                bool isSetFlag = type == DialogueActionType.SetFlag;
                flagNameField.style.display = isSetFlag ? DisplayStyle.Flex : DisplayStyle.None;
                flagValueField.style.display = isSetFlag ? DisplayStyle.Flex : DisplayStyle.None;
                scriptIdField.style.display = isSetFlag ? DisplayStyle.None : DisplayStyle.Flex;
            }

            UpdateVisibility(Node.actionType);

            typeField.RegisterValueChangedCallback(evt =>
            {
                var newType = (DialogueActionType)evt.newValue;
                Node.actionType = newType;
                UpdateVisibility(newType);
                MarkDirty();
            });
        }
    }
}
