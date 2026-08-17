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
        public DialogueNode DialogueNode { get; }
        public Port InputPort { get; private set; }
        public List<Port> OutputPorts { get; } = new List<Port>();

        private readonly DialogueGraph _graph;
        private Button _addChoiceButton;

        public DialogueNodeView(DialogueNode dialogueNode, DialogueGraph graph)
        {
            DialogueNode = dialogueNode;
            _graph = graph;
            title = BuildTitle();
            titleContainer.style.backgroundColor = new StyleColor(GetTitleColor(dialogueNode.nodeType));

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

        private string BuildTitle()
        {
            if (DialogueNode.nodeType == DialogueNodeType.Dialogue)
                return string.IsNullOrEmpty(DialogueNode.speakerId) ? "(no speaker)" : DialogueNode.speakerId;

            return DialogueNode.nodeType.ToString().ToUpper();
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
        private void BuildInputPort()
        {
            if (DialogueNode.nodeType == DialogueNodeType.Start) return;

            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "";
            inputContainer.Add(InputPort);
        }

        private void BuildOutputPorts()
        {
            if (DialogueNode.nodeType == DialogueNodeType.End) return;

            if (DialogueNode.nodeType == DialogueNodeType.Dialogue && DialogueNode.hasChoices)
            {
                foreach (var choice in DialogueNode.choices)
                    AddChoicePort(choice);

                _addChoiceButton = new Button(OnAddChoiceClicked) { text = "+ Choice" };
                outputContainer.Add(_addChoiceButton);
                return;
            }

            for (int i = 0; i < DialogueNode.choices.Count; i++)
            {
                var choice = DialogueNode.choices[i];
                var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(bool));
                port.userData = choice;
                port.portName = DialogueNode.nodeType == DialogueNodeType.Condition
                    ? (i == 0 ? "True" : "False")
                    : "";

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
    }
}