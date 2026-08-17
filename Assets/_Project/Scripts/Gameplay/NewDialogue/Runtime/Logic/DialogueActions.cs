using System;
using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// Registry of actions for Action nodes in "Script Action" mode ,for any effect that goes beyond a flag 
    /// </summary>
    public static class DialogueActions
    {
        #region Private Fields

        private static readonly Dictionary<string, Action> Actions = new Dictionary<string, Action>();

        #endregion

        #region Public Methods

        public static void Register(string id, Action action)
        {
            if (string.IsNullOrEmpty(id) || action == null) return;
            Actions[id] = action;
        }

        public static void Unregister(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            Actions.Remove(id);
        }

        public static void Invoke(string id)
        {
            if (string.IsNullOrEmpty(id)) return;

            if (Actions.TryGetValue(id, out var action))
            {
                action.Invoke();
                return;
            }

            Debug.LogWarning($"[DialogueActions] No action registered for ID '{id}'.");
        }

        #endregion
    }
}
