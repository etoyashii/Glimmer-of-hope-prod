using System;
using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// Base class for the typed node hierarchy. One subclass per DialogueNodeType
    /// (DialogueLineNode, StartNode, EndNode, GateNode, ConditionNode, ActionNode).
    ///
    /// Requires [SerializeReference] on the list that holds these (see DialogueGraph) since
    /// this is a polymorphic list - plain [Serializable] alone can't tell subclasses apart.
    /// </summary>
    [Serializable]
    public abstract class DialogueNodeBase
    {
        [Tooltip("Unique node ID (auto-generated if left empty).")]
        public string nodeId;

        [Header("What Comes Next")]
        [Tooltip("Choices / links to the next node(s). Empty = end of dialogue. A single choice with no text = automatic continue.")]
        public List<DialogueChoice> choices = new List<DialogueChoice>();

        [HideInInspector]
        public Vector2 editorPosition = new Vector2(100f, 100f);

        public abstract DialogueNodeType NodeType { get; }

        public bool IsStart => NodeType == DialogueNodeType.Start;
        public bool IsEnd => NodeType == DialogueNodeType.End;
        public bool IsGate => NodeType == DialogueNodeType.Gate;
        public bool IsCondition => NodeType == DialogueNodeType.Condition;
        public bool IsAction => NodeType == DialogueNodeType.Action;

        /// <summary>Overridden by DialogueLineNode only - every other type resolves instantly, no "continue" concept.</summary>
        public virtual bool IsSimpleContinuation() => false;

        public bool IsEndOfDialogue()
        {
            return NodeType == DialogueNodeType.End || choices == null || choices.Count == 0;
        }

        /// <summary>Reads the nextNodeId of a given choice, safely handling out-of-range indices.</summary>
        public string GetNextNodeId(int choiceIndex = 0)
        {
            return choiceIndex >= 0 && choiceIndex < choices.Count
                ? choices[choiceIndex].nextNodeId
                : null;
        }
    }
}
