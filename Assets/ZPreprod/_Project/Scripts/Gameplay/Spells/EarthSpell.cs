using System.Collections;
using UnityEngine;
namespace GlimmerOfHope.Gameplay.Spells
{
    /// <summary>
    /// Sort de terre avec 1 mode : faire apparaître une plateforme de terre devant le joueur.
    /// </summary>
    public class EarthSpell : ElementalSpell
    {
        #region Serialized Fields
        [Header("Références")]
        [Tooltip("Le prefab de la plateforme de terre")]
        [SerializeField] private GameObject _platformPrefab;

        [Tooltip("Transform du joueur (ou de la caméra) pour calculer la direction")]
        [SerializeField] private Transform _playerTransform;

        [Header("Paramètres de détection")]
        [Tooltip("Distance devant le joueur où la plateforme doit apparaître")]
        [SerializeField] private float _spawnDistance = 3f;

        [Tooltip("Hauteur depuis laquelle le raycast part (au-dessus du sol)")]
        [SerializeField] private float _raycastOriginHeight = 5f;

        [Tooltip("Longueur maximale du raycast vers le bas")]
        [SerializeField] private float _raycastMaxDistance = 20f;

        [Header("Paramètres d'animation")]
        [Tooltip("Profondeur sous le sol d'où la plateforme commence à monter")]
        [SerializeField] private float _startDepth = 2f;

        [Tooltip("Durée de l'animation de montée en secondes")]
        [SerializeField] private float _riseDuration = 0.6f;

        [Tooltip("Hauteur cible au-dessus du point d'impact (0 = au ras du sol)")]
        [SerializeField] private float _targetHeightOffset = 0f;

        [Tooltip("Courbe d'animation de la montée")]
        [SerializeField] private AnimationCurve _riseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        #endregion

        #region Private Fields
        // Layer mask calculé automatiquement depuis le nom "EarthPlatform"
        private int earthPlatformLayer;
        private int earthPlatformLayerMask;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            earthPlatformLayer = LayerMask.NameToLayer("EarthPlatform");

            if (earthPlatformLayer == -1)
            {
                Debug.LogError("[EarthPlatformSpell] Le layer 'EarthPlatform' n'existe pas. " +
                               "Crée-le dans Edit > Project Settings > Tags and Layers.");
            }

            earthPlatformLayerMask = 1 << earthPlatformLayer;
        }
        #endregion

        #region Public Methods
        public override void CastSpell(bool spellmode)
        {
            Debug.Log("Casting a powerful Earth spell!");
            if (_platformPrefab == null)
            {
                Debug.LogWarning("[EarthPlatformSpell] platformPrefab non assigné !");
                return;
            }

            // Point devant le joueur (ignore l'axe Y pour rester horizontal)
            Vector3 flatForward = new Vector3(
                _playerTransform.forward.x,
                0f,
                _playerTransform.forward.z
                ).normalized;

            Vector3 spawnCenter = _playerTransform.position + flatForward * _spawnDistance;

            // Origine du raycast bien au-dessus du sol
            Vector3 rayOrigin = spawnCenter + Vector3.up * _raycastOriginHeight;

            // On ne détecte QUE les colliders sur le layer EarthPlatform
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                                _raycastMaxDistance, earthPlatformLayerMask))
            {
                Vector3 targetPosition = hit.point + Vector3.up * _targetHeightOffset;
                SpawnPlatform(targetPosition);
            }
            else
            {
                Debug.Log("[EarthPlatformSpell] Aucune zone EarthPlatform détectée devant le joueur.");
            }
        }
        #endregion

        #region Private Methods
        private void SpawnPlatform(Vector3 targetPosition)
        {
            // Position de départ sous le sol
            Vector3 startPosition = targetPosition - Vector3.up * _startDepth;

            GameObject platform = Instantiate(_platformPrefab, startPosition, Quaternion.identity);

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

            // S'assure que la plateforme est exactement à la position cible
            platform.transform.position = to;
        }
        #endregion
    }
}
