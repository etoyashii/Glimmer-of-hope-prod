using UnityEngine;

namespace GlimmerOfHope.Gameplay.Firefly
{
    public partial class FireflyAI
    {
        #region Private Fields

        private Transform _playerBoundsOwner;
        private Collider _playerCollider;

        private Transform _lanternBoundsOwner;
        private Collider _lanternCollider;
        private Renderer _lanternRenderer;

        #endregion

        #region Private Methods

        private Vector3 GetPlayerCenter()
        {
            if (_playerTransform == null) return transform.position;

            if (_playerBoundsOwner != _playerTransform)
            {
                _playerBoundsOwner = _playerTransform;
                _playerCollider = _playerTransform.GetComponentInChildren<Collider>();
            }

            if (_playerCollider == null || !_playerCollider.enabled) return _playerTransform.position;

            return _playerCollider.bounds.center;
        }

        private Vector3 GetLanternCenter()
        {
            if (_lanternBoundsOwner != _lanternTransform)
            {
                _lanternBoundsOwner = _lanternTransform;
                _lanternCollider = _lanternTransform.GetComponentInChildren<Collider>();
                _lanternRenderer = _lanternTransform.GetComponentInChildren<Renderer>();
            }

            if (_lanternCollider != null && _lanternCollider.enabled) return _lanternCollider.bounds.center;
            if (_lanternRenderer != null && _lanternRenderer.enabled) return _lanternRenderer.bounds.center;

            return _lanternTransform.position;
        }

        #endregion
    }
}
