using GlimmerOfHope.Gameplay.NewDialogue;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;

namespace GlimmerOfHope.Editor.NewDialogue
{
    public class DialogueGraphView : GraphView
    {

        private readonly DialogueGraph _graph;
        private readonly Dictionary<string, List<Port>> _outputPorts = new Dictionary<string, List<Port>>();
        private readonly Dictionary<string, Port> _inputPorts = new Dictionary<string, Port>();



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
        private void PopulateView()
        {
            foreach (var element in graphElements.ToList()) 
            { 
                RemoveElement(element);
            }

            _outputPorts.Clear();
            _inputPorts.Clear();

            if (_graph == null) return;

            foreach (var node in _graph.nodes)
                CreateNodeView(node);

            foreach (var node in _graph.nodes)
                ConnectExistingLink(node);

        }
        private void CreateNodeView(DialogueNode dialogueNode)
        {
            var node = new DialogueNodeView(dialogueNode, _graph);

            node.SetPosition(new Rect(dialogueNode.editorPosition, new Vector2(150, 100)));
            AddElement(node);

            if (node.InputPort != null)
                _inputPorts[dialogueNode.nodeId] = node.InputPort;

            _outputPorts[dialogueNode.nodeId] = node.OutputPorts;
        }

        private void ConnectExistingLink(DialogueNode dialogueNode)
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
        private string BuildTitle(DialogueNode dialogueNode)
        {
            if (dialogueNode.nodeType == DialogueNodeType.Dialogue)
                return string.IsNullOrEmpty(dialogueNode.speakerId) ? "(no speaker)" : dialogueNode.speakerId;

            return dialogueNode.nodeType.ToString().ToUpper();
        }
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(port =>
                port.direction != startPort.direction &&
                port.node != startPort.node
            ).ToList();
        }
        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (change.edgesToCreate != null)
                foreach (var edge in change.edgesToCreate)
                    HandleEdgeCreated(edge);

            if (change.elementsToRemove != null)
            {
                // Start est unique et obligatoire : on retire toute tentative de le supprimer AVANT de traiter la liste.
                change.elementsToRemove.RemoveAll(element =>
                    element is DialogueNodeView nodeView && nodeView.DialogueNode.nodeType == DialogueNodeType.Start);

                foreach (var element in change.elementsToRemove)
                {
                    if (element is Edge edge)
                        HandleEdgeRemoved(edge);
                    else if (element is DialogueNodeView nodeView)
                        _graph.nodes.Remove(nodeView.DialogueNode);
                }
            }

            EditorUtility.SetDirty(_graph);
            return change;
        }

        private void HandleEdgeCreated(Edge edge)
        {
            if (edge.output.userData is DialogueChoice choice && edge.input.node is DialogueNodeView targetView)
                choice.nextNodeId = targetView.DialogueNode.nodeId;
        }

        private void HandleEdgeRemoved(Edge edge)
        {
            if (edge.output.userData is DialogueChoice choice)
                choice.nextNodeId = "";
        }

        public void CreateNewNode(Vector2 localPosition, DialogueNodeType type, bool hasChoices = false)
        {
            var newNode = new DialogueNode
            {
                nodeId = System.Guid.NewGuid().ToString("N").Substring(0, 8),
                nodeType = type,
                hasChoices = hasChoices,
                editorPosition = localPosition
            };

            InitializeDefaultChoices(newNode);

            _graph.nodes.Add(newNode);
            EditorUtility.SetDirty(_graph);

            PopulateView();
        }

        private static void InitializeDefaultChoices(DialogueNode node)
        {
            switch (node.nodeType)
            {
                case DialogueNodeType.Dialogue:
                    int count = node.hasChoices ? 2 : 1;
                    for (int i = 0; i < count; i++)
                        node.choices.Add(new DialogueChoice { choiceText = "", nextNodeId = "" });
                    break;
                case DialogueNodeType.Gate:
                case DialogueNodeType.Action:
                    node.choices.Add(new DialogueChoice { choiceText = "", nextNodeId = "" });
                    break;
                case DialogueNodeType.Condition:
                    node.choices.Add(new DialogueChoice { choiceText = "", nextNodeId = "" });
                    node.choices.Add(new DialogueChoice { choiceText = "", nextNodeId = "" });
                    break;
            }
        }
    }
}