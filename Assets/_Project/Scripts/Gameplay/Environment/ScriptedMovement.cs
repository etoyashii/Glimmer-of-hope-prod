using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace GlimmerOfHope.Gameplay.Environment
{
    /// <summary>
    /// Generic reusable component that moves and or rotates this object
    /// from its current state to a target transform over a fixed duration,
    /// following an easing curve. Meant to be triggered by any external
    /// system through Trigger(), for example a wind skill via WindReactive,
    /// an Interactable OnInteracted event, or a PuzzleManager OnPuzzleSolved
    /// event. This component has no knowledge of what triggers it.
    /// </summary>
    public class ScriptedMovement : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Target")]
        [Tooltip("Transform this object moves and or rotates towards.")]
        [SerializeField] private Transform _targetTransform;

        [SerializeField] private bool _movePosition = true;
        [SerializeField] private bool _moveRotation = true;

        [Header("Timing")]
        [SerializeField] private float _duration = 1.5f;

        [Tooltip("Eases the movement over time, X axis is normalized time, Y axis is progress.")]
        [SerializeField] private AnimationCurve _easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Physics")]
        [Tooltip("If this object has a Rigidbody, it is set to kinematic during the move so the path stays exact regardless of physics.")]
        [SerializeField] private bool _keepKinematicAfterMovement = true;

        [Header("Behaviour")]
        [Tooltip("If true, Trigger() only works once until ResetMovement() is called.")]
        [SerializeField] private bool _playOnce = true;

        [Header("Events")]
        public UnityEvent OnMovementStarted;
        public UnityEvent OnMovementCompleted;

        #endregion

        #region Private Fields

        private Rigidbody _rb;
        private bool _wasKinematicBeforeMove;

        private Vector3 _initialPosition;
        private Quaternion _initialRotation;

        private bool _hasPlayed;
        private Coroutine _moveRoutine;

        #endregion

        #region Public Properties

        public bool HasPlayed => _hasPlayed;
        public bool IsMoving => _moveRoutine != null;

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
        /// Starts the scripted movement towards the target transform.
        /// Safe to call multiple times, ignored while already playing or,
        /// if playOnce is set, once it has already completed.
        /// </summary>
        public void Trigger()
        {
            if (_targetTransform == null) return;
            if (_playOnce && _hasPlayed) return;
            if (_moveRoutine != null) return;

            _moveRoutine = StartCoroutine(MoveRoutine());
        }

        /// <summary>
        /// Restores this object to its initial position, rotation and
        /// physics state, and allows Trigger() to be used again.
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

            _hasPlayed = false;
        }

        #endregion

        #region Private Methods

        private IEnumerator MoveRoutine()
        {
            OnMovementStarted?.Invoke();

            Vector3 originPosition = transform.position;
            Quaternion originRotation = transform.rotation;

            if (_rb != null)
            {
                _wasKinematicBeforeMove = _rb.isKinematic;
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.isKinematic = true;
            }

            float elapsed = 0f;

            while (elapsed < _duration)
            {
                float progress = _easeCurve.Evaluate(elapsed / _duration);

                Vector3 nextPosition = _movePosition
                    ? Vector3.Lerp(originPosition, _targetTransform.position, progress)
                    : transform.position;

                Quaternion nextRotation = _moveRotation
                    ? Quaternion.Slerp(originRotation, _targetTransform.rotation, progress)
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

            // Snap exactly to the target to avoid floating point drift
            Vector3 finalPosition = _movePosition ? _targetTransform.position : transform.position;
            Quaternion finalRotation = _moveRotation ? _targetTransform.rotation : transform.rotation;

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

            _hasPlayed = true;
            _moveRoutine = null;

            OnMovementCompleted?.Invoke();
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            if (_targetTransform == null) return;

            Gizmos.color = _hasPlayed ? Color.green : Color.cyan;
            Gizmos.DrawLine(transform.position, _targetTransform.position);
            Gizmos.DrawWireSphere(_targetTransform.position, 0.2f);
        }

        #endregion
    }
}