using System;
using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEngine.UIElements;

namespace GlimmerOfHope.Editor.NewDialogue
{
    public static class DialogueNodeFieldBuilderFactory
    {
        public static DialogueNodeFieldBuilderBase Create(DialogueNode node, VisualElement container, Action markDirty, Action<string> setTitle)
        {
            return node.nodeType switch
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
    }
}
