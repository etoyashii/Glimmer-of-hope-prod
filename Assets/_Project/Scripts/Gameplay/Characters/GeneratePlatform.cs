using System;
using System.Collections;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
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

            [Tooltip("Previzualisation prefab.")]
            public GameObject preBuildPlatformPrefab;
        }

        #endregion

        #region Serialized Fields

        [Header("Refs")]
        [Tooltip("Caster's transform from wich are made the raycasts")]
        [SerializeField] private Transform _playerTransform;

        [Header("Surfaces & Prefabs")]
        [Tooltip("Associate every layer with a different prefab")]
        [SerializeField] private SurfaceEntry[] _surfaceEntries;

        [Header("Detection Parameters")]
        [Tooltip("Raycast distance in front of the caster")]
        [SerializeField] private float _spawnDistance = 3f;

        [Tooltip("For walls => height from wich the raycats is casted")]
        [SerializeField] private float _wallRaycastHeight = 1f;

        [Tooltip("For walls => maw distance of raycast")]
        [SerializeField] private float _wallRaycastDistance = 5f;

        [Tooltip("For the ground => height from wich the vertical raycast is casted")]
        [SerializeField] private float _groundRaycastOriginHeight = 5f;

        [Tooltip("For the ground => Max distance of vertical raycast")]
        [SerializeField] private float _groundRaycastDistance = 20f;

        [Tooltip("Every detectable layers")]
        [SerializeField] private LayerMask _allDetectableLayers;

        [Header("Animations parameters")]
        [Tooltip("Depth from wich the plateform comes from")]
        [SerializeField] private float _startDepth = 2f;

        [Tooltip("Animation time (in seconds)")]
        [SerializeField] private float _riseDuration = 0.6f;

        [Tooltip("Final Height of the platform (0 = at the surface of raycasted object) ")]
        [SerializeField] private float _targetHeightOffset = 0f;

        [Tooltip("Animation curve")]
        [SerializeField] private AnimationCurve _riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        #endregion

        #region Public Methods

        public override void PerformSkill()
        {
            // try raycast for walls
            if (TryCastOnWall()) return;

            // if no wall found, try raycast for ground
            TryCastOnGround();
        }

        #endregion

        #region Private Methods — Detection

        /// <summary>horizontal raycast to detect walls in front of caster.</summary>
        private bool TryCastOnWall()
        {
            Debug.Log("Executing GeneratePlatform");

            Vector3 flatForward = GetFlatForward();
            Vector3 rayOrigin = _playerTransform.position + Vector3.up * _wallRaycastHeight;

            if (!Physics.Raycast(rayOrigin, flatForward, out RaycastHit hit,
                                 _wallRaycastDistance, _allDetectableLayers))
                return false;

            // normal of hit objects tell us if its a wall or not
            if (Mathf.Abs(hit.normal.y) > 0.5f)
                return false; 

            GameObject prefab = GetPrefabForLayer(hit.collider.gameObject.layer);
            if (prefab == null)
            {
                Debug.LogWarning($"[GeneratePlatform] Aucun prefab associé au layer '{LayerMask.LayerToName(hit.collider.gameObject.layer)}'.");
                return false;
            }

            
            Vector3 targetPosition = hit.point + hit.normal * _targetHeightOffset;

            // Spawn platform on the wall
            SpawnPlatform(prefab, targetPosition, -hit.normal, isWall: true);
            return true;
        }

        /// <summary>raycast towards the ground in front of caster to detect grount or water where platform could spawn.</summary>
        private bool TryCastOnGround()
        {
            Vector3 flatForward = GetFlatForward();
            Vector3 spawnCenter = _playerTransform.position + flatForward * _spawnDistance;
            Vector3 rayOrigin = spawnCenter + Vector3.up * _groundRaycastOriginHeight;

            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                                 _groundRaycastDistance, _allDetectableLayers))
            {
                Debug.Log("[GeneratePlatform] Aucune surface détectée devant le joueur.");
                return false;
            }

            GameObject prefab = GetPrefabForLayer(hit.collider.gameObject.layer);
            if (prefab == null)
            {
                Debug.LogWarning($"[GeneratePlatform] Aucun prefab associé au layer '{LayerMask.LayerToName(hit.collider.gameObject.layer)}'.");
                return false;
            }

            Vector3 targetPosition = hit.point + Vector3.up * _targetHeightOffset;

            // spawn from the ground
            SpawnPlatform(prefab, targetPosition, Vector3.up, isWall: false);
            return true;
        }

        /// <summary>return prefab associated to raycasted object's layer</summary>
        private GameObject GetPrefabForLayer(int layer)
        {
            foreach (SurfaceEntry entry in _surfaceEntries)
            {
                // LayerMask to compare the layers and find the right ones
                if ((entry.layer.value & (1 << layer)) != 0)
                    return entry.platformPrefab;
            }
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

        #region Private Methods — Spawn

        private void SpawnPlatform(GameObject prefab, Vector3 targetPosition, Vector3 exitDirection, bool isWall)
        {
            // depart in the surface
            Vector3 startPosition = targetPosition - exitDirection * _startDepth;

            GameObject platform = Instantiate(prefab, startPosition, Quaternion.identity);

            if (isWall)
            {
                // wall => spawn horizontally
                platform.transform.rotation = Quaternion.identity;
            }
            else
            {
                // ground => spawn vertically
                platform.transform.forward = GetFlatForward();
            }

            StartCoroutine(RisePlatform(platform, startPosition, targetPosition));
        }

        private IEnumerator RisePlatform(GameObject platform, Vector3 from, Vector3 to)
        {
            float elapsed = 0f;

            while (elapsed < _riseDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _riseDuration);
                float curvedT = _riseCurve.Evaluate(t);

                platform.transform.position = Vector3.Lerp(from, to, curvedT);
                yield return null;
            }

            platform.transform.position = to;
        }

        #endregion
    }
}