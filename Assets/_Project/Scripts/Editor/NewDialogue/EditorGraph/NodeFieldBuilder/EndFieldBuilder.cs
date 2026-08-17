using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEngine.UIElements;

namespace GlimmerOfHope.Editor.NewDialogue
{
    public class EndFieldBuilder : DialogueNodeFieldBuilderBase
    {
        public EndFieldBuilder(DialogueNode node, VisualElement container, System.Action markDirty, System.Action<string> setTitle)
            : base(node, container, markDirty, setTitle) { }

        public override void Build()
        {
            var outcomeField = new TextField("Outcome ID") { value = Node.outcomeId };
            outcomeField.RegisterValueChangedCallback(evt => { Node.outcomeId = evt.newValue; MarkDirty(); });
            Container.Add(outcomeField);
        }
    }
}
