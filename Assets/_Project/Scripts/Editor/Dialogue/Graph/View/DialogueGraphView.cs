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

        #region Events

        public event System.Action<DialogueLineNode> LineNodeCreated;
        public event System.Action<DialogueLineNode> LineNodeChanged;

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

        public void AddLineNode(DialogueLineNode node)
        {
            node.ContentChanged += OnLineNodeContentChanged;
            AddElement(node);
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
            if (_currentConversation == null)
            {
                EditorUtility.DisplayDialog(
                    "Aucune conversation",
                    "Chargez une conversation avant d'ajouter une ligne.",
                    "OK");
                return null;
            }

            var usedIds = new HashSet<string>();
            foreach (var existing in GetLineNodes())
                usedIds.Add(existing.LineId);

            var lineId = DialogueLineAssetFactory.GenerateUniqueLineId(
                _currentConversation.ConversationId, usedIds);

            var lineSO = DialogueLineAssetFactory.Create(_currentConversation, lineId, usedIds.Count + 1);
            if (lineSO == null)
                return null;

            var node = NodeFactory.CreateFromSO(lineSO, new Dictionary<string, string>());
            node.SetPosition(new Rect(position, Vector2.zero));
            AddLineNode(node);

            ClearSelection();
            AddToSelection(node);
            LineNodeCreated?.Invoke(node);

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

        private void OnLineNodeContentChanged(DialogueLineNode node)
        {
            LineNodeChanged?.Invoke(node);
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

            if (change.elementsToRemove != null)
                DeleteUnsavedLineAssets(change.elementsToRemove);

            return change;
        }

        private void DeleteUnsavedLineAssets(List<GraphElement> elements)
        {
            foreach (var element in elements)
            {
                if (element is not DialogueLineNode node || node.LineSO == null)
                    continue;

                if (IsReferencedByConversation(node.LineSO))
                    continue;

                var path = AssetDatabase.GetAssetPath(node.LineSO);
                if (!string.IsNullOrEmpty(path))
                    AssetDatabase.DeleteAsset(path);
            }
        }

        private bool IsReferencedByConversation(DialogueLineSO lineSO)
        {
            if (_currentConversation == null || _currentConversation.AllLines == null)
                return false;

            foreach (var line in _currentConversation.AllLines)
            {
                if (line == lineSO)
                    return true;
            }

            return false;
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
