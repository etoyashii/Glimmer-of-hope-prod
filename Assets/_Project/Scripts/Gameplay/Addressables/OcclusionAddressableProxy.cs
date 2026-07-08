using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace OcclusionAddressables
{
    [DisallowMultipleComponent]
    public class OcclusionAddressableProxy : MonoBehaviour
    {
        #region Serialized Fields
        [Tooltip("Clé Addressable du prefab (remplie par le converter)")]
        public string addressableKey;

        [Tooltip("Délai avant unload effectif — évite les micro-unloads quand on passe brièvement hors range")]
        [Range(0f, 10f)]
        public float unloadDelay = 1.5f;

        [Tooltip("Si true : SetActive(false) au lieu de détruire + libérer mémoire")]
        public bool keepInMemory = false;
        #endregion

        #region Public Properties
        [HideInInspector] public Vector3 savedPosition;
        [HideInInspector] public Quaternion savedRotation;
        [HideInInspector] public Vector3 savedScale;
        [HideInInspector] public Transform savedParent;

        // Flag interne : indique que ce proxy est attaché à un ghost,
        // Start() ne doit PAS le traiter comme un objet chargé.
        [HideInInspector] public bool isGhostProxy = false;

        public bool IsLoaded => _isLoaded;
        public bool IsVisible => _isVisible;
        #endregion

        #region Private Fields
        private AsyncOperationHandle<GameObject> _handle;
        private bool _isLoaded = false;
        private bool _isVisible = false;
        private bool _isUnloading = false;
        private Bounds _cachedBounds;
        #endregion

        #region Unity Lifecycle
        private void Awake() => SaveTransform();

        private void Start()
        {
            // Les proxies fantômes ne doivent pas s'enregistrer comme objets actifs :
            // c'est le GhostVisibilityWatcher qui gère leur cycle de vie.
            if (isGhostProxy) return;

            _cachedBounds = ComputeBounds();
            _isLoaded = true;
            _isVisible = true;
            OcclusionAddressableManager.Instance.RegisterProxy(this);
        }

        private void OnDestroy()
        {
            if (!isGhostProxy)
                OcclusionAddressableManager.Instance?.UnregisterProxy(this);
        }
        #endregion

        #region Public Methods
        public void SetVisibilityFromManager(bool nowVisible)
        {
            if (nowVisible == _isVisible) return;
            _isVisible = nowVisible;

            if (nowVisible)
            {
                if (_isUnloading)
                {
                    StopAllCoroutines();
                    _isUnloading = false;
                }
                if (!_isLoaded)
                    StartCoroutine(LoadAndInstantiate());
            }
            else
            {
                if (_isLoaded && !_isUnloading)
                    StartCoroutine(UnloadAfterDelay());
            }
        }

        public Bounds GetWorldBounds()
        {
            if (gameObject.activeInHierarchy) return ComputeBounds();
            return new Bounds(savedPosition, _cachedBounds.size);
        }
        #endregion

        #region Private Methods
        private IEnumerator LoadAndInstantiate()
        {
            if (string.IsNullOrEmpty(addressableKey))
            {
                Debug.LogWarning($"[OcclusionProxy] Clé vide sur {name}");
                yield break;
            }

            _handle = Addressables.InstantiateAsync(addressableKey, savedPosition, savedRotation, savedParent);
            yield return _handle;

            if (_handle.Status == AsyncOperationStatus.Succeeded)
            {
                var inst = _handle.Result;
                inst.transform.localScale = savedScale;
                inst.name = gameObject.name.Replace("[Ghost] ", ""); // nettoyer le nom

                // Transférer le proxy sur la nouvelle instance
                var np = inst.GetComponent<OcclusionAddressableProxy>();
                if (np == null)
                {
                    np = inst.AddComponent<OcclusionAddressableProxy>();
                }
                else
                {
                    // Start() a déjà enregistré np → on le désenregistre pour éviter le doublon
                    OcclusionAddressableManager.Instance?.UnregisterProxy(np);
                }
                np.addressableKey = addressableKey;
                np.unloadDelay = unloadDelay;
                np.keepInMemory = keepInMemory;
                np.isGhostProxy = false; // c'est un vrai objet chargé
                np._handle = _handle;
                np._isLoaded = true;
                np._isVisible = true;
                np.savedPosition = savedPosition;
                np.savedRotation = savedRotation;
                np.savedScale = savedScale;
                np.savedParent = savedParent;
                np._cachedBounds = _cachedBounds;

                // Enregistrer manuellement car Start() a déjà été appelé
                // (le composant est ajouté dynamiquement sur une instance existante)
                OcclusionAddressableManager.Instance.RegisterProxy(np);

                Destroy(gameObject); // détruire le ghost
            }
            else
            {
                Debug.LogError($"[OcclusionProxy] Échec load '{addressableKey}': {_handle.OperationException}");
            }
        }

        private IEnumerator UnloadAfterDelay()
        {
            _isUnloading = true;
            yield return new WaitForSeconds(unloadDelay);

            if (_isVisible) { _isUnloading = false; yield break; }

            SaveTransform();

            if (keepInMemory)
            {
                gameObject.SetActive(false);
                _isLoaded = false;
                _isUnloading = false;
                yield break;
            }

            OcclusionAddressableManager.Instance?.UnregisterProxy(this);

            if (_handle.IsValid())
                Addressables.ReleaseInstance(_handle);

            _isLoaded = false;
            _isUnloading = false;

            SpawnGhost();
            Destroy(gameObject);
        }

        private void SpawnGhost()
        {
            var ghost = new GameObject($"[Ghost] {name}");
            ghost.transform.SetParent(savedParent);
            ghost.transform.SetPositionAndRotation(savedPosition, savedRotation);
            ghost.transform.localScale = savedScale;

            var bc = ghost.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = _cachedBounds.size.magnitude < 0.1f ? Vector3.one : _cachedBounds.size;

            var gp = ghost.AddComponent<OcclusionAddressableProxy>();
            gp.addressableKey = addressableKey;
            gp.unloadDelay = unloadDelay;
            gp.keepInMemory = keepInMemory;
            gp.isGhostProxy = true;  // ← empêche Start() de l'enregistrer comme proxy actif
            gp._isLoaded = false;
            gp._isVisible = false;
            gp.savedPosition = savedPosition;
            gp.savedRotation = savedRotation;
            gp.savedScale = savedScale;
            gp.savedParent = savedParent;
            gp._cachedBounds = _cachedBounds;

            ghost.AddComponent<GhostVisibilityWatcher>();
        }

        private void SaveTransform()
        {
            savedPosition = transform.position;
            savedRotation = transform.rotation;
            savedScale = transform.lossyScale;
            savedParent = transform.parent;
        }

        private Bounds ComputeBounds()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(transform.position, Vector3.one * 2f);
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            return b;
        }
        #endregion
    }
}