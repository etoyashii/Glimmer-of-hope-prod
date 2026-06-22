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
            [Tooltip("Layer de la surface détectée.")]
            public LayerMask layer;

            [Tooltip("Prefab à spawner sur cette surface.")]
            public GameObject platformPrefab;
        }

        #endregion

        #region Serialized Fields

        [Header("Références")]
        [Tooltip("Transform du joueur (ou de la caméra) pour calculer la direction.")]
        [SerializeField] private Transform _playerTransform;

        [Header("Surfaces & Prefabs")]
        [Tooltip("Associe chaque layer de surface à un prefab de plateforme.")]
        [SerializeField] private SurfaceEntry[] _surfaceEntries;

        [Header("Paramètres de détection")]
        [Tooltip("Distance devant le joueur où le raycast part.")]
        [SerializeField] private float _spawnDistance = 3f;

        [Tooltip("Hauteur depuis laquelle le raycast horizontal part (pour les murs).")]
        [SerializeField] private float _wallRaycastHeight = 1f;

        [Tooltip("Longueur maximale du raycast horizontal vers le mur.")]
        [SerializeField] private float _wallRaycastDistance = 5f;

        [Tooltip("Hauteur depuis laquelle le raycast sol part (au-dessus du sol).")]
        [SerializeField] private float _groundRaycastOriginHeight = 5f;

        [Tooltip("Longueur maximale du raycast vers le bas.")]
        [SerializeField] private float _groundRaycastDistance = 20f;

        [Tooltip("Layer mask global : tous les layers détectables.")]
        [SerializeField] private LayerMask _allDetectableLayers;

        [Header("Paramètres d'animation")]
        [Tooltip("Profondeur / distance depuis laquelle la plateforme commence à sortir.")]
        [SerializeField] private float _startDepth = 2f;

        [Tooltip("Durée de l'animation de montée en secondes.")]
        [SerializeField] private float _riseDuration = 0.6f;

        [Tooltip("Hauteur cible au-dessus du point d'impact (0 = au ras de la surface).")]
        [SerializeField] private float _targetHeightOffset = 0f;

        [Tooltip("Courbe d'animation de la montée.")]
        [SerializeField] private AnimationCurve _riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        #endregion

        #region Public Methods

        public override void PerformSkill()
        {
            // 1 — Essaie d'abord un raycast horizontal pour détecter un mur
            if (TryCastOnWall()) return;

            // 2 — Sinon, raycast vers le bas pour détecter le sol
            TryCastOnGround();
        }

        #endregion

        #region Private Methods — Detection

        /// <summary>Raycast horizontal depuis la hauteur du joueur pour détecter un mur devant.</summary>
        private bool TryCastOnWall()
        {
            Debug.Log("Executing GeneratePlatform");

            Vector3 flatForward = GetFlatForward();
            Vector3 rayOrigin = _playerTransform.position + Vector3.up * _wallRaycastHeight;

            if (!Physics.Raycast(rayOrigin, flatForward, out RaycastHit hit,
                                 _wallRaycastDistance, _allDetectableLayers))
                return false;

            // La normale du hit détermine si c'est bien un mur (normale ~horizontale)
            if (Mathf.Abs(hit.normal.y) > 0.5f)
                return false; // c'est une surface horizontale, pas un mur

            GameObject prefab = GetPrefabForLayer(hit.collider.gameObject.layer);
            if (prefab == null)
            {
                Debug.LogWarning($"[GeneratePlatform] Aucun prefab associé au layer '{LayerMask.LayerToName(hit.collider.gameObject.layer)}'.");
                return false;
            }

            // Position : collée au mur, à la hauteur du joueur
            Vector3 targetPosition = hit.point + hit.normal * _targetHeightOffset;

            // Sortie depuis le mur (direction opposée à la normale du mur)
            SpawnPlatform(prefab, targetPosition, -hit.normal, isWall: true);
            return true;
        }

        /// <summary>Raycast vers le bas pour détecter une surface horizontale (sol, eau…).</summary>
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

            // Sortie depuis le bas (direction vers le haut)
            SpawnPlatform(prefab, targetPosition, Vector3.up, isWall: false);
            return true;
        }

        /// <summary>Retourne le prefab associé au layer donné, ou null si aucun n'est trouvé.</summary>
        private GameObject GetPrefabForLayer(int layer)
        {
            foreach (SurfaceEntry entry in _surfaceEntries)
            {
                // LayerMask contient le layer si le bit correspondant est à 1
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
            // Départ : enfoncé dans la surface dans la direction opposée à la sortie
            Vector3 startPosition = targetPosition - exitDirection * _startDepth;

            GameObject platform = Instantiate(prefab, startPosition, Quaternion.identity);

            if (isWall)
            {
                // Plateforme murale : toujours horizontale, face orientée vers le joueur
                platform.transform.rotation = Quaternion.identity;
            }
            else
            {
                // Plateforme au sol : orientée dans la direction du joueur
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