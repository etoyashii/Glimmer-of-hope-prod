using System;
using UnityEngine;
using UnityEngine.Events;

namespace GlimmerOfHope.Gameplay.Puzzles
{
    /// <summary>
    /// Base class for every object that belongs to a puzzle.
    /// Handles state snapshot (position, rotation, solved state) and reset.
    /// Inherit from this to create specific puzzle elements (physical objects, switches, etc.)
    /// </summary>
    public abstract class PuzzleElement : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Puzzle Element")]
        [Tooltip("Display name for this element, used for debugging.")]
        [SerializeField] private string _elementName = "PuzzleElement";

        [Tooltip("If true, this element's state is included in the puzzle solved check.")]
        [SerializeField] private bool _requiredForSolution = true;

        #endregion

        #region Events

        /// <summary>Fired when this element reaches its solved state.</summary>
        public event Action OnElementSolved;

        /// <summary>Fired when this element leaves its solved state.</summary>
        public event Action OnElementUnsolved;

        #endregion

        #region Public Properties

        public string ElementName => _elementName;
        public bool RequiredForSolution => _requiredForSolution;
        public bool IsSolved { get; private set; }

        #endregion

        #region Private Fields

        // Initial state snapshot taken on Awake, used for reset
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private bool _snapshotTaken;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            TakeSnapshot();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Resets this element back to its initial state (position, rotation, and any custom state).
        /// </summary>
        public void ResetElement()
        {
            transform.position = _initialPosition;
            transform.rotation = _initialRotation;

            OnReset();

            // Reset solved state silently (no event fired to avoid loop during full puzzle reset)
            SetSolved(false, silent: true);
        }

        /// <summary>
        /// Force-overrides the initial snapshot to the current transform.
        /// Useful when elements are procedurally placed before the puzzle starts.
        /// </summary>
        public void BakeCurrentStateAsInitial()
        {
            TakeSnapshot();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Call this from subclasses whenever the element's solved condition changes.
        /// </summary>
        protected void SetSolved(bool solved, bool silent = false)
        {
            if (IsSolved == solved) return;

            IsSolved = solved;

            if (silent) return;

            if (solved)
                OnElementSolved?.Invoke();
            else
                OnElementUnsolved?.Invoke();
        }

        /// <summary>
        /// Override to restore any custom state specific to the subclass (e.g. switch off, velocity zero).
        /// Called automatically during ResetElement(), after position/rotation are restored.
        /// </summary>
        protected virtual void OnReset() { }

        /// <summary>
        /// Override to implement the logic that checks whether this element is in its solved state.
        /// Called by the PuzzleManager every frame (or on demand).
        /// </summary>
        public abstract void CheckSolvedState();

        #endregion

        #region Private Methods

        private void TakeSnapshot()
        {
            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
            _snapshotTaken = true;
        }

        #endregion
    }
}