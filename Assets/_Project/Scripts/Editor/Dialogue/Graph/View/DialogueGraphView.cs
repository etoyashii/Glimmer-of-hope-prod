using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using GlimmerOfHope.Gameplay.Dialogue;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public class DialogueGraphView : GraphView
    {
        #region Private Fields

        private ConversationSO _currentConversation;
        private GraphSerializer _serializer;
        private string _activeLanguage = "fr";

        #endregion

        #region Properties

        public ConversationSO CurrentConversation => _currentConversation;
        public string ActiveLanguage => _activeLanguage;

        #endregion

        #region Constructor

        public DialogueGraphView()
        {
            _serializer = new GraphSerializer(this);

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            LoadStyleSheet();
            AddGridBackground();

            graphViewChanged += OnGraphViewChanged;
        }

        #endregion

        #region Public Methods

        public void LoadConversation(ConversationSO conversation)
        {
            _currentConversation = conversation;
            _serializer.LoadConversation(conversation);
        }

        public bool SaveConversation()
        {
            return _serializer.SaveConversation(_currentConversation);
        }

        public void ClearGraph()
        {
            foreach (var element in graphElements.ToList())
                RemoveElement(element);
        }

        public void SetActiveLanguage(string language)
        {
            _activeLanguage = language;
            RefreshAllNodeTexts();
        }

        public Edge CreateEdge(Port output, Port input)
        {
            var edge = new Edge { output = output, input = input };
            edge.output.Connect(edge);
            edge.input.Connect(edge);
            AddElement(edge);
            return edge;
        }

        public DialogueLineNode AddNewLineNode(Vector2 position)
        {
            int nextIndex = GetLineNodes().Count + 1;
            var convId = _currentConversation != null ? _currentConversation.ConversationId : "conv";
            var lineId = NodeFactory.GenerateLineId(convId, nextIndex);

            var node = NodeFactory.CreateEmpty(lineId, position);
            AddElement(node);
            return node;
        }

        public ConversationEndNode AddEndNode(Vector2 position)
        {
            var existing = GetEndNode();
            if (existing != null)
                return existing;

            var node = NodeFactory.CreateEndNode();
            node.SetPosition(new Rect(position, Vector2.zero));
            AddElement(node);
            return node;
        }

        #endregion

        #region Query Methods

        public ConversationStartNode GetStartNode()
        {
            return nodes.OfType<ConversationStartNode>().FirstOrDefault();
        }

        public ConversationEndNode GetEndNode()
        {
            return nodes.OfType<ConversationEndNode>().FirstOrDefault();
        }

        public List<DialogueLineNode> GetLineNodes()
        {
            return nodes.OfType<DialogueLineNode>().ToList();
        }

        public DialogueLineNode GetLineNodeById(string lineId)
        {
            return nodes.OfType<DialogueLineNode>().FirstOrDefault(n => n.LineId == lineId);
        }

        #endregion

        #region Overrides

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.Where(p =>
                p.direction != startPort.direction &&
                p.node != startPort.node &&
                p.portType == startPort.portType)
                .ToList();
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            var mousePos = viewTransform.matrix.inverse.MultiplyPoint(evt.localMousePosition);

            evt.menu.AppendAction("Nouvelle Ligne", _ => AddNewLineNode(mousePos));
            evt.menu.AppendAction("Noeud Fin", _ => AddEndNode(mousePos));
            evt.menu.AppendSeparator();

            base.BuildContextualMenu(evt);
        }

        #endregion

        #region Private Methods

        private void LoadStyleSheet()
        {
            var guids = AssetDatabase.FindAssets("DialogueGraph t:StyleSheet");

            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);

                if (styleSheet != null)
                    styleSheets.Add(styleSheet);
            }
        }

        private void AddGridBackground()
        {
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (change.edgesToCreate != null)
            {
                foreach (var edge in change.edgesToCreate)
                    edge.AddToClassList("dialogue-edge");
            }

            return change;
        }

        private void RefreshAllNodeTexts()
        {
            foreach (var node in GetLineNodes())
            {
                node.UpdateVisuals(_activeLanguage);
            }
        }

        #endregion
    }
}
