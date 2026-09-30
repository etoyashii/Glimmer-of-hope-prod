using System;
using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEngine.UIElements;

namespace GlimmerOfHope.Editor.NewDialogue
{
    /// <summary>
    /// Base class for a per-type node field builder. One subclass per DialogueNodeType,
    /// each overriding Build() to add the fields of that type.
    /// </summary>
    public abstract class DialogueNodeFieldBuilderBase
    {
        #region Private Fields
        protected readonly DialogueNodeBase Node;
        protected readonly VisualElement Container;
        protected readonly Action MarkDirty;
        protected readonly Action<string> SetTitle;
        #endregion

        #region Private Methods
        protected DialogueNodeFieldBuilderBase(DialogueNodeBase node, VisualElement container, Action markDirty, Action<string> setTitle)
        {
            Node = node;
            Container = container;
            MarkDirty = markDirty;
            SetTitle = setTitle;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Builds and appends the UI fields of this node type into Container />.
        /// </summary>
        public abstract void Build();
        #endregion
    }
}