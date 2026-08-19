using System;
using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// A complete dialogue. Right-click in the Project window > Create > Dialogue System > Dialogue Graph.
    /// </summary>
    [CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue System/Dialogue Graph")]
    public class DialogueGraph : ScriptableObject
    {
        #region Serialized Fields

        [Tooltip("Unique ID for this dialogue (auto-generated). Lets you look it up by name from a script without an asset reference.")]
        public string graphId;

        [Tooltip("ID of the Start node (managed automatically, don't edit by hand).")]
        public string startNodeId;

        [SerializeField, SerializeReference]
        private List<DialogueNodeBase> _typedNodes = new List<DialogueNodeBase>();

        #endregion

        #region Public Properties

        public List<DialogueNodeBase> TypedNodes => _typedNodes;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (_typedNodes == null) _typedNodes = new List<DialogueNodeBase>();
            if (string.IsNullOrEmpty(graphId)) graphId = GenerateId();

            // Brand new graph (no nodes yet) — set up Start automatically.
            if (_typedNodes.Count == 0)
                CreateDefaultStart();
        }

        #endregion

        #region Public Methods

        /// <summary>Looks up a node by ID.</summary>
        public DialogueNodeBase GetTypedNode(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var node in _typedNodes)
                if (node.nodeId == id) return node;
            return null;
        }

        /// <summary>The Start node (holds the trigger settings). Unique per graph.</summary>
        public StartNode GetTypedStartNode()
        {
            foreach (var node in _typedNodes)
                if (node is StartNode start) return start;
            return null;
        }

        /// <summary>
        /// Start has no content of its own — it's just an entry point. This follows its single
        /// link to return the actual first node to play.
        /// </summary>
        public DialogueNodeBase GetFirstTypedDialogueNode()
        {
            var start = GetTypedStartNode();
            if (start == null || start.choices.Count == 0) return null;
            return GetTypedNode(start.choices[0].nextNodeId);
        }

        #endregion

        #region Private Methods

        private void CreateDefaultStart()
        {
            var start = new StartNode
            {
                nodeId = GenerateId(),
                editorPosition = new Vector2(0f, 200f)
            };

            _typedNodes.Add(start);
            startNodeId = start.nodeId;
        }
        private static string GenerateId() => Guid.NewGuid().ToString("N").Substring(0, 8);

        #endregion

        #region Editor

#if UNITY_EDITOR
        // Assigns a fresh ID to any node left empty OR duplicated. Also keeps a few structural
        // invariants: Start/Gate/Action always have exactly one link, Condition always has
        // exactly two, End never has any, and a Dialogue node's link count matches hasChoices.
        private void OnValidate()
        {
            var seenIds = new HashSet<string>();

            foreach (var node in _typedNodes)
            {
                EnsureUniqueId(node, seenIds);
                EnforceLinkCountForType(node);

                if (node is StartNode)
                    startNodeId = node.nodeId;
            }
        }

        private static void EnsureUniqueId(DialogueNodeBase node, HashSet<string> seenIds)
        {
            bool isEmpty = string.IsNullOrEmpty(node.nodeId);
            if (!isEmpty && seenIds.Add(node.nodeId)) return;

            node.nodeId = GenerateId();
            seenIds.Add(node.nodeId);
        }

        private static void EnforceLinkCountForType(DialogueNodeBase node)
        {
            switch (node)
            {
                case StartNode:
                case GateNode:
                case ActionNode:
                    if (node.choices.Count == 0)
                        node.choices.Add(new DialogueChoice { choiceText = "", nextNodeId = "" });
                    break;

                case DialogueLineNode lineNode when !lineNode.hasChoices:
                    if (node.choices.Count == 0)
                        node.choices.Add(new DialogueChoice { choiceText = "", nextNodeId = "" });
                    else if (node.choices.Count > 1)
                        node.choices.RemoveRange(1, node.choices.Count - 1);
                    node.choices[0].choiceText = "";
                    break;

                case ConditionNode:
                    while (node.choices.Count < 2)
                        node.choices.Add(new DialogueChoice { choiceText = "", nextNodeId = "" });
                    if (node.choices.Count > 2)
                        node.choices.RemoveRange(2, node.choices.Count - 2);
                    break;

                case EndNode:
                    if (node.choices.Count > 0)
                        node.choices.Clear();
                    break;
            }
        }
#endif

        #endregion
    }
}