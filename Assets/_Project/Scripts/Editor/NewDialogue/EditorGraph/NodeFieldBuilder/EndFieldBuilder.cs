using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEngine.UIElements;
namespace GlimmerOfHope.Editor.NewDialogue
{
    public class EndFieldBuilder : DialogueNodeFieldBuilderBase
    {
        #region Public Methods
        public EndFieldBuilder(DialogueNodeBase node, VisualElement container, System.Action markDirty, System.Action<string> setTitle)
            : base(node, container, markDirty, setTitle) { }
        /// <summary>
        /// Builds and appends the UI fields of the End node type into Container.
        /// </summary>
        public override void Build()
        {
            var node = (EndNode)Node;
            var outcomeField = new TextField("Outcome ID") { value = node.outcomeId };
            outcomeField.RegisterValueChangedCallback(evt => { node.outcomeId = evt.newValue; MarkDirty(); });
            Container.Add(outcomeField);
        }
        #endregion
    }
}