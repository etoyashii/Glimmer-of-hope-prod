using UnityEngine;
using UnityEngine.Events;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Adapter placed on a WindPushable tagged object so PushSkill and
    /// PullSkill can hand off to a scripted behaviour, for example
    /// ScriptedMovement, instead of applying a raw physics force.
    /// The wind skills only know about this component, never about what
    /// it is wired to react.
    /// </summary>
    public class WindReactive : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Reacts To")]
        [Tooltip("If true, PushSkill calls OnWindPush instead of applying force to this object.")]
        [SerializeField] private bool _reactToPush = true;

        [Tooltip("If true, PullSkill calls OnWindPull instead of applying force to this object.")]
        [SerializeField] private bool _reactToPull = false;

        [Header("Events")]
        [Tooltip("Invoked by PushSkill when this object is hit by a push, only if reactToPush is true.")]
        public UnityEvent OnWindPush;

        [Tooltip("Invoked by PullSkill when this object is hit by a pull, only if reactToPull is true.")]
        public UnityEvent OnWindPull;

        #endregion

        #region Public Properties

        public bool ReactsToPush => _reactToPush;
        public bool ReactsToPull => _reactToPull;

        #endregion

        #region Public Methods

        public void NotifyPush() => OnWindPush?.Invoke();
        public void NotifyPull() => OnWindPull?.Invoke();

        #endregion
    }
}