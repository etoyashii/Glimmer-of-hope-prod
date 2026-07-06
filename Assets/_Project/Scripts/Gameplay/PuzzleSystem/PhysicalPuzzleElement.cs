using UnityEngine;

namespace GlimmerOfHope.Gameplay.Puzzles
{
    /// <summary>
    /// A puzzle element based on a Rigidbody (crates, rocks, orbs...).
    /// Considered solved when placed close enough to a target position.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicalPuzzleElement : PuzzleElement
    {
        #region Serialized Fields

        [Header("Physical Element")]
        [Tooltip("Target transform the object must reach to be considered solved. Leave empty if solved by external event.")]
        [SerializeField] private Transform _targetPosition;

        [Tooltip("Distance tolerance to the target position to consider this element solved.")]
        [SerializeField] private float _solveDistance = 0.5f;

        #endregion

        #region Private Fields

        private Rigidbody _rb;

        // Initial velocity snapshot (always zero at start, but kept for consistency)
        private Vector3 _initialVelocity = Vector3.zero;
        private Vector3 _initialAngularVelocity = Vector3.zero;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _rb = GetComponent<Rigidbody>();
        }

        #endregion

        #region PuzzleElement Implementation

        public override void CheckSolvedState()
        {
            // If no target is assigned, solved state is driven externally via ForceSetSolved()
            if (_targetPosition == null) return;

            float distance = Vector3.Distance(transform.position, _targetPosition.position);
            SetSolved(distance <= _solveDistance);
        }

        protected override void OnReset()
        {
            // Stop all physics motion on reset
            if (_rb != null)
            {
                _rb.linearVelocity = _initialVelocity;
                _rb.angularVelocity = _initialAngularVelocity;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually force this element into a solved or unsolved state.
        /// Use this when the solved condition is triggered by an external event.
        /// </summary>
        public void ForceSetSolved(bool solved)
        {
            SetSolved(solved);
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            if (_targetPosition == null) return;

            Gizmos.color = IsSolved ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(_targetPosition.position, _solveDistance);
            Gizmos.DrawLine(transform.position, _targetPosition.position);
        }

        #endregion
    }
}