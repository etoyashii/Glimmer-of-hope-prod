using UnityEngine;

namespace OcclusionAddressables
{
    #region Dependencies
    [RequireComponent(typeof(OcclusionAddressableProxy))]
    #endregion
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
        /// <summary>Appelé par le Manager quand le ghost est dans la reloadDistance.</summary>
        public void TriggerLoad()
        {
            if (_loadTriggered) return;
            _loadTriggered = true;
            // Forcer la visibilité → le proxy lance LoadAndInstantiate
            _proxy.SetVisibilityFromManager(true);
        }

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