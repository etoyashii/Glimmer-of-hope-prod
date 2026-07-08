using UnityEngine;
using UnityEngine.Events;

namespace GlimmerOfHope.Gameplay.Puzzles
{
    /// <summary>
    /// A puzzle element based on a Rigidbody (crates, rocks, orbs...).
    /// Considered solved when placed close enough to a target position.
    /// Optionally snaps and locks to the target when the solve distance is reached.
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

        [Header("Snap")]
        [Tooltip("If true, the object snaps and locks to the target position when it enters the solve distance.")]
        [SerializeField] private bool _snapOnSolve = true;

        [Tooltip("How fast the object lerps to the target position when snapping (units/sec). 0 = instant.")]
        [SerializeField] private float _snapSpeed = 10f;

        [Tooltip("Fired when the object snaps to the target.")]
        public UnityEvent OnSnapped;

        #endregion

        #region Private Fields

        private Rigidbody _rb;

        private bool _isSnapped;
        private bool _isSnapping;

        // Initial velocity snapshot (always zero at start, but kept for consistency)
        private Vector3 _initialVelocity = Vector3.zero;
        private Vector3 _initialAngularVelocity = Vector3.zero;

        #endregion

        #region Public Properties

        public bool IsSnapped => _isSnapped;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            _rb = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (!_isSnapping || _isSnapped) return;

            if (_snapSpeed <= 0f)
            {
                // Instant snap
                SnapToTarget();
                return;
            }

            // Lerp toward target
            _rb.MovePosition(Vector3.Lerp(
                transform.position,
                _targetPosition.position,
                _snapSpeed * Time.fixedDeltaTime
            ));

            // Lock once close enough
            if (Vector3.Distance(transform.position, _targetPosition.position) < 0.01f)
                SnapToTarget();
        }

        #endregion

        #region PuzzleElement Implementation

        public override void CheckSolvedState()
        {
            // If no target is assigned, solved state is driven externally via ForceSetSolved()
            if (_targetPosition == null) return;

            // Already snapped — stays solved
            if (_isSnapped) return;

            float distance = Vector3.Distance(transform.position, _targetPosition.position);
            bool inRange = distance <= _solveDistance;

            if (inRange && _snapOnSolve && !_isSnapping)
                BeginSnap();

            // When not snapping, solved is purely distance-based
            if (!_snapOnSolve)
                SetSolved(inRange);
        }

        protected override void OnReset()
        {
            // Cancel any ongoing snap and unlock the rigidbody
            _isSnapping = false;
            _isSnapped = false;

            if (_rb != null)
            {
                _rb.isKinematic = false;
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

        #region Private Methods

        private void BeginSnap()
        {
            _isSnapping = true;

            // Freeze physics so nothing interrupts the snap lerp
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.isKinematic = true;
        }

        private void SnapToTarget()
        {
            _isSnapping = false;
            _isSnapped = true;

            transform.position = _targetPosition.position;
            transform.rotation = _targetPosition.rotation;

            // Keep kinematic so the object stays locked in place
            _rb.isKinematic = true;

            SetSolved(true);
            OnSnapped?.Invoke();
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