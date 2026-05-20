using System;

namespace GlimmerOfHope.Gameplay.Dialogue
{
    /// <summary>
    /// Parses and evaluates dialogue branching conditions.
    /// Supports: HAS:flag, NOT:flag, AND, OR operators.
    /// Limitation: Cannot mix AND and OR in same condition (MVP scope).
    /// </summary>
    public static class ConditionParser
    {
        #region Constants

        private const string HAS_PREFIX = "HAS:";
        private const string NOT_PREFIX = "NOT:";
        private const string AND_OP = " AND ";
        private const string OR_OP = " OR ";

        #endregion

        #region Public Methods

        public static bool Evaluate(string condition, FlagManager flagManager)
        {
            if (string.IsNullOrWhiteSpace(condition))
                return true;

            if (flagManager == null)
                return false;

            condition = condition.Trim();

            if (condition.Contains(AND_OP, StringComparison.OrdinalIgnoreCase))
                return EvaluateAnd(condition, flagManager);

            if (condition.Contains(OR_OP, StringComparison.OrdinalIgnoreCase))
                return EvaluateOr(condition, flagManager);

            return EvaluateSingle(condition, flagManager);
        }

        #endregion

        #region Private Methods

        private static bool EvaluateSingle(string condition, FlagManager flagManager)
        {
            condition = condition.Trim();

            if (condition.StartsWith(HAS_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                var flag = condition.Substring(HAS_PREFIX.Length).Trim();
                return flagManager.HasFlag(flag);
            }

            if (condition.StartsWith(NOT_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                var flag = condition.Substring(NOT_PREFIX.Length).Trim();
                return !flagManager.HasFlag(flag);
            }

            return flagManager.HasFlag(condition);
        }

        private static bool EvaluateAnd(string condition, FlagManager flagManager)
        {
            var parts = condition.Split(new[] { AND_OP }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (!EvaluateSingle(part, flagManager))
                    return false;
            }

            return true;
        }

        private static bool EvaluateOr(string condition, FlagManager flagManager)
        {
            var parts = condition.Split(new[] { OR_OP }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (EvaluateSingle(part, flagManager))
                    return true;
            }

            return false;
        }

        #endregion
    }
}
