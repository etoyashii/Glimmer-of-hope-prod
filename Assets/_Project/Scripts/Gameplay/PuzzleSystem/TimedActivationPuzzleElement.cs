using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace GlimmerOfHope.Gameplay.Puzzles
{
    /// <summary>
    /// A puzzle element that becomes solved when activated, for example by
    /// a push or pull skill through WindReactive, then automatically
    /// unsolves itself if not reactivated within a time limit. Meant for
    /// race against the clock puzzles, several of these all need to report
    /// IsSolved at the same instant for the puzzle to solve, which
    /// PuzzleManager AllElements mode already enforces on its own.
    /// </summary>
    public class TimedActivationPuzzleElement : PuzzleElement
    {
        #region Serialized Fields

        [Header("Timed Activation")]
        [Tooltip("Time in seconds this element stays active before automatically deactivating if not reactivated.")]
        [SerializeField] private float _activeDuration = 3f;

        [Header("Events")]
        [Tooltip("Fired every time this element is activated or reactivated. Use for VFX, sound.")]
        public UnityEvent OnActivated;

        [Tooltip("Fired when this element times out and deactivates on its own.")]
        public UnityEvent OnTimedOut;

        #endregion

        #region Private Fields

        private Coroutine _timeoutRoutine;

        #endregion

        #region Public Properties

        public bool IsActive => IsSolved;

        /// <summary>Remaining time in seconds before this element times out, 0 if inactive. Useful for a UI countdown ring.</summary>
        public float RemainingTime { get; private set; }

        #endregion

        #region PuzzleElement Implementation

        public override void CheckSolvedState()
        {
            // Solved state is event driven, Activate() sets it directly,
            // the countdown coroutine clears it on timeout.
        }

        protected override void OnReset()
        {
            if (_timeoutRoutine != null)
            {
                StopCoroutine(_timeoutRoutine);
                _timeoutRoutine = null;
            }

            RemainingTime = 0f;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Activates this element, or refreshes its countdown if already
        /// active. Wire this to WindReactive OnWindPush and or OnWindPull.
        /// </summary>
        public void Activate()
        {
            if (_timeoutRoutine != null)
                StopCoroutine(_timeoutRoutine);

            _timeoutRoutine = StartCoroutine(TimeoutRoutine());

            SetSolved(true);
            OnActivated?.Invoke();
        }

        #endregion

        #region Private Methods

        private IEnumerator TimeoutRoutine()
        {
            RemainingTime = _activeDuration;

            while (RemainingTime > 0f)
            {
                RemainingTime -= Time.deltaTime;
                yield return null;
            }

            RemainingTime = 0f;
            _timeoutRoutine = null;

            SetSolved(false);
            OnTimedOut?.Invoke();
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            Gizmos.color = IsSolved ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }

        #endregion
    }
}