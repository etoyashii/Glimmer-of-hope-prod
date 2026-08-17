using System;
using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEngine.UIElements;

namespace GlimmerOfHope.Editor.NewDialogue
{
    /// <summary>
    /// Base class for a per-type node field builder. One subclass per DialogueNodeType,
    /// each overriding Build() to add exactly the fields relevant to that type.
    /// </summary>
    public abstract class DialogueNodeFieldBuilderBase
    {
        protected readonly DialogueNode Node;
        protected readonly VisualElement Container;
        protected readonly Action MarkDirty;
        protected readonly Action<string> SetTitle;

        protected DialogueNodeFieldBuilderBase(DialogueNode node, VisualElement container, Action markDirty, Action<string> setTitle)
        {
            Node = node;
            Container = container;
            MarkDirty = markDirty;
            SetTitle = setTitle;
        }

        public abstract void Build();
    }
}
