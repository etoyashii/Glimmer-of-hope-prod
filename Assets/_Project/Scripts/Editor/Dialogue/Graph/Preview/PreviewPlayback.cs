using System;
using System.Linq;
using GlimmerOfHope.Gameplay.Dialogue;

namespace GlimmerOfHope.Editor.Dialogue.Graph
{
    public class PreviewPlayback
    {
        #region Private Fields

        private readonly PreviewFlagManager _flagManager;

        #endregion

        #region Constructor

        public PreviewPlayback(PreviewFlagManager flagManager)
        {
            _flagManager = flagManager;
        }

        #endregion

        #region Public Methods

        public void ExecuteActions(DialogueAction[] actions)
        {
            if (actions == null) return;

            foreach (var action in actions)
            {
                if (action.type == DialogueActionType.SetFlag)
                    _flagManager.SetFlag(action.parameter);
            }
        }

        public DialogueLineNode FindNextNode(DialogueLineNode currentNode)
        {
            if (currentNode == null) return null;

            var condTarget = EvaluateConditionals(currentNode);
            if (condTarget != null)
                return condTarget;

            return GetDefaultNext(currentNode);
        }

        #endregion

        #region Private Methods — Conditions

        private DialogueLineNode EvaluateConditionals(DialogueLineNode node)
        {
            foreach (var condPort in node.ConditionPorts)
            {
                if (!condPort.connected) continue;

                var edge = condPort.connections.FirstOrDefault();
                if (edge?.input?.node is not DialogueLineNode target) continue;

                var condIndex = (int)condPort.userData;
                if (condIndex >= node.LineSO.ConditionalNexts.Length) continue;

                var condition = node.LineSO.ConditionalNexts[condIndex].condition;
                if (EvaluateCondition(condition))
                    return target;
            }

            return null;
        }

        private DialogueLineNode GetDefaultNext(DialogueLineNode node)
        {
            if (!node.DefaultOutputPort.connected)
                return null;

            var edge = node.DefaultOutputPort.connections.FirstOrDefault();
            return edge?.input?.node as DialogueLineNode;
        }

        public bool EvaluateCondition(string condition)
        {
            if (string.IsNullOrWhiteSpace(condition))
                return true;

            if (condition.Contains(" AND "))
            {
                var parts = condition.Split(new[] { " AND " }, StringSplitOptions.RemoveEmptyEntries);
                return parts.All(EvaluateSingle);
            }

            if (condition.Contains(" OR "))
            {
                var parts = condition.Split(new[] { " OR " }, StringSplitOptions.RemoveEmptyEntries);
                return parts.Any(EvaluateSingle);
            }

            return EvaluateSingle(condition);
        }

        private bool EvaluateSingle(string cond)
        {
            cond = cond.Trim();

            if (cond.StartsWith("HAS:"))
                return _flagManager.HasFlag(cond[4..].Trim());

            if (cond.StartsWith("NOT:"))
                return !_flagManager.HasFlag(cond[4..].Trim());

            return _flagManager.HasFlag(cond);
        }

        #endregion
    }
}
