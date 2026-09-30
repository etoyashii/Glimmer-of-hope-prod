using System;
using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// Registry of custom conditions for Condition nodes in "Script Query" mode whenever a flag isn't enough.
    /// </summary>
    public static class DialogueConditions
    {
        #region Private Fields

        private static readonly Dictionary<string, Func<bool>> Providers = new Dictionary<string, Func<bool>>();

        #endregion

        #region Public Methods

        public static void Register(string id, Func<bool> provider)
        {
            if (string.IsNullOrEmpty(id) || provider == null) return;
            Providers[id] = provider;
        }

        public static void Unregister(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            Providers.Remove(id);
        }

        public static bool Evaluate(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;

            if (Providers.TryGetValue(id, out var provider))
                return provider.Invoke();

            Debug.LogWarning($"[DialogueConditions] No condition registered for ID '{id}'.");
            return false;
        }

        #endregion
    }
}
