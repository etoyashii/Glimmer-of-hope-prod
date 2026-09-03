using System;
using UnityEngine;
using UnityEngine.Events;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// A progression milestone, when the number of completed sessions reaches RequiredCompletions on THIS bench, OnReached is triggered once
    /// </summary>
    [Serializable]
    public class CompletionMilestone
    {
        #region Public Fields
        [Tooltip("Number of complete sessions (all breaths done) to reach on this bench before triggering the event.")]
        public int RequiredCompletions = 1;
        public UnityEvent OnReached;
        #endregion
    }
}
