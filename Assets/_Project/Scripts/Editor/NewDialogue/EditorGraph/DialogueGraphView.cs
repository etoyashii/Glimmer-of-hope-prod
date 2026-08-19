using GlimmerOfHope.Gameplay.NewDialogue;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace GlimmerOfHope.Editor.NewDialogue
{
    public class DialogueGraphView : GraphView
    {
        #region Private Fields
        private readonly DialogueGraph _graph;
        private readonly Dictionary<string, List<Port>> _outputPorts = new Dictionary<string, List<Port>>();
        private readonly Dictionary<string, Port> _inputPorts = new Dictionary<string, Port>();
        #endregion

        #region Public Methods
        public DialogueGraphView(DialogueGraph graph)
        {
            _graph = graph;

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/_Project/Scripts/Editor/NewDialogue/EditorGraph/DialogueGridView.uss");
            if (styleSheet != null) styleSheets.Add(styleSheet);

            graphViewChanged += OnGraphViewChanged;

            PopulateView();
        }

        // Only allow connecting output <-> input ports on different nodes
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(port =>
                port.direction != startPort.direction &&
                port.node != startPort.node
            ).ToList();
        }

        /// <summary>
        /// Creates a new node of the given type at the given position and adds it to the graph.
        /// </summary>
        public void CreateNewNode(Vector2 localPosition, DialogueNodeType type, bool hasChoices = false)
        {
            DialogueNodeBase newNode = type switch
            {
                DialogueNodeType.Dialogue => new DialogueLineNode { hasChoices = hasChoices },
                DialogueNodeType.Start => new StartNode(),
                DialogueNodeType.End => new EndNode(),
                DialogueNodeType.Gate => new GateNode(),
                DialogueNodeType.Condition => new ConditionNode(),
                DialogueNodeType.Action => new ActionNode(),
                _ => null
            };
            if (newNode == null) return;

            newNode.nodeId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
            newNode.editorPosition = localPosition;

            InitializeDefaultChoices(newNode, hasChoices);

            _graph.TypedNodes.Add(newNode);
            EditorUtility.SetDirty(_graph);

            PopulateView();
        }
        #endregion

        #region Private Methods
        // Rebuilds the entire view from the graph asset: clears existing elements,
        // recreates node views, then reconnects edges (nodes must all exist before linking).
        private void PopulateView()
        {
            foreach (var element in graphElements.ToList())
                RemoveElement(element);

            _outputPorts.Clear();
            _inputPorts.Clear();

            if (_graph == null) return;

            foreach (var node in _graph.TypedNodes)
                CreateNodeView(node);

            foreach (var node in _graph.TypedNodes)
                ConnectExistingLink(node);
        }

        private void CreateNodeView(DialogueNodeBase dialogueNode)
        {
            var node = new DialogueNodeView(dialogueNode, _graph);

            node.SetPosition(new Rect(dialogueNode.editorPosition, new Vector2(150, 100)));
            AddElement(node);

            if (node.InputPort != null)
                _inputPorts[dialogueNode.nodeId] = node.InputPort;

            _outputPorts[dialogueNode.nodeId] = node.OutputPorts;
        }

        // Draws an edge for each choice that already points to a valid target node
        private void ConnectExistingLink(DialogueNodeBase dialogueNode)
        {
            if (!_outputPorts.TryGetValue(dialogueNode.nodeId, out var outputPorts)) return;

            for (int i = 0; i < dialogueNode.choices.Count; i++)
            {
                string targetId = dialogueNode.choices[i].nextNodeId;
                if (string.IsNullOrEmpty(targetId)) continue;
                if (!_inputPorts.TryGetValue(targetId, out var inputPort)) continue;

                var edge = outputPorts[i].ConnectTo(inputPort);
                AddElement(edge);
            }
        }


        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (change.edgesToCreate != null)
                foreach (var edge in change.edgesToCreate)
                    HandleEdgeCreated(edge);

            if (change.elementsToRemove != null)
            {
                // Start is unique and required: prevent any attempt to delete it before processing the list.
                change.elementsToRemove.RemoveAll(element =>
                    element is DialogueNodeView nodeView && nodeView.DialogueNode is StartNode);

                foreach (var element in change.elementsToRemove)
                {
                    if (element is Edge edge)
                        HandleEdgeRemoved(edge);
                    else if (element is DialogueNodeView nodeView)
                        _graph.TypedNodes.Remove(nodeView.DialogueNode);
                }
            }

            EditorUtility.SetDirty(_graph);
            return change;
        }

        // Syncs the underlying choice's nextNodeId when the user draws a new edge
        private void HandleEdgeCreated(Edge edge)
        {
            if (edge.output.userData is DialogueChoice choice && edge.input.node is DialogueNodeView targetView)
                choice.nextNodeId = targetView.DialogueNode.nodeId;
        }

        // Clears the underlying choice's nextNodeId when the user deletes an edge
        private void HandleEdgeRemoved(Edge edge)
        {
            if (edge.output.userData is DialogueChoice choice)
                choice.nextNodeId = "";
        }

       

        // Seeds each node type with its default choice slots 
        private static void InitializeDefaultChoices(DialogueNodeBase node, bool hasChoices)
        {
            switch (node)
            {
                case DialogueLineNode:
                    int count = hasChoices ? 2 : 1;
                    for (int i = 0; i < count; i++)
                        node.choices.Add(new DialogueChoice { choiceText = "", nextNodeId = "" });
                    break;
                case GateNode:
                case ActionNode:
                    node.choices.Add(new DialogueChoice { choiceText = "", nextNodeId = "" });
                    break;
                case ConditionNode:
                    node.choices.Add(new DialogueChoice { choiceText = "", nextNodeId = "" });
                    node.choices.Add(new DialogueChoice { choiceText = "", nextNodeId = "" });
                    break;
            }
        }
        #endregion
    }
}