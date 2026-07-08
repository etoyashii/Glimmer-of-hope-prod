using UnityEngine;

namespace OcclusionAddressables
{
    #region Dependencies
    [RequireComponent(typeof(OcclusionAddressableProxy))]
    #endregion

    /// <summary>Watches for ghost visibility and triggers loading when in range.</summary>
    public class GhostVisibilityWatcher : MonoBehaviour
    {
        #region Private Fields
        private OcclusionAddressableProxy _proxy;
        private BoxCollider _col;
        private bool _loadTriggered;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _proxy = GetComponent<OcclusionAddressableProxy>();
            _col = GetComponent<BoxCollider>();
            OcclusionAddressableManager.Instance.RegisterGhost(this);
        }

        private void OnDestroy()
        {
            OcclusionAddressableManager.Instance?.UnregisterGhost(this);
        }
        #endregion

        #region Public Methods
        /// <summary>Called by the Manager when the ghost is within the reloadDistance.</summary>
        public void TriggerLoad()
        {
            if (_loadTriggered) return;
            _loadTriggered = true;
            // Force visibility → proxy triggers LoadAndInstantiate
            _proxy.SetVisibilityFromManager(true);
        }

        /// <summary>Returns the bounds of the ghost, either from its BoxCollider or a default size.</summary>
        public Bounds GetBounds()
        {
            if (_col != null) return _col.bounds;
            return new Bounds(transform.position, Vector3.one * 2f);
        }
        #endregion

        #region Editor
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f);
            var b = GetBounds();
            Gizmos.DrawWireCube(b.center, b.size);
        }
#endif
        #endregion
    }
}