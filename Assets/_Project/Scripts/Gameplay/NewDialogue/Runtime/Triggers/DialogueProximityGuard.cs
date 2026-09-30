using UnityEngine;

namespace GlimmerOfHope.Gameplay.NewDialogue
{
    /// <summary>
    /// take the player's position the moment a dialogue starts, then if the player walk more than the allowed distance dialog cut.
    /// </summary>
    public class DialogueProximityGuard
    {
        #region Private Fields
        private readonly string _playerTag;
        private readonly float _maxDistance;

        private Transform _playerTransform;
        private Vector3 _startPosition;
        private bool _isTracking;
        #endregion

        #region Public Methods
        public DialogueProximityGuard(string playerTag, float maxDistance)
        {
            _playerTag = playerTag;
            _maxDistance = maxDistance;
        }

        //Call to take the player's current position
        public void BeginTracking()
        {
            _isTracking = false;

            if (_maxDistance <= 0f) return;
            if (_playerTransform == null && !TryFindPlayer()) return;

            _startPosition = _playerTransform.position;
            _isTracking = true;
        }

        //Call to stop taking player position
        public void StopTracking()
        {
            _isTracking = false;
        }

        //True if tracking is active and the player has walked more than maxDistance from where they were when BeginTracking was called.
        public bool HasPlayerMovedTooFar()
        {
            if (!_isTracking || _playerTransform == null) return false;

            float distance = Vector3.Distance(_playerTransform.position, _startPosition);
            return distance > _maxDistance;
        }
        #endregion

        #region Private Methods
        private bool TryFindPlayer()
        {
            var playerObject = GameObject.FindGameObjectWithTag(_playerTag);
            if (playerObject == null) return false;

            _playerTransform = playerObject.transform;
            return true;
        }
        #endregion
    }
}