using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GlimmerOfHope.Gameplay.Environment
{
    [System.Serializable]
    public class MovementKeypoint
    {
        [Tooltip("Position and rotation this keypoint moves to.")]
        public Transform target;

        [Tooltip("Duration of the movement into this keypoint, in seconds.")]
        public float duration = 1.5f;

        [Tooltip("If true, reaching this keypoint pauses the sequence until Trigger() is called again.")]
        public bool isBlocking = false;

        [Tooltip("Optional feedback fired the moment this keypoint is reached.")]
        public UnityEvent onReached;
    }

    /// <summary>
    /// Generic reusable component that moves and or rotates this object
    /// through an ordered list of keypoints, one segment per Trigger() call
    /// unless a keypoint is marked non blocking, in which case it chains
    /// straight into the next segment automatically. Reaching the end of
    /// the list, or looping back to the start, always requires a fresh
    /// Trigger() call to continue, since resuming in that case means
    /// picking a new direction. Meant to be triggered by any external
    /// system, for example a wind skill via WindReactive, an Interactable
    /// OnInteracted event, or a PuzzleManager OnPuzzleSolved event. This
    /// component has no knowledge of what triggers it.
    /// </summary>
    public class ScriptedMovement : MonoBehaviour
    {
        #region Inner Types

        public enum LoopMode
        {
            /// <summary>Stops for good once the last keypoint is reached.</summary>
            None,
            /// <summary>After the last keypoint, wraps directly back to the start and continues forward.</summary>
            Loop,
            /// <summary>After the last keypoint, reverses direction back through the list, then forward again.</summary>
            PingPong
        }

        #endregion

        #region Serialized Fields

        [Header("Keypoints")]
        [Tooltip("Ordered list of stops. The implicit start, index -1, is this object's initial transform.")]
        [SerializeField] private List<MovementKeypoint> _keypoints = new();

        [SerializeField] private bool _movePosition = true;
        [SerializeField] private bool _moveRotation = true;

        [Header("Timing")]
        [Tooltip("Eases each segment over time, X axis is normalized time, Y axis is progress.")]
        [SerializeField] private AnimationCurve _easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Looping")]
        [SerializeField] private LoopMode _loopMode = LoopMode.None;

        [Header("Physics")]
        [Tooltip("If this object has a Rigidbody, it is set to kinematic during each segment so the path stays exact regardless of physics.")]
        [SerializeField] private bool _keepKinematicAfterMovement = true;

        [Header("Events")]
        [Tooltip("Fired once at the start of every Trigger() call that actually starts moving.")]
        public UnityEvent OnMovementStarted;

        [Tooltip("Fired whenever the sequence stops and needs a fresh Trigger() to continue, but has not fully ended.")]
        public UnityEvent OnMovementPaused;

        [Tooltip("Fired only when the sequence truly ends, LoopMode None reaching its last keypoint.")]
        public UnityEvent OnSequenceCompleted;

        #endregion

        #region Private Fields

        private Rigidbody _rb;
        private bool _wasKinematicBeforeMove;

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;

        private int _currentIndex = -1;
        private int _direction = 1;
        private bool _sequenceEnded;
        private Coroutine _moveRoutine;

        #endregion

        #region Public Properties

        public bool IsMoving => _moveRoutine != null;
        public bool IsSequenceEnded => _sequenceEnded;
        public int CurrentKeypointIndex => _currentIndex;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();

            _initialPosition = transform.position;
            _initialRotation = transform.rotation;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Advances the sequence by one segment, or several chained segments
        /// if the keypoints in between are not marked blocking. Ignored while
        /// already moving, or once the sequence has fully ended in None mode.
        /// </summary>
        public void Trigger()
        {
            if (_keypoints.Count == 0) return;
            if (_sequenceEnded) return;
            if (_moveRoutine != null) return;

            _moveRoutine = StartCoroutine(RunSequenceStep());
        }

        /// <summary>
        /// Restores this object to its initial position, rotation and
        /// physics state, and allows Trigger() to be used again from the start.
        /// Wire this to a puzzle reset event if needed.
        /// </summary>
        public void ResetMovement()
        {
            if (_moveRoutine != null)
            {
                StopCoroutine(_moveRoutine);
                _moveRoutine = null;
            }

            transform.position = _initialPosition;
            transform.rotation = _initialRotation;

            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = _wasKinematicBeforeMove;
            }

            _currentIndex = -1;
            _direction = 1;
            _sequenceEnded = false;
        }

        #endregion

        #region Private Methods - Sequence

        private IEnumerator RunSequenceStep()
        {
            OnMovementStarted?.Invoke();

            while (true)
            {
                int nextIndex = _currentIndex + _direction;

                if (nextIndex < -1 || nextIndex > _keypoints.Count - 1)
                {
                    if (_loopMode == LoopMode.None)
                    {
                        _sequenceEnded = true;
                        _moveRoutine = null;
                        OnSequenceCompleted?.Invoke();
                        yield break;
                    }

                    if (_loopMode == LoopMode.Loop)
                    {
                        nextIndex = -1;
                    }
                    else
                    {
                        _direction = -_direction;
                        nextIndex = _currentIndex + _direction;
                    }
                }

                yield return MoveToIndex(_currentIndex, nextIndex);
                _currentIndex = nextIndex;

                if (_currentIndex >= 0)
                    _keypoints[_currentIndex].onReached?.Invoke();

                int peekIndex = _currentIndex + _direction;
                bool nextIsOutOfBounds = peekIndex < -1 || peekIndex > _keypoints.Count - 1;
                bool manualBlock = _currentIndex >= 0 && _keypoints[_currentIndex].isBlocking;

                if (nextIsOutOfBounds && _loopMode == LoopMode.None)
                {
                    _sequenceEnded = true;
                    _moveRoutine = null;
                    OnSequenceCompleted?.Invoke();
                    yield break;
                }

                if (nextIsOutOfBounds || manualBlock)
                {
                    _moveRoutine = null;
                    OnMovementPaused?.Invoke();
                    yield break;
                }
            }
        }

        private float GetDurationForTransition(int fromIndex, int toIndex)
        {
            if (toIndex >= 0) return _keypoints[toIndex].duration;

            // Returning to the start reuses the duration of the keypoint being left
            return fromIndex >= 0 ? _keypoints[fromIndex].duration : 1f;
        }

        private IEnumerator MoveToIndex(int fromIndex, int toIndex)
        {
            Vector3 originPosition = transform.position;
            Quaternion originRotation = transform.rotation;

            Vector3 targetPosition = toIndex >= 0 ? _keypoints[toIndex].target.position : _initialPosition;
            Quaternion targetRotation = toIndex >= 0 ? _keypoints[toIndex].target.rotation : _initialRotation;

            float duration = GetDurationForTransition(fromIndex, toIndex);

            if (_rb != null)
            {
                _wasKinematicBeforeMove = _rb.isKinematic;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                float progress = _easeCurve.Evaluate(duration > 0f ? elapsed / duration : 1f);

                Vector3 nextPosition = _movePosition
                    ? Vector3.Lerp(originPosition, targetPosition, progress)
                    : transform.position;

                Quaternion nextRotation = _moveRotation
                    ? Quaternion.Slerp(originRotation, targetRotation, progress)
                    : transform.rotation;

                if (_rb != null)
                {
                    _rb.MovePosition(nextPosition);
                    _rb.MoveRotation(nextRotation);
                    yield return new WaitForFixedUpdate();
                }
                else
                {
                    transform.SetPositionAndRotation(nextPosition, nextRotation);
                    yield return null;
                }

                elapsed += Time.deltaTime;
            }

            Vector3 finalPosition = _movePosition ? targetPosition : transform.position;
            Quaternion finalRotation = _moveRotation ? targetRotation : transform.rotation;

            if (_rb != null)
            {
                _rb.MovePosition(finalPosition);
                _rb.MoveRotation(finalRotation);
                _rb.isKinematic = _keepKinematicAfterMovement;
            }
            else
            {
                transform.SetPositionAndRotation(finalPosition, finalRotation);
            }
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            if (_keypoints == null || _keypoints.Count == 0) return;

            Vector3 previousPoint = Application.isPlaying ? _initialPosition : transform.position;

            for (int i = 0; i < _keypoints.Count; i++)
            {
                if (_keypoints[i].target == null) continue;

                Gizmos.color = i <= _currentIndex ? Color.green : Color.cyan;
                Gizmos.DrawLine(previousPoint, _keypoints[i].target.position);
                Gizmos.DrawWireSphere(_keypoints[i].target.position, 0.15f);

                previousPoint = _keypoints[i].target.position;
            }
        }

        #endregion
    }
}