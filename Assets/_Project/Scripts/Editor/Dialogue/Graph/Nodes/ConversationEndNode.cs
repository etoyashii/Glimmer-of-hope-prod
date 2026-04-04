using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public class ConversationEndNode : Node
    {
        #region Private Fields

        private Port _inputPort;

        #endregion

        #region Properties

        public Port InputPort => _inputPort;

        #endregion

        #region Public Methods

        public void Initialize()
        {
            title = "FIN";
            AddToClassList("end-node");

            SetupPorts();
            BuildBody();
            RefreshExpandedState();
            RefreshPorts();
        }

        #endregion

        #region Private Methods

        private void SetupPorts()
        {
            _inputPort = DialoguePortFactory.CreateFlowInput(this);
            inputContainer.Add(_inputPort);
        }

        private void BuildBody()
        {
            var label = new Label("Fin de la conversation");
            label.AddToClassList("end-node__label");
            mainContainer.Add(label);
        }

        #endregion
    }
}
