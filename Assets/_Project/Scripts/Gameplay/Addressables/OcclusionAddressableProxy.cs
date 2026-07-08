using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace OcclusionAddressables
{
    /// <summary>Manages loading, unloading, and ghost spawning for addressable objects based on visibility.</summary>
    [DisallowMultipleComponent]
    public class OcclusionAddressableProxy : MonoBehaviour
    {
        #region Serialized Fields
        [Tooltip("Clé Addressable du prefab (remplie par le converter)")]
        public string addressableKey; // Key to load the prefab from Addressables

        [Tooltip("Délai avant unload effectif — évite les micro-unloads quand on passe brièvement hors range")]
        [Range(0f, 10f)]
        public float unloadDelay = 1.5f; // Delay before unloading to avoid rapid load/unload cycles

        [Tooltip("Si true : SetActive(false) au lieu de détruire + libérer mémoire")]
        public bool keepInMemory = false; // If true, disable instead of destroying the object
        #endregion

        #region Public Properties
        [HideInInspector] public Vector3 savedPosition; 
        [HideInInspector] public Quaternion savedRotation; 
        [HideInInspector] public Vector3 savedScale; 
        [HideInInspector] public Transform savedParent; // Parent saved before unloading

        // Internal flag: indicates this proxy is attached to a ghost.
        // Start() should NOT treat it as a loaded object.
        [HideInInspector] public bool isGhostProxy = false;

        public bool IsLoaded => _isLoaded;
        public bool IsVisible => _isVisible;
        #endregion

        #region Private Fields
        private AsyncOperationHandle<GameObject> _handle; // Handle for the Addressables async operation
        private bool _isLoaded = false;
        private bool _isVisible = false; 
        private bool _isUnloading = false;
        private Bounds _cachedBounds; // Cached bounds of the object for performance
        #endregion

        #region Unity Lifecycle
        private void Awake() => SaveTransform(); // Save transform data when the object is created

        private void Start()
        {
            // Ghost proxies should not register as active objects:
            // GhostVisibilityWatcher manages their lifecycle.
            if (isGhostProxy) return;

            _cachedBounds = ComputeBounds(); // Calculate and cache the bounds
            _isLoaded = true;
            _isVisible = true;
            OcclusionAddressableManager.Instance.RegisterProxy(this); // Register with the manager
        }

        private void OnDestroy()
        {
            if (!isGhostProxy)
                OcclusionAddressableManager.Instance?.UnregisterProxy(this); // Unregister when destroyed
        }
        #endregion

        #region Public Methods
        /// <summary>Called by the manager to update visibility state.</summary>
        public void SetVisibilityFromManager(bool nowVisible)
        {
            if (nowVisible == _isVisible) return; 
            _isVisible = nowVisible;

            if (nowVisible)
            {
                // If we were unloading, stop it
                if (_isUnloading)
                {
                    StopAllCoroutines();
                    _isUnloading = false;
                }
                // If not loaded, start loading
                if (!_isLoaded)
                    StartCoroutine(LoadAndInstantiate());
            }
            else
            {
                // If loaded and not already unloading, start unloading
                if (_isLoaded && !_isUnloading)
                    StartCoroutine(UnloadAfterDelay());
            }
        }

        /// <summary>Returns the world-space bounds of the object.</summary>
        public Bounds GetWorldBounds()
        {
            if (gameObject.activeInHierarchy) return ComputeBounds(); // Compute bounds if active
            return new Bounds(savedPosition, _cachedBounds.size); // Use cached bounds if inactive
        }
        #endregion

        #region Private Methods
        /// <summary>Loads and instantiates the prefab asynchronously.</summary>
        private IEnumerator LoadAndInstantiate()
        {
            if (string.IsNullOrEmpty(addressableKey))
            {
                Debug.LogWarning($"[OcclusionProxy] Empty key on {name}");
                yield break;
            }

            // Start loading the prefab
            _handle = Addressables.InstantiateAsync(addressableKey, savedPosition, savedRotation, savedParent);
            yield return _handle;

            if (_handle.Status == AsyncOperationStatus.Succeeded)
            {
                var inst = _handle.Result;
                inst.transform.localScale = savedScale;
                inst.name = gameObject.name.Replace("[Ghost] ", ""); // Clean up the name

                // Transfer the proxy to the new instance
                var np = inst.GetComponent<OcclusionAddressableProxy>();
                if (np == null)
                {
                    np = inst.AddComponent<OcclusionAddressableProxy>();
                }
                else
                {
                    // Start() already registered then unregister to avoid duplicates
                    OcclusionAddressableManager.Instance?.UnregisterProxy(np);
                }

                // Copy all settings from this proxy to the new one
                np.addressableKey = addressableKey;
                np.unloadDelay = unloadDelay;
                np.keepInMemory = keepInMemory;
                np.isGhostProxy = false; // This is a real loaded object
                np._handle = _handle;
                np._isLoaded = true;
                np._isVisible = true;
                np.savedPosition = savedPosition;
                np.savedRotation = savedRotation;
                np.savedScale = savedScale;
                np.savedParent = savedParent;
                np._cachedBounds = _cachedBounds;

                // Manually register because Start() was already called
                OcclusionAddressableManager.Instance.RegisterProxy(np);

                Destroy(gameObject); // Destroy the ghost
            }
            else
            {
                Debug.LogError($"[OcclusionProxy] Failed to load '{addressableKey}': {_handle.OperationException}");
            }
        }

        /// <summary>Unloads the object after a delay.</summary>
        private IEnumerator UnloadAfterDelay()
        {
            _isUnloading = true;
            yield return new WaitForSeconds(unloadDelay); // Wait for the delay

            if (_isVisible) { _isUnloading = false; yield break; } // Cancel if visible again

            SaveTransform(); // Save transform before unloading

            if (keepInMemory)
            {
                // Just disable the object if we want to keep it in memory
                gameObject.SetActive(false);
                _isLoaded = false;
                _isUnloading = false;
                yield break;
            }

            // Unregister and release the object
            OcclusionAddressableManager.Instance?.UnregisterProxy(this);

            if (_handle.IsValid())
                Addressables.ReleaseInstance(_handle);

            _isLoaded = false;
            _isUnloading = false;

            SpawnGhost(); // Create a ghost to replace this object
            Destroy(gameObject); // Destroy the object
        }

        /// <summary>Creates a ghost object to replace this proxy when unloaded.</summary>
        private void SpawnGhost()
        {
            // Create a new GameObject for the ghost
            var ghost = new GameObject($"[Ghost] {name}");
            ghost.transform.SetParent(savedParent);
            ghost.transform.SetPositionAndRotation(savedPosition, savedRotation);
            ghost.transform.localScale = savedScale;

            // Add a BoxCollider to detect when the ghost is near the camera
            var bc = ghost.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = _cachedBounds.size.magnitude < 0.1f ? Vector3.one : _cachedBounds.size;

            // Add a proxy component to the ghost
            var gp = ghost.AddComponent<OcclusionAddressableProxy>();
            gp.addressableKey = addressableKey;
            gp.unloadDelay = unloadDelay;
            gp.keepInMemory = keepInMemory;
            gp.isGhostProxy = true; // This prevents Start() from registering it as active
            gp._isLoaded = false;
            gp._isVisible = false;
            gp.savedPosition = savedPosition;
            gp.savedRotation = savedRotation;
            gp.savedScale = savedScale;
            gp.savedParent = savedParent;
            gp._cachedBounds = _cachedBounds;

            // Add the GhostVisibilityWatcher to manage when to reload
            ghost.AddComponent<GhostVisibilityWatcher>();
        }

        /// <summary>Saves the current transform data.</summary>
        private void SaveTransform()
        {
            savedPosition = transform.position;
            savedRotation = transform.rotation;
            savedScale = transform.lossyScale;
            savedParent = transform.parent;
        }

        /// <summary>Computes the bounds of the object based on its renderers.</summary>
        private Bounds ComputeBounds()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(transform.position, Vector3.one * 2f); // Default bounds if no renderers
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds); // Include all renderers in the bounds
            return b;
        }
        #endregion
    }
}