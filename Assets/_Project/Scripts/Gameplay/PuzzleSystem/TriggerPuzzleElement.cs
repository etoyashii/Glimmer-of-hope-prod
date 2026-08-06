using UnityEngine;
using UnityEngine.Events;

namespace GlimmerOfHope.Gameplay.Puzzles
{
    /// <summary>
    /// A puzzle element solved once the player enters its trigger collider,
    /// for example a rune, a light, or a marker to walk past. Stays solved
    /// permanently until the whole puzzle is reset via ResetElement().
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TriggerPuzzleElement : PuzzleElement
    {
        #region Serialized Fields

        [Header("Trigger Element")]
        [Tooltip("Tag the entering collider must have to activate this element.")]
        [SerializeField] private string _playerTag = "Player";

        [Header("Events")]
        [Tooltip("Fired the moment this element is activated, use for VFX, sound, color change.")]
        public UnityEvent OnActivated;

        #endregion

        #region Private Fields

        private bool _isActivated;

        #endregion

        #region Public Properties

        public bool IsActivated => _isActivated;

        #endregion

        #region Unity Lifecycle

        private void OnTriggerEnter(Collider other)
        {
            if (_isActivated) return;
            if (!other.CompareTag(_playerTag)) return;

            Debug.Log($"TriggerPuzzleElement activated by {other.name}.");

            Activate();
        }

        #endregion

        #region PuzzleElement Implementation

        public override void CheckSolvedState()
        {
            // Solved state is event driven, set directly in Activate().
            // Nothing to poll here, same approach as SwitchPuzzleElement.
        }

        protected override void OnReset()
        {
            _isActivated = false;
        }

        #endregion

        #region Private Methods

        private void Activate()
        {
            _isActivated = true;
            SetSolved(true);
            OnActivated?.Invoke();
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            Gizmos.color = _isActivated ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }

        #endregion
    }
}