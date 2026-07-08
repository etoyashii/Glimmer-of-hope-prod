using System.Collections.Generic;
using UnityEngine;

namespace OcclusionAddressables
{
    [DefaultExecutionOrder(-100)]
    public class OcclusionAddressableManager : MonoBehaviour
    {
        #region Public Properties
        public static OcclusionAddressableManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("[OcclusionAddressableManager]");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<OcclusionAddressableManager>();
                }
                return _instance;
            }
        }

        public int ProxyCount => _proxies.Count;
        public int GhostCount => _ghosts.Count;
        public int TotalLoaded => _totalLoaded;
        public int TotalUnloaded => _totalUnloaded;
        #endregion

        #region Serialized Fields
        [Header("Visibilité")]
        [Tooltip("Distance max à partir de laquelle un objet est unloadé")]
        [Range(5f, 500f)]
        public float unloadDistance = 50f;

        [Tooltip("Distance à partir de laquelle un ghost recharge son prefab (doit être <= unloadDistance)")]
        [Range(5f, 500f)]
        public float reloadDistance = 40f;

        [Tooltip("Fréquence du check de visibilité (secondes)")]
        [Range(0.05f, 2f)]
        public float checkInterval = 0.2f;

        [Header("Debug")]
        public bool showDebugGUI = true;
        public bool showDebugGizmos = true;
        public bool verboseLogs = false;
        #endregion

        #region Private Fields
        private static OcclusionAddressableManager _instance;
        private readonly List<OcclusionAddressableProxy> _proxies = new();
        private readonly List<GhostVisibilityWatcher> _ghosts = new();
        private Camera _mainCam;
        private float _lastCheck;
        private int _totalLoaded;
        private int _totalUnloaded;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(gameObject); return; }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (_mainCam == null) _mainCam = Camera.main;
            if (_mainCam == null) return;
            if (Time.time - _lastCheck < checkInterval) return;
            _lastCheck = Time.time;

            Vector3 camPos = _mainCam.transform.position;
            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(_mainCam);

            CheckProxies(camPos, frustumPlanes);
            CheckGhosts(camPos);
        }
        #endregion

        #region Public Methods
        public void RegisterProxy(OcclusionAddressableProxy proxy)
        {
            if (!_proxies.Contains(proxy))
            {
                _proxies.Add(proxy);
                _totalLoaded++;
                if (verboseLogs) Debug.Log($"[OcclusionManager] + Proxy : {proxy.name} ({_proxies.Count} total)");
            }
        }

        public void UnregisterProxy(OcclusionAddressableProxy proxy)
        {
            _proxies.Remove(proxy);
            _totalUnloaded++;
            if (verboseLogs) Debug.Log($"[OcclusionManager] - Proxy : {proxy.name} ({_proxies.Count} restants)");
        }

        public void RegisterGhost(GhostVisibilityWatcher ghost)
        {
            if (!_ghosts.Contains(ghost)) _ghosts.Add(ghost);
            if (verboseLogs) Debug.Log($"[OcclusionManager] + Ghost : {ghost.name}");
        }

        public void UnregisterGhost(GhostVisibilityWatcher ghost)
        {
            _ghosts.Remove(ghost);
        }
        #endregion

        #region Private Methods
        private void CheckProxies(Vector3 camPos, Plane[] frustumPlanes)
        {
            for (int i = _proxies.Count - 1; i >= 0; i--)
            {
                var proxy = _proxies[i];
                if (proxy == null) { _proxies.RemoveAt(i); continue; }

                float dist = Vector3.Distance(camPos, proxy.savedPosition);

                // Visible si : dans la distance ET dans le frustum
                bool inRange = dist <= unloadDistance;
                bool inFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, proxy.GetWorldBounds());
                bool visible = inRange && inFrustum;

                proxy.SetVisibilityFromManager(visible);
            }
        }

        private void CheckGhosts(Vector3 camPos)
        {
            for (int i = _ghosts.Count - 1; i >= 0; i--)
            {
                var ghost = _ghosts[i];
                if (ghost == null) { _ghosts.RemoveAt(i); continue; }

                float dist = Vector3.Distance(camPos, ghost.transform.position);
                if (dist <= reloadDistance)
                {
                    if (verboseLogs) Debug.Log($"[OcclusionManager] Ghost dans reloadDistance → load : {ghost.name}");
                    ghost.TriggerLoad();
                    _ghosts.RemoveAt(i);
                }
            }
        }
        #endregion

        #region Editor
        private void OnValidate()
        {
            // Garantir reloadDistance <= unloadDistance
            if (reloadDistance > unloadDistance)
                reloadDistance = unloadDistance;
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;
            if (_mainCam == null) _mainCam = Camera.main;
            if (_mainCam == null) return;

            Vector3 camPos = _mainCam.transform.position;

            // Sphère de déchargement (rouge)
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.08f);
            Gizmos.DrawSphere(camPos, unloadDistance);
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(camPos, unloadDistance);

            // Sphère de rechargement (vert)
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.08f);
            Gizmos.DrawSphere(camPos, reloadDistance);
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(camPos, reloadDistance);

            // Proxies actifs (boites vertes)
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.6f);
            foreach (var proxy in _proxies)
            {
                if (proxy == null) continue;
                var b = proxy.GetWorldBounds();
                Gizmos.DrawWireCube(b.center, b.size);
                // Ligne caméra → proxy
                Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.2f);
                Gizmos.DrawLine(camPos, proxy.savedPosition);
                Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.6f);
            }

            // Ghosts (sphères orange)
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
            foreach (var ghost in _ghosts)
            {
                if (ghost == null) continue;
                Gizmos.DrawWireSphere(ghost.transform.position, 0.5f);
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
                Gizmos.DrawLine(camPos, ghost.transform.position);
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
            }
        }
        #endregion
    }
}