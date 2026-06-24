using System;
using System.Collections;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// To generate platforms on walls or ground. The player can aim at a surface,
    /// and a ghost preview of the platform will appear.
    /// Pressing the skill button again confirms the spawn,
    /// and the platform rises from below the surface if ground or to the wall if walls.
    /// </summary>
    public class GeneratePlatform : Skills
    {
        #region Inner Types

        [Serializable]
        public class SurfaceEntry
        {
            [Tooltip("Detectable layer")]
            public LayerMask layer;

            [Tooltip("Prefab to spawn on this layer")]
            public GameObject platformPrefab;

            [Tooltip("Preview prefab (should use a transparent material)")]
            public GameObject preBuildPlatformPrefab;
        }

        private enum SpellState { Idle, Previewing }

        #endregion

        #region Serialized Fields

        [Header("Refs")]
        [Tooltip("Caster's transform from which raycasts are made")]
        [SerializeField] private Transform _playerTransform;

        [Header("Surfaces & Prefabs")]
        [Tooltip("Associate every layer with a different prefab")]
        [SerializeField] private SurfaceEntry[] _surfaceEntries;

        [Header("Detection Parameters")]
        [Tooltip("Raycast distance in front of the caster")]
        [SerializeField] private float _spawnDistance = 3f;

        [Tooltip("For walls => height from which the raycast is cast")]
        [SerializeField] private float _wallRaycastHeight = 1f;

        [Tooltip("For walls => max distance of raycast")]
        [SerializeField] private float _wallRaycastDistance = 5f;

        [Tooltip("For the ground => height from which the vertical raycast is cast")]
        [SerializeField] private float _groundRaycastOriginHeight = 5f;

        [Tooltip("For the ground => max distance of vertical raycast")]
        [SerializeField] private float _groundRaycastDistance = 20f;

        [Tooltip("Every detectable layer")]
        [SerializeField] private LayerMask _allDetectableLayers;

        [Header("Animation Parameters")]
        [Tooltip("Depth from which the platform rises")]
        [SerializeField] private float _startDepth = 2f;

        [Tooltip("Animation time (in seconds)")]
        [SerializeField] private float _riseDuration = 0.6f;

        [Tooltip("Final height of the platform (0 = flush with surface)")]
        [SerializeField] private float _targetHeightOffset = 0f;

        [Tooltip("Animation curve")]
        [SerializeField] private AnimationCurve _riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        #endregion

        #region Private Fields

        private SpellState _state = SpellState.Idle;

        // Preview ghost instance currently shown
        private GameObject _previewInstance;

        [Header("Preview")]
        [Tooltip("Layer assigned to the preview ghost so raycasts ignore it. Use the built-in 'Ignore Raycast' layer (2) or create a dedicated 'Preview' layer and exclude it from _allDetectableLayers.")]
        [SerializeField] private int _previewLayer = 2; // 2 = Ignore Raycast by default

        // Data resolved during preview, reused on confirm
        private GameObject _confirmedPrefab;
        private Vector3 _confirmedTargetPosition;
        private Vector3 _confirmedExitDirection;
        private bool _confirmedIsWall;

        #endregion

        #region Unity Lifecycle

        private void Update()
        {
            if (_state == SpellState.Previewing)
                UpdatePreview();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// First press : enter preview mode.
        /// Second press : confirm and spawn the platform.
        /// </summary>
        public override void PerformSkill()
        {
            if (_state == SpellState.Idle)
                EnterPreview();
            else
                ConfirmSpawn();
        }

        /// <summary>Cancel the preview without spawning anything.</summary>
        public void CancelPreview()
        {
            if (_state != SpellState.Previewing) return;

            ClearPreview();
            _state = SpellState.Idle;
        }

        #endregion

        #region Private Methods — Preview

        private void EnterPreview()
        {
            // Try to resolve a hit right away; abort if nothing detectable
            if (!TryResolveHit(out GameObject prefab, out GameObject preBuildPrefab,
                               out Vector3 targetPos, out Vector3 exitDir, out bool isWall))
            {
                Debug.Log("[GeneratePlatform] No detectable surface found.");
                return;
            }

            // Store resolved data for confirm step
            _confirmedPrefab = prefab;
            _confirmedTargetPosition = targetPos;
            _confirmedExitDirection = exitDir;
            _confirmedIsWall = isWall;

            // Spawn the ghost preview at final position (no rise animation)
            _previewInstance = Instantiate(preBuildPrefab, targetPos, Quaternion.identity);
            SetLayerRecursive(_previewInstance, _previewLayer);
            ApplyOrientation(_previewInstance, isWall);

            _state = SpellState.Previewing;
        }

        /// <summary>
        /// Called every frame while previewing.
        /// Re-resolves the raycast so the ghost follows the player's aim in real time.
        /// </summary>
        private void UpdatePreview()
        {
            if (!TryResolveHit(out GameObject prefab, out GameObject preBuildPrefab,
                               out Vector3 targetPos, out Vector3 exitDir, out bool isWall))
            {
                // Surface lost — hide ghost but stay in preview state
                if (_previewInstance != null)
                    _previewInstance.SetActive(false);
                return;
            }

            // Update stored data in case the player moved
            _confirmedPrefab = prefab;
            _confirmedTargetPosition = targetPos;
            _confirmedExitDirection = exitDir;
            _confirmedIsWall = isWall;

            // Move / swap ghost if needed
            if (_previewInstance == null || _previewInstance.activeSelf == false)
            {
                ClearPreview();
                _previewInstance = Instantiate(preBuildPrefab, targetPos, Quaternion.identity);
                SetLayerRecursive(_previewInstance, _previewLayer);
            }

            _previewInstance.SetActive(true);
            _previewInstance.transform.position = targetPos;
            ApplyOrientation(_previewInstance, isWall);
        }

        private void ConfirmSpawn()
        {
            ClearPreview();
            _state = SpellState.Idle;

            SpawnPlatform(_confirmedPrefab, _confirmedTargetPosition,
                          _confirmedExitDirection, _confirmedIsWall);
        }

        private void ClearPreview()
        {
            if (_previewInstance != null)
            {
                Destroy(_previewInstance);
                _previewInstance = null;
            }
        }

        #endregion

        #region Private Methods — Detection

        /// <summary>
        /// Unified hit resolution : tries wall first, then ground.
        /// Returns true if a valid surface + prefab pair was found.
        /// </summary>
        private bool TryResolveHit(out GameObject prefab, out GameObject preBuildPrefab,
                                   out Vector3 targetPos, out Vector3 exitDir, out bool isWall)
        {
            prefab = null;
            preBuildPrefab = null;
            targetPos = Vector3.zero;
            exitDir = Vector3.up;
            isWall = false;

            // --- Wall raycast ---
            Vector3 flatForward = GetFlatForward();
            Vector3 wallOrigin = _playerTransform.position + Vector3.up * _wallRaycastHeight;

            if (Physics.Raycast(wallOrigin, flatForward, out RaycastHit wallHit,
                                _wallRaycastDistance, _allDetectableLayers))
            {
                if (Mathf.Abs(wallHit.normal.y) <= 0.5f) // it's a wall
                {
                    int layer = wallHit.collider.gameObject.layer;
                    prefab = GetPrefabForLayer(layer);
                    preBuildPrefab = GetPreBuildPrefabForLayer(layer);

                    if (prefab != null && preBuildPrefab != null)
                    {
                        targetPos = wallHit.point + wallHit.normal * _targetHeightOffset;
                        exitDir = -wallHit.normal;
                        isWall = true;
                        return true;
                    }
                }
            }

            // --- Ground raycast ---
            Vector3 spawnCenter = _playerTransform.position + flatForward * _spawnDistance;
            Vector3 groundOrigin = spawnCenter + Vector3.up * _groundRaycastOriginHeight;

            if (Physics.Raycast(groundOrigin, Vector3.down, out RaycastHit groundHit,
                                _groundRaycastDistance, _allDetectableLayers))
            {
                int layer = groundHit.collider.gameObject.layer;
                prefab = GetPrefabForLayer(layer);
                preBuildPrefab = GetPreBuildPrefabForLayer(layer);

                if (prefab != null && preBuildPrefab != null)
                {
                    targetPos = groundHit.point + Vector3.up * _targetHeightOffset;
                    exitDir = Vector3.up;
                    isWall = false;
                    return true;
                }
            }

            return false;
        }

        private GameObject GetPrefabForLayer(int layer)
        {
            foreach (SurfaceEntry entry in _surfaceEntries)
                if ((entry.layer.value & (1 << layer)) != 0)
                    return entry.platformPrefab;
            return null;
        }

        private GameObject GetPreBuildPrefabForLayer(int layer)
        {
            foreach (SurfaceEntry entry in _surfaceEntries)
                if ((entry.layer.value & (1 << layer)) != 0)
                    return entry.preBuildPlatformPrefab;
            return null;
        }

        private Vector3 GetFlatForward()
        {
            return new Vector3(
                _playerTransform.forward.x,
                0f,
                _playerTransform.forward.z
            ).normalized;
        }

        #endregion

        #region Private Methods — Spawn & Orientation

        private void ApplyOrientation(GameObject obj, bool isWall)
        {
            obj.transform.rotation = isWall ? Quaternion.identity
                                             : Quaternion.LookRotation(GetFlatForward());
        }

        private void SpawnPlatform(GameObject prefab, Vector3 targetPosition,
                                   Vector3 exitDirection, bool isWall)
        {
            Vector3 startPosition = targetPosition - exitDirection * _startDepth;
            GameObject platform = Instantiate(prefab, startPosition, Quaternion.identity);
            ApplyOrientation(platform, isWall);
            StartCoroutine(RisePlatform(platform, startPosition, targetPosition));
        }

        private IEnumerator RisePlatform(GameObject platform, Vector3 from, Vector3 to)
        {
            float elapsed = 0f;

            while (elapsed < _riseDuration)
            {
                elapsed += Time.deltaTime;
                float curvedT = _riseCurve.Evaluate(Mathf.Clamp01(elapsed / _riseDuration));
                platform.transform.position = Vector3.Lerp(from, to, curvedT);
                yield return null;
            }

            platform.transform.position = to;
        }

        /// <summary>Recursively sets the layer on a GameObject and all its children.</summary>
        private static void SetLayerRecursive(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerRecursive(child.gameObject, layer);
        }

        #endregion
    }
}