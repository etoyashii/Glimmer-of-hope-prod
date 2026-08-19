using System.Collections.Generic;
using System.Linq;
using GlimmerOfHope.Gameplay.NewDialogue;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace GlimmerOfHope.Editor.NewDialogue
{
    public class DialogueNodeView : Node
    {
        #region Public Properties
        public DialogueNodeBase DialogueNode { get; }
        public Port InputPort { get; private set; }
        public List<Port> OutputPorts { get; } = new List<Port>();
        #endregion

        #region Private Fields
        private readonly DialogueGraph _graph;
        private Button _addChoiceButton;
        #endregion

        #region Public Methods
        public DialogueNodeView(DialogueNodeBase dialogueNode, DialogueGraph graph)
        {
            DialogueNode = dialogueNode;
            _graph = graph;
            title = BuildTitle();
            titleContainer.style.backgroundColor = new StyleColor(GetTitleColor(dialogueNode.NodeType));

            BuildInputPort();
            BuildOutputPorts();

            var fieldBuilder = DialogueNodeFieldBuilderFactory.Create(DialogueNode, mainContainer, MarkDirty, newTitle => title = newTitle);
            fieldBuilder?.Build();

            RefreshExpandedState();
            RefreshPorts();
        }

        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            DialogueNode.editorPosition = new Vector2(newPos.xMin, newPos.yMin);
            MarkDirty();
        }
        #endregion

        #region Private Methods
        // Dialogue nodes use their speaker as the title, all other types use their node type name
        private string BuildTitle()
        {
            if (DialogueNode is DialogueLineNode lineNode)
                return string.IsNullOrEmpty(lineNode.speakerId) ? "(no speaker)" : lineNode.speakerId;

            return DialogueNode.NodeType.ToString().ToUpper();
        }

        private static Color GetTitleColor(DialogueNodeType type) => type switch
        {
            DialogueNodeType.Start => new Color(0.18f, 0.45f, 0.2f),
            DialogueNodeType.End => new Color(0.5f, 0.18f, 0.18f),
            DialogueNodeType.Gate => new Color(0.5f, 0.32f, 0.6f),
            DialogueNodeType.Condition => new Color(0.2f, 0.5f, 0.5f),
            DialogueNodeType.Action => new Color(0.55f, 0.4f, 0.15f),
            _ => new Color(0.2f, 0.25f, 0.35f)
        };

        // Start nodes have no input: they're always the entry point
        private void BuildInputPort()
        {
            if (DialogueNode is StartNode) return;

            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "";
            inputContainer.Add(InputPort);
        }

        // End nodes have no outputs. Dialogue nodes with choices get a dynamic
        // list of choice ports plus an "add" button; everything else gets one
        // fixed port per existing choice 
        private void BuildOutputPorts()
        {
            if (DialogueNode is EndNode) return;

            if (DialogueNode is DialogueLineNode lineNode && lineNode.hasChoices)
            {
                foreach (var choice in DialogueNode.choices)
                    AddChoicePort(choice);

                _addChoiceButton = new Button(OnAddChoiceClicked) { text = "+ Choice" };
                outputContainer.Add(_addChoiceButton);
                return;
            }

            bool isCondition = DialogueNode is ConditionNode;
            for (int i = 0; i < DialogueNode.choices.Count; i++)
            {
                var choice = DialogueNode.choices[i];
                var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                port.userData = choice;
                port.portName = isCondition ? (i == 0 ? "True" : "False") : "";

                OutputPorts.Add(port);
                outputContainer.Add(port);
            }
        }

        private void OnAddChoiceClicked()
        {
            var newChoice = new DialogueChoice { choiceText = "", nextNodeId = "" };
            DialogueNode.choices.Add(newChoice);
            AddChoicePort(newChoice);
            RefreshExpandedState();
            RefreshPorts();
            MarkDirty();
        }

        // Builds a choice port with an inline text field (choice label) and remove button,
        // inserted before the "+ Choice" button so it stays at the bottom of the list
        private void AddChoicePort(DialogueChoice choice)
        {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
            port.userData = choice;
            port.portName = "";

            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            var textField = new TextField { value = choice.choiceText };
            textField.style.minWidth = 90;
            textField.RegisterValueChangedCallback(evt =>
            {
                choice.choiceText = evt.newValue;
                MarkDirty();
            });
            row.Add(textField);

            var removeButton = new Button(() => RemoveChoicePort(choice, port)) { text = "x" };
            row.Add(removeButton);

            port.contentContainer.Insert(0, row);

            OutputPorts.Add(port);

            if (_addChoiceButton != null)
                outputContainer.Insert(outputContainer.IndexOf(_addChoiceButton), port);
            else
                outputContainer.Add(port);
        }

        // Disconnects any edge attached to this port before removing it,
        // so the graph doesn't end up with a dangling edge
        private void RemoveChoicePort(DialogueChoice choice, Port port)
        {
            foreach (var edge in port.connections.ToList())
            {
                edge.output.Disconnect(edge);
                edge.input.Disconnect(edge);
                edge.RemoveFromHierarchy();
            }

            DialogueNode.choices.Remove(choice);
            OutputPorts.Remove(port);
            outputContainer.Remove(port);

            RefreshExpandedState();
            RefreshPorts();
            MarkDirty();
        }

        private void MarkDirty() => EditorUtility.SetDirty(_graph);
        #endregion
    }
}