using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public static class DialoguePortFactory
    {
        #region Constants

        public const string FLOW_PORT_TYPE = "Flow";
        public const string CHOICE_PORT_TYPE = "Choice";
        public const string CONDITION_PORT_TYPE = "Condition";

        private static readonly Color FLOW_COLOR = new(0.8f, 0.8f, 0.8f);
        private static readonly Color CHOICE_COLOR = new(0.2f, 0.7f, 1f);
        private static readonly Color CONDITION_COLOR = new(1f, 0.7f, 0.2f);

        #endregion

        #region Public Methods

        public static Port CreateFlowInput(Node node)
        {
            var port = node.InstantiatePort(
                Orientation.Horizontal,
                Direction.Input,
                Port.Capacity.Multi,
                typeof(float));

            port.portName = "";
            port.portColor = FLOW_COLOR;
            return port;
        }

        public static Port CreateFlowOutput(Node node, string label = "")
        {
            var port = node.InstantiatePort(
                Orientation.Horizontal,
                Direction.Output,
                Port.Capacity.Single,
                typeof(float));

            port.portName = label;
            port.portColor = FLOW_COLOR;
            return port;
        }

        public static Port CreateChoiceOutput(Node node, string choiceText, int index)
        {
            var port = node.InstantiatePort(
                Orientation.Horizontal,
                Direction.Output,
                Port.Capacity.Single,
                typeof(float));

            port.portName = TruncateLabel(choiceText, 30);
            port.portColor = CHOICE_COLOR;
            port.userData = index;
            return port;
        }

        public static Port CreateConditionOutput(Node node, string condition, int index)
        {
            var port = node.InstantiatePort(
                Orientation.Horizontal,
                Direction.Output,
                Port.Capacity.Single,
                typeof(float));

            port.portName = condition;
            port.portColor = CONDITION_COLOR;
            port.userData = index;
            return port;
        }

        #endregion

        #region Private Methods

        private static string TruncateLabel(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return "(vide)";

            return text.Length <= maxLength ? text : text.Substring(0, maxLength - 3) + "...";
        }

        #endregion
    }
}
