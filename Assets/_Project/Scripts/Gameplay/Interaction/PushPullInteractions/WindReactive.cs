using UnityEngine;
using UnityEngine.Events;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Adapter placed on a WindPushable tagged object so PushSkill and
    /// PullSkill can hand off to a scripted behaviour, for example
    /// ScriptedMovement, instead of applying a raw physics force.
    /// The wind skills only know about this component, never about what
    /// it is wired to react. Optionally gates the reaction behind an
    /// approach angle, so the caster must be roughly on a specific side
    /// of the object for the push or pull to count.
    /// </summary>
    public class WindReactive : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Reacts To")]
        [Tooltip("If true, PushSkill calls OnWindPush instead of applying force to this object.")]
        [SerializeField] private bool _reactToPush = true;

        [Tooltip("If true, PullSkill calls OnWindPull instead of applying force to this object.")]
        [SerializeField] private bool _reactToPull = false;

        [Header("Angle Gate")]
        [Tooltip("If true, the caster must stand within MaxAngle of RequiredDirection for the reaction to trigger.")]
        [SerializeField] private bool _useAngleGate = false;

        [Tooltip("Reference transform, its forward axis is the direction the caster must be standing in. Defaults to this object if left empty.")]
        [SerializeField] private Transform _requiredDirection;

        [Range(1f, 180f)]
        [Tooltip("Half angle in degrees around RequiredDirection.forward the caster must be within, height is ignored.")]
        [SerializeField] private float _maxAngle = 45f;

        [Header("Events")]
        [Tooltip("Invoked by PushSkill when this object is hit by a push from a valid angle, only if reactToPush is true.")]
        public UnityEvent OnWindPush;

        [Tooltip("Invoked by PullSkill when this object is hit by a pull from a valid angle, only if reactToPull is true.")]
        public UnityEvent OnWindPull;

        #endregion

        #region Public Properties

        public bool ReactsToPush => _reactToPush;
        public bool ReactsToPull => _reactToPull;

        #endregion

        #region Public Methods

        /// <summary>Called by PushSkill. casterPosition is only used if the angle gate is active.</summary>
        public void NotifyPush(Vector3 casterPosition)
        {
            if (!IsWithinAngle(casterPosition)) return;
            OnWindPush?.Invoke();
        }

        /// <summary>Called by PullSkill. casterPosition is only used if the angle gate is active.</summary>
        public void NotifyPull(Vector3 casterPosition)
        {
            if (!IsWithinAngle(casterPosition)) return;
            OnWindPull?.Invoke();
        }

        #endregion

        #region Private Methods

        private bool IsWithinAngle(Vector3 casterPosition)
        {
            if (!_useAngleGate) return true;

            Transform reference = _requiredDirection != null ? _requiredDirection : transform;

            Vector3 toCaster = casterPosition - transform.position;
            toCaster.y = 0f;

            if (toCaster.sqrMagnitude < 0.0001f) return true;

            float angle = Vector3.Angle(reference.forward, toCaster.normalized);
            return angle <= _maxAngle;
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (!_useAngleGate) return;

            Transform reference = _requiredDirection != null ? _requiredDirection : transform;

            Vector3 leftBound = Quaternion.Euler(0f, -_maxAngle, 0f) * reference.forward;
            Vector3 rightBound = Quaternion.Euler(0f, _maxAngle, 0f) * reference.forward;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + reference.forward * 2f);
            Gizmos.DrawLine(transform.position, transform.position + leftBound * 2f);
            Gizmos.DrawLine(transform.position, transform.position + rightBound * 2f);
        }

        #endregion
    }
}