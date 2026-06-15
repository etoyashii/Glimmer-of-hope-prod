using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using GlimmerOfHope.Gameplay.Dialogue;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public class ConversationStartNode : Node
    {
        #region Private Fields

        private Port _outputPort;
        private string _conversationId;
        private string _displayName;
        private ConversationType _conversationType;

        #endregion

        #region Properties

        public Port OutputPort => _outputPort;
        public string ConversationId => _conversationId;
        public string DisplayName => _displayName;
        public ConversationType ConversationType => _conversationType;

        #endregion

        #region Public Methods

        public void Initialize(ConversationSO conversation)
        {
            title = "START";
            _conversationId = conversation != null ? conversation.ConversationId : "new_conversation";
            _displayName = conversation != null ? conversation.DisplayName : "New Conversation";
            _conversationType = conversation != null ? conversation.Type : ConversationType.Standard;

            AddToClassList("start-node");
            SetupPorts();
            BuildBody();
            RefreshExpandedState();
            RefreshPorts();

            capabilities &= ~Capabilities.Deletable;
        }

        public void UpdateMetadata(string convId, string displayName, ConversationType type)
        {
            _conversationId = convId;
            _displayName = displayName;
            _conversationType = type;
            BuildBody();
        }

        #endregion

        #region Private Methods

        private void SetupPorts()
        {
            _outputPort = DialoguePortFactory.CreateFlowOutput(this, "First Line");
            outputContainer.Add(_outputPort);
        }

        private void BuildBody()
        {
            var container = mainContainer.Q("contents")?.Q("top") ?? mainContainer;

            var existingInfo = container.Q("start-info");
            if (existingInfo != null)
                container.Remove(existingInfo);

            var info = new VisualElement { name = "start-info" };
            info.AddToClassList("start-node__info");

            info.Add(CreateLabel("ID", _conversationId));
            info.Add(CreateLabel("Nom", _displayName));
            info.Add(CreateLabel("Type", _conversationType.ToString()));

            container.Add(info);
        }

        private Label CreateLabel(string prefix, string value)
        {
            var label = new Label($"{prefix}: {value}");
            label.AddToClassList("start-node__label");
            return label;
        }

        #endregion
    }
}
