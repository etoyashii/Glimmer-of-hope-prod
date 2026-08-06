using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public class GraphValidator
    {
        #region Nested Types

        public class ValidationResult
        {
            public bool IsValid => Errors.Count == 0;
            public List<string> Errors { get; } = new();
            public List<string> Warnings { get; } = new();
        }

        #endregion

        #region Private Fields

        private readonly DialogueGraphView _graphView;

        #endregion

        #region Constructor

        public GraphValidator(DialogueGraphView graphView)
        {
            _graphView = graphView;
        }

        #endregion

        #region Public Methods

        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            ValidateStartNode(result);
            ValidateLineNodes(result);
            ValidateConnections(result);
            ValidateDuplicateIds(result);
            ValidateReachability(result);

            return result;
        }

        #endregion

        #region Private Methods — Checks

        private void ValidateStartNode(ValidationResult result)
        {
            var startNode = _graphView.GetStartNode();

            if (startNode == null)
            {
                result.Errors.Add("Aucun noeud START trouve");
                return;
            }

            if (!startNode.OutputPort.connected)
                result.Errors.Add("Le noeud START n'est connecte a aucune ligne");

            if (string.IsNullOrWhiteSpace(startNode.ConversationId))
            {
                result.Errors.Add(
                    "La conversation n'a pas d'ID. Renseignez ConversationId sur l'asset " +
                    "avant de sauvegarder, sinon les textes ne seront pas ecrits.");
            }
        }

        private void ValidateLineNodes(ValidationResult result)
        {
            var lineNodes = _graphView.GetLineNodes();

            if (lineNodes.Count == 0)
            {
                result.Warnings.Add("Aucune ligne de dialogue dans le graph");
                return;
            }

            foreach (var node in lineNodes)
            {
                ValidateSingleLineNode(node, result);
            }
        }

        private void ValidateSingleLineNode(DialogueLineNode node, ValidationResult result)
        {
            if (string.IsNullOrEmpty(node.LineId))
                result.Errors.Add($"Noeud sans ID detecte");

            if (node.LineSO != null && node.LineSO.Speaker == null)
                result.Warnings.Add($"[{node.LineId}] Aucun speaker assigne");

            var text = node.GetLocalizedText("fr");
            if (string.IsNullOrWhiteSpace(text))
                result.Warnings.Add($"[{node.LineId}] Texte FR vide");

            if (!node.InputPort.connected)
                result.Warnings.Add($"[{node.LineId}] Noeud non connecte en entree (orphelin)");

            ValidateNodeOutputs(node, result);
        }

        private void ValidateNodeOutputs(DialogueLineNode node, ValidationResult result)
        {
            bool hasDefaultConnection = node.DefaultOutputPort.connected;
            bool hasChoices = node.ChoicePorts.Count > 0;
            bool hasConditions = node.ConditionPorts.Count > 0;

            if (!hasDefaultConnection && !hasChoices && !hasConditions)
            {
                result.Warnings.Add($"[{node.LineId}] Aucune sortie connectee (cul-de-sac)");
            }

            CheckDisconnectedChoicePorts(node, result);
            CheckDisconnectedConditionPorts(node, result);
        }

        private void CheckDisconnectedChoicePorts(DialogueLineNode node, ValidationResult result)
        {
            for (int i = 0; i < node.ChoicePorts.Count; i++)
            {
                if (node.ChoicePorts[i].connected)
                    continue;

                if (IsChoiceEmpty(node, i))
                    result.Warnings.Add($"[{node.LineId}] Choix {i + 1} vide et non connecte (ignore en jeu)");
                else
                    result.Errors.Add($"[{node.LineId}] Choix {i + 1} non connecte");
            }
        }

        private bool IsChoiceEmpty(DialogueLineNode node, int index)
        {
            if (node.Choices == null)
                return true;

            foreach (var lang in DialogueCSVFormat.LANGUAGES)
            {
                if (!string.IsNullOrWhiteSpace(node.Choices.GetChoiceText(index, lang)))
                    return false;
            }

            return true;
        }

        private void CheckDisconnectedConditionPorts(DialogueLineNode node, ValidationResult result)
        {
            for (int i = 0; i < node.ConditionPorts.Count; i++)
            {
                if (!node.ConditionPorts[i].connected)
                    result.Errors.Add($"[{node.LineId}] Condition {i + 1} non connectee");
            }
        }

        private void ValidateConnections(ValidationResult result)
        {
            var edges = _graphView.edges.ToList();

            foreach (var edge in edges)
            {
                if (edge.output == null || edge.input == null)
                    result.Errors.Add("Edge invalide detecte (port manquant)");
            }
        }

        private void ValidateDuplicateIds(ValidationResult result)
        {
            var lineNodes = _graphView.GetLineNodes();
            var idCounts = new Dictionary<string, int>();

            foreach (var node in lineNodes)
            {
                if (string.IsNullOrEmpty(node.LineId)) continue;

                if (idCounts.ContainsKey(node.LineId))
                    idCounts[node.LineId]++;
                else
                    idCounts[node.LineId] = 1;
            }

            foreach (var kvp in idCounts)
            {
                if (kvp.Value > 1)
                    result.Errors.Add($"ID duplique: '{kvp.Key}' ({kvp.Value} occurrences)");
            }
        }

        private void ValidateReachability(ValidationResult result)
        {
            var startNode = _graphView.GetStartNode();
            if (startNode == null) return;

            var reachable = new HashSet<string>();
            TraverseFromPort(startNode.OutputPort, reachable);

            var lineNodes = _graphView.GetLineNodes();
            var unreachable = lineNodes.Where(n => !reachable.Contains(n.LineId)).ToList();

            foreach (var node in unreachable)
            {
                result.Warnings.Add($"[{node.LineId}] Non atteignable depuis START");
            }
        }

        private void TraverseFromPort(Port outputPort, HashSet<string> visited)
        {
            if (outputPort == null || !outputPort.connected)
                return;

            foreach (var edge in outputPort.connections)
            {
                if (edge.input?.node is not DialogueLineNode lineNode)
                    continue;

                if (!visited.Add(lineNode.LineId))
                    continue;

                TraverseFromPort(lineNode.DefaultOutputPort, visited);

                foreach (var choicePort in lineNode.ChoicePorts)
                    TraverseFromPort(choicePort, visited);

                foreach (var condPort in lineNode.ConditionPorts)
                    TraverseFromPort(condPort, visited);
            }
        }

        #endregion
    }
}
