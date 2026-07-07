using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GlimmerOfHope.Gameplay.Puzzles
{
    /// <summary>
    /// Manages a single puzzle: tracks all its PuzzleElements, checks the solved condition,
    /// handles reset, and exposes the solved state for the save system.
    /// 
    /// Two solve modes are available (set in Inspector):
    /// - AllElements: every required PuzzleElement must be solved.
    /// - ExternalEvent: the puzzle is solved by calling CompletePuzzle() from outside
    ///   (e.g. a cutscene, a door opening, etc.) regardless of element states.
    /// </summary>
    public class PuzzleManager : MonoBehaviour
    {
        #region Inner Types

        public enum SolveMode
        {
            /// <summary>Puzzle is solved when all required PuzzleElements report IsSolved.</summary>
            AllElements,

            /// <summary>Puzzle is solved by an external call to CompletePuzzle().</summary>
            ExternalEvent
        }

        #endregion

        #region Serialized Fields

        [Header("Puzzle Settings")]
        [Tooltip("Unique identifier for this puzzle, used by the save system.")]
        [SerializeField] private string _puzzleId = "puzzle_01";

        [Tooltip("Human-readable name, used for debugging.")]
        [SerializeField] private string _puzzleName = "My Puzzle";

        [Tooltip("How the puzzle determines it is solved.")]
        [SerializeField] private SolveMode _solveMode = SolveMode.AllElements;

        [Header("Elements")]
        [Tooltip("All PuzzleElements that belong to this puzzle. Drag them in here.")]
        [SerializeField] private List<PuzzleElement> _elements = new();

        [Header("Events")]
        [Tooltip("Fired once when the puzzle is solved for the first time.")]
        public UnityEvent OnPuzzleSolved;

        [Tooltip("Fired once when the puzzle is triggered")]
        public UnityEvent OnPuzzleTriggered;

        [Tooltip("Fired when the puzzle is reset.")]
        public UnityEvent OnPuzzleReset;

        #endregion

        #region Public Properties

        public string PuzzleId => _puzzleId;
        public string PuzzleName => _puzzleName;
        public bool IsSolved { get; private set; }

        /// <summary>Returns a 0-1 ratio of how many required elements are currently solved.</summary>
        public float Progress
        {
            get
            {
                List<PuzzleElement> required = GetRequiredElements();
                if (required.Count == 0) return 0f;

                int solvedCount = 0;
                foreach (PuzzleElement element in required)
                    if (element.IsSolved) solvedCount++;

                return (float)solvedCount / required.Count;
            }
        }

        #endregion

        #region Private Fields

        private bool _hasBeenSolvedOnce = false;
        private bool _hasBeenTriggered = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Subscribe to each element's events to react immediately when states change
            foreach (PuzzleElement element in _elements)
            {
                if (element == null) continue;
                element.OnElementSolved += OnAnyElementStateChanged;
                element.OnElementUnsolved += OnAnyElementStateChanged;
            }
        }

        private void OnDestroy()
        {
            foreach (PuzzleElement element in _elements)
            {
                if (element == null) continue;
                element.OnElementSolved -= OnAnyElementStateChanged;
                element.OnElementUnsolved -= OnAnyElementStateChanged;
            }
        }

        private void Update()
        {
            // Physical elements need continuous position checking
            if (_solveMode == SolveMode.AllElements && !IsSolved)
            {
                foreach (PuzzleElement element in _elements)
                {
                    if (element is PhysicalPuzzleElement)
                        element.CheckSolvedState();
                }

                EvaluateSolvedCondition();
            }
        }

        #endregion

        #region Public Methods

        public void TriggerPuzzle()
        {
            if (!_hasBeenTriggered)
            {
            _hasBeenTriggered = true;
            OnPuzzleTriggered?.Invoke();
            Debug.Log($"'{_puzzleName}' has been triggered.");
            }
        }

        /// <summary>
        /// Resets all elements of this puzzle to their initial state.
        /// Also resets the solved flag so the puzzle can be completed again.
        /// Does NOT reset _hasBeenSolvedOnce (save data is preserved).
        /// </summary>
        public void ResetPuzzle()
        {
            if (IsSolved)
            {
                Debug.Log($"[PuzzleManager] '{_puzzleName}' is already solved — reset blocked.");
                return;
            }

            foreach (PuzzleElement element in _elements)
            {
                if (element != null)
                    element.ResetElement();
            }

            IsSolved = false;
            OnPuzzleReset?.Invoke();
            Debug.Log($"[PuzzleManager] '{_puzzleName}' has been reset.");
        }

        /// <summary>
        /// Forces this puzzle to be solved regardless of element states.
        /// Use for ExternalEvent mode or cheat/debug purposes.
        /// </summary>
        public void CompletePuzzle()
        {
            if (IsSolved) return;

            SetSolved();
        }

        /// <summary>
        /// Restores the solved state from a save file without firing events again.
        /// Call this during game load when the save system reports this puzzle was already solved.
        /// </summary>
        public void LoadSolvedState(bool wasSolved)
        {
            if (!wasSolved) return;

            IsSolved = true;
            _hasBeenSolvedOnce = true;
            Debug.Log($"[PuzzleManager] '{_puzzleName}' loaded as already solved.");
        }

        /// <summary>
        /// Returns all data needed to save this puzzle's state.
        /// Expand this struct when you build the save system.
        /// </summary>
        public PuzzleSaveData GetSaveData()
        {
            return new PuzzleSaveData
            {
                PuzzleId = _puzzleId,
                IsSolved = _hasBeenSolvedOnce,
                WasPuzzleActivated = _hasBeenTriggered
            };
        }

        #endregion

        #region Private Methods

        private void OnAnyElementStateChanged()
        {
            if (_solveMode != SolveMode.AllElements) return;
            EvaluateSolvedCondition();
        }

        private void EvaluateSolvedCondition()
        {
            if (IsSolved) return;

            List<PuzzleElement> required = GetRequiredElements();
            if (required.Count == 0) return;

            foreach (PuzzleElement element in required)
            {
                if (!element.IsSolved) return;
            }

            SetSolved();
        }

        private void SetSolved()
        {
            IsSolved = true;
            _hasBeenSolvedOnce = true;

            Debug.Log($"[PuzzleManager] '{_puzzleName}' solved!");
            OnPuzzleSolved?.Invoke();
        }

        private List<PuzzleElement> GetRequiredElements()
        {
            List<PuzzleElement> required = new();
            foreach (PuzzleElement element in _elements)
            {
                if (element != null && element.RequiredForSolution)
                    required.Add(element);
            }
            return required;
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            // Draw a sphere above the manager to spot it easily in Scene view
            Gizmos.color = IsSolved ? Color.green : new Color(1f, 0.6f, 0f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.3f);

#if UNITY_EDITOR
            UnityEditor.Handles.color = IsSolved ? Color.green : Color.white;
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 1f,
                $"{_puzzleName}\n{(IsSolved ? "SOLVED" : $"{Progress * 100f:F0}%")}"
            );
#endif
        }

        #endregion
    }

    #region Save Data

    /// <summary>
    /// Lightweight struct representing the save state of a puzzle.
    /// Plug this into your save system when you build it.
    /// </summary>
    [Serializable]
    public struct PuzzleSaveData
    {
        //Unique identifier matching PuzzleManager._puzzleId
        public string PuzzleId;

        //True if the player has solved this puzzle
        public bool IsSolved;

        //True if the puzzle has already been triggered (presentation cinematic, dialogue...)
        public bool WasPuzzleActivated;
    }

    #endregion
}