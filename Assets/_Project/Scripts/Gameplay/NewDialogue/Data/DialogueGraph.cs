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

        public List<DialogueNode> nodes = new List<DialogueNode>();




        public List<DialogueNodeBase> TypedNodes => _typedNodes;

        #endregion

        #region Private Fields

        private Dictionary<string, DialogueNode> _lookup;

        [SerializeField, SerializeReference]
        private List<DialogueNodeBase> _typedNodes = new List<DialogueNodeBase>();
        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (nodes == null) nodes = new List<DialogueNode>();
            if (string.IsNullOrEmpty(graphId)) graphId = GenerateId();

            // Brand new graph (no nodes yet) set up Start and End automatically.
            if (nodes.Count == 0)
                CreateDefaultStartAndEnd();
        }

        #endregion

        #region Public Methods

        //Get a node by ID
        public DialogueNode GetNode(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            if (_lookup == null || _lookup.Count != nodes.Count)
                RebuildLookup();

            _lookup.TryGetValue(id, out var node);
            return node;
        }

        //The Start node (holds the trigger settings)
        public DialogueNode GetStartNode()
        {
            return GetNode(startNodeId);
        }

        /// <summary>
        /// Start has no text or speaker of its own it's just an entry point. This return the actual first line to play.
        /// </summary>
        public DialogueNode GetFirstDialogueNode()
        {
            var start = GetStartNode();
            if (start == null || start.choices.Count == 0) return null;
            return GetNode(start.choices[0].nextNodeId);
        }

        public void SetTypedNodes(List<DialogueNodeBase> newNodes)
        {
            _typedNodes = newNodes;
        }
        #endregion

        #region Private Methods

        private void RebuildLookup()
        {
            _lookup = new Dictionary<string, DialogueNode>();
            foreach (var node in nodes)
            {
                if (string.IsNullOrEmpty(node.nodeId)) continue;
                _lookup[node.nodeId] = node;
            }
        }

        private void CreateDefaultStartAndEnd()
        {
            var start = new DialogueNode
            {
                nodeId = GenerateId(),
                nodeType = DialogueNodeType.Start,
                editorPosition = new Vector2(0f, 200f)
            };
            var end = new DialogueNode
            {
                nodeId = GenerateId(),
                nodeType = DialogueNodeType.End,
                editorPosition = new Vector2(600f, 200f)
            };

            nodes.Add(start);
            nodes.Add(end);
            startNodeId = start.nodeId;
            _lookup = null;
        }

        #endregion

        #region Helpers

        private static string GenerateId() => Guid.NewGuid().ToString("N").Substring(0, 8);

        #endregion

        #region Editor

#if UNITY_EDITOR
        // Assigns a new unique ID to any node left empty OR duplicated.
        private void OnValidate()
        {
            var seenIds = new HashSet<string>();

            foreach (var node in nodes)
            {
                EnsureUniqueId(node, seenIds);
                EnforceLinkCountForType(node);

                if (node.nodeType == DialogueNodeType.Start)
                    startNodeId = node.nodeId;
            }

            _lookup = null;
        }

        private static void EnsureUniqueId(DialogueNode node, HashSet<string> seenIds)
        {
            bool isEmpty = string.IsNullOrEmpty(node.nodeId);
            if (!isEmpty && seenIds.Add(node.nodeId)) return;

            node.nodeId = GenerateId();
            seenIds.Add(node.nodeId);
        }

        private static void EnforceLinkCountForType(DialogueNode node)
        {
            switch (node.nodeType)
            {
                case DialogueNodeType.Start:
                case DialogueNodeType.Gate:
                case DialogueNodeType.Action:
                    if (node.choices.Count == 0)
                        node.choices.Add(new DialogueChoice { choiceText = "", nextNodeId = "" });
                    break;

                case DialogueNodeType.Dialogue when !node.hasChoices:
                    if (node.choices.Count == 0)
                        node.choices.Add(new DialogueChoice { choiceText = "", nextNodeId = "" });
                    else if (node.choices.Count > 1)
                        node.choices.RemoveRange(1, node.choices.Count - 1);
                    node.choices[0].choiceText = "";
                    break;

                case DialogueNodeType.Condition:
                    while (node.choices.Count < 2)
                        node.choices.Add(new DialogueChoice { choiceText = "", nextNodeId = "" });
                    if (node.choices.Count > 2)
                        node.choices.RemoveRange(2, node.choices.Count - 2);
                    break;

                case DialogueNodeType.End:
                    if (node.choices.Count > 0)
                        node.choices.Clear();
                    break;
            }
        }
#endif

        #endregion
    }
}
