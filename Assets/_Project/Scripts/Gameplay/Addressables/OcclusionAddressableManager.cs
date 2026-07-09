using System.Collections.Generic;
using UnityEngine;

namespace OcclusionAddressables
{
    /// <summary>Manages loading and unloading of objects based on visibility and distance from the camera.</summary>
    [DefaultExecutionOrder(-100)]
    public class OcclusionAddressableManager : MonoBehaviour
    {
        #region Public Properties
        /// <summary>Singleton instance. Creates one if it doesn't exist.</summary>
        public static OcclusionAddressableManager Instance
        {
            get
            {
                // If no instance exists, create a new GameObject and add this component
                if (_instance == null)
                {
                    var go = new GameObject("[OcclusionAddressableManager]");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<OcclusionAddressableManager>();
                }
                return _instance;
            }
        }

        // Count of registered proxies and ghosts
        public int ProxyCount => _proxies.Count;
        public int GhostCount => _ghosts.Count;
        public int TotalLoaded => _totalLoaded;
        public int TotalUnloaded => _totalUnloaded;
        #endregion

        #region Serialized Fields
        [Header("Visibilité")]
        [Tooltip("Distance max à partir de laquelle un objet est unloadé")]
        [Range(5f, 500f)]
        public float unloadDistance = 50f; // Objects farther than this are unloaded

        [Tooltip("Distance à partir de laquelle un ghost recharge son prefab (doit être <= unloadDistance)")]
        [Range(5f, 500f)]
        public float reloadDistance = 40f; // Ghosts closer than this will reload their prefab

        [Tooltip("Fréquence du check de visibilité (secondes)")]
        [Range(0.05f, 2f)]
        public float checkInterval = 0.2f; // How often to check visibility (in seconds)

        [Header("Debug")]
        public bool showDebugGUI = true; // Show debug info in the Game view
        public bool showDebugGizmos = true; // Show debug shapes in the Scene view
        public bool verboseLogs = false; // Print detailed logs to the console
        #endregion

        #region Private Fields
        private static OcclusionAddressableManager _instance;
        private readonly List<OcclusionAddressableProxy> _proxies = new(); // List of all proxies
        private readonly List<GhostVisibilityWatcher> _ghosts = new(); // List of all ghosts
        private Camera _mainCam; // Reference to the main camera
        private float _lastCheck; // Time of the last visibility check
        private int _totalLoaded; // Total number of loaded proxies
        private int _totalUnloaded; // Total number of unloaded proxies
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Only one instance of this manager is allowed
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject); // Keep this object alive between scenes
        }

        private void Update()
        {
            // Get the main camera if not already set
            if (_mainCam == null) _mainCam = Camera.main;
            if (_mainCam == null) return;

            // Only check visibility at the specified interval
            if (Time.time - _lastCheck < checkInterval) return;
            _lastCheck = Time.time;

            Vector3 camPos = _mainCam.transform.position;
            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(_mainCam);

            // Check visibility for proxies and ghosts
            CheckProxies(camPos, frustumPlanes);
            CheckGhosts(camPos);
        }
        #endregion

        #region Public Methods
        /// <summary>Add a proxy to the list of tracked objects.</summary>
        public void RegisterProxy(OcclusionAddressableProxy proxy)
        {
            if (!_proxies.Contains(proxy))
            {
                _proxies.Add(proxy);
                _totalLoaded++;
                if (verboseLogs) Debug.Log($"[OcclusionManager] Added Proxy: {proxy.name} (Total: {_proxies.Count})");
            }
        }

        /// <summary>Remove a proxy from the list of tracked objects.</summary>
        public void UnregisterProxy(OcclusionAddressableProxy proxy)
        {
            _proxies.Remove(proxy);
            _totalUnloaded++;
            if (verboseLogs) Debug.Log($"[OcclusionManager] Removed Proxy: {proxy.name} (Remaining: {_proxies.Count})");
        }

        /// <summary>Add a ghost to the list of tracked objects.</summary>
        public void RegisterGhost(GhostVisibilityWatcher ghost)
        {
            if (!_ghosts.Contains(ghost)) _ghosts.Add(ghost);
            if (verboseLogs) Debug.Log($"[OcclusionManager] Added Ghost: {ghost.name}");
        }

        /// <summary>Remove a ghost from the list of tracked objects.</summary>
        public void UnregisterGhost(GhostVisibilityWatcher ghost)
        {
            _ghosts.Remove(ghost);
        }
        #endregion

        #region Private Methods
        /// <summary>Check if proxies are visible and update their state.</summary>
        private void CheckProxies(Vector3 camPos, Plane[] frustumPlanes)
        {
            // Loop through proxies in reverse to safely remove items
            for (int i = _proxies.Count - 1; i >= 0; i--)
            {
                var proxy = _proxies[i];
                if (proxy == null)
                {
                    _proxies.RemoveAt(i);
                    continue;
                }

                // Calculate distance from camera to proxy
                float dist = Vector3.Distance(camPos, proxy.savedPosition);

                // Check if proxy is within range and visible in the camera's view
                bool inRange = dist <= unloadDistance;
                bool inFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, proxy.GetWorldBounds());
                bool visible = inRange && inFrustum;

                // Update proxy visibility
                proxy.SetVisibilityFromManager(visible);
            }
        }

        /// <summary>Check if ghosts are close enough to reload their prefabs.</summary>
        private void CheckGhosts(Vector3 camPos)
        {
            // Loop through ghosts in reverse to safely remove items
            for (int i = _ghosts.Count - 1; i >= 0; i--)
            {
                var ghost = _ghosts[i];
                if (ghost == null)
                {
                    _ghosts.RemoveAt(i);
                    continue;
                }

                // Calculate distance from camera to ghost
                float dist = Vector3.Distance(camPos, ghost.transform.position);
                if (dist <= reloadDistance)
                {
                    if (verboseLogs) Debug.Log($"[OcclusionManager] Ghost in reload range: {ghost.name}");
                    ghost.TriggerLoad(); // Tell the ghost to reload
                    _ghosts.RemoveAt(i); // Remove from the list after triggering
                }
            }
        }
        #endregion

        #region Editor
        private void OnValidate()
        {
            // Ensure reloadDistance is never greater than unloadDistance
            if (reloadDistance > unloadDistance)
                reloadDistance = unloadDistance;
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;
            if (_mainCam == null) _mainCam = Camera.main;
            if (_mainCam == null) return;

            Vector3 camPos = _mainCam.transform.position;

            // Draw unload distance sphere (red)
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.08f);
            Gizmos.DrawSphere(camPos, unloadDistance);
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(camPos, unloadDistance);

            // Draw reload distance sphere (green)
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.08f);
            Gizmos.DrawSphere(camPos, reloadDistance);
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(camPos, reloadDistance);

            // Draw active proxies as green boxes
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.6f);
            foreach (var proxy in _proxies)
            {
                if (proxy == null) continue;
                var b = proxy.GetWorldBounds();
                Gizmos.DrawWireCube(b.center, b.size);
                // Draw line from camera to proxy
                Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.2f);
                Gizmos.DrawLine(camPos, proxy.savedPosition);
                Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.6f);
            }

            // Draw ghosts as orange spheres
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
            foreach (var ghost in _ghosts)
            {
                if (ghost == null) continue;
                Gizmos.DrawWireSphere(ghost.transform.position, 0.5f);
                // Draw line from camera to ghost
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
                Gizmos.DrawLine(camPos, ghost.transform.position);
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
            }
        }
        #endregion
    }
}