using System;
using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEngine.UIElements;
namespace GlimmerOfHope.Editor.NewDialogue
{
    public static class DialogueNodeFieldBuilderFactory
    {
        #region Public Methods
        /// <summary>
        /// Returns the field builder matching the node's type, or null if none exists.
        /// </summary>
        public static DialogueNodeFieldBuilderBase Create(DialogueNodeBase node, VisualElement container, Action markDirty, Action<string> setTitle)
        {
            return node.NodeType switch
            {
                DialogueNodeType.Dialogue => new DialogueFieldBuilder(node, container, markDirty, setTitle),
                DialogueNodeType.Condition => new ConditionFieldBuilder(node, container, markDirty, setTitle),
                DialogueNodeType.Action => new ActionFieldBuilder(node, container, markDirty, setTitle),
                DialogueNodeType.Gate => new GateFieldBuilder(node, container, markDirty, setTitle),
                DialogueNodeType.Start => new StartFieldBuilder(node, container, markDirty, setTitle),
                DialogueNodeType.End => new EndFieldBuilder(node, container, markDirty, setTitle),
                _ => null
            };
        }
        #endregion
    }
}