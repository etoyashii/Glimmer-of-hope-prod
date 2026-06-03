using System.Collections;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.Spells
{
    /// <summary>
    /// Sorts de vents avec deux modes : pousser les objets devant soi ou les attirer vers soi.
    /// </summary>
    public class WindSpell : ElementalSpell
    {
        #region Serialized Fields
        [Header("Références")]
        [Tooltip("Transform du joueur pour la direction et l'origine du vent")]
        [SerializeField] private Transform _playerTransform;

        [Tooltip("(Optionnel) Particle System pour l'effet visuel de la bourrasque")]
        [SerializeField] private ParticleSystem _windVFX;

        [Header("Forme de la bourrasque")]
        [Tooltip("Longueur du cône de vent devant le joueur")]
        [SerializeField] private float _gustLength = 8f;

        [Tooltip("Rayon du cône de vent à son extrémité")]
        [SerializeField] private float _gustRadius = 3f;

        [Tooltip("Layers pris en compte par l'OverlapCapsule (tout par défaut)")]
        [SerializeField] private LayerMask _detectionMask = ~0;

        [Header("Force de la bourrasque")]
        [Tooltip("Force totale appliquée aux objets")]
        [SerializeField] private float _pushForce = 18f;

        [Tooltip("Force vers le haut ajoutée à l'impulsion (donne un effet de soulèvement)")]
        [SerializeField] private float _upwardForce = 4f;

        [Tooltip("Falloff : les objets plus loin reçoivent moins de force (0 = aucun falloff, 1 = falloff total)")]
        [Range(0f, 1f)]
        [SerializeField] private float _distanceFalloff = 0.6f;

        [Header("Bourrasque progressive")]
        [Tooltip("Activer l'application progressive de la force sur plusieurs frames")]
        [SerializeField] private bool _useGustWave = true;

        [Tooltip("Durée totale de la bourrasque progressive")]
        [SerializeField] private float _gustWaveDuration = 0.3f;

        [Tooltip("Nombre d'impulsions pendant la bourrasque")]
        [SerializeField] private int _gustPulseCount = 3;

        [Header("Forme de la zone d'attraction")]
        [SerializeField] private float _pullLength = 8f;
        [SerializeField] private float _pullRadius = 3f;

        [Header("Force d'attraction")]
        [SerializeField] private float _pullForce = 18f;

        [Tooltip("Force vers le bas ajoutée à l'impulsion (plaque les objets au sol en arrivant)")]
        [SerializeField] private float _downwardForce = 2f;

        [Tooltip("Stopper les objets une fois arrivés près du joueur")]
        [SerializeField] private bool _dampOnArrival = true;

        [Tooltip("Distance du joueur à partir de laquelle on freine l'objet")]
        [SerializeField] private float _arrivalRadius = 1.5f;
        #endregion

        #region Constants
        // Tag Unity recherché sur les objets poussables
        private const string PUSHABLE_TAG = "WindPushable";
        #endregion

        #region Public Methods
        public override void CastSpell(bool spellmode)
        {
            Debug.Log("Casting a powerful wind spell!");
            if (_playerTransform == null)
            {
                Debug.LogWarning("[WindGustSpell] playerTransform non assigné !");
                return;
            }
            if (spellmode == true) WindPush();
            else WindPull();
        }
        #endregion

        #region Private Methods
        private void WindPush()
        {
            Debug.Log("Executing Wind Push!");
            // Direction horizontale du regard du joueur
            Vector3 forward = new Vector3(
                _playerTransform.forward.x,
                0f,
                _playerTransform.forward.z
            ).normalized;

            // Centre du cône : à mi-chemin devant le joueur
            Vector3 capsuleCenter = _playerTransform.position + forward * (_gustLength * 0.5f);

            // On cherche tous les colliders dans une capsule représentant le cône de vent
            Collider[] hits = Physics.OverlapCapsule(
                _playerTransform.position,
                capsuleCenter + forward * (_gustLength * 0.5f),
                _gustRadius,
                _detectionMask
            );

            // Jouer l'effet visuel si assigné
            if (_windVFX != null)
            {
                _windVFX.transform.position = _playerTransform.position;
                _windVFX.transform.rotation = Quaternion.LookRotation(forward);
                _windVFX.Play();
            }

            // Filtrer uniquement les WindPushable avec un Rigidbody
            foreach (Collider col in hits)
            {
                if (!col.CompareTag(PUSHABLE_TAG)) continue;

                Rigidbody rb = col.attachedRigidbody;
                if (rb == null || rb.isKinematic) continue;

                // Vérifier que l'objet est bien DEVANT le joueur (et pas derrière)
                Vector3 toObject = (col.transform.position - _playerTransform.position).normalized;
                if (Vector3.Dot(forward, toObject) < 0f) continue;

                float distance = Vector3.Distance(_playerTransform.position, col.transform.position);
                float falloff = Mathf.Lerp(1f, 1f - _distanceFalloff, distance / _gustLength);
                float finalForce = _pushForce * falloff;

                // Direction de poussée : forward + légère composante vers le haut
                Vector3 pushDir = (forward + Vector3.up * (_upwardForce / _pushForce)).normalized;

                if (_useGustWave)
                    StartCoroutine(ApplyGustWave(rb, pushDir, finalForce));
                else
                    rb.AddForce(pushDir * finalForce, ForceMode.Impulse);
            }
        }

        private IEnumerator ApplyGustWave(Rigidbody rb, Vector3 direction, float totalForce)
        {
            float forcePerPulse = totalForce / _gustPulseCount;
            float interval = _gustWaveDuration / _gustPulseCount;

            for (int i = 0; i < _gustPulseCount; i++)
            {
                if (rb == null) yield break;

                rb.AddForce(direction * forcePerPulse, ForceMode.Impulse);
                yield return new WaitForSeconds(interval);
            }
        }
        private void WindPull()
        {
            Debug.Log("Executing Wind Pull!");
            Vector3 forward = new Vector3(
            _playerTransform.forward.x,
            0f,
            _playerTransform.forward.z
            ).normalized;

            Vector3 capsuleEnd = _playerTransform.position + forward * _pullLength;

            Collider[] hits = Physics.OverlapCapsule(
                _playerTransform.position,
                capsuleEnd,
                _pullRadius,
                _detectionMask
            );

            if (_windVFX != null)
            {
                // VFX orienté vers le joueur (sens inverse du vent)
                _windVFX.transform.position = _playerTransform.position + forward * _pullLength;
                _windVFX.transform.rotation = Quaternion.LookRotation(-forward);
                _windVFX.Play();
            }

            foreach (Collider col in hits)
            {
                if (!col.CompareTag(PUSHABLE_TAG)) continue;

                Rigidbody rb = col.attachedRigidbody;
                if (rb == null || rb.isKinematic) continue;

                // L'objet doit être devant le joueur
                Vector3 toObject = (col.transform.position - _playerTransform.position).normalized;
                if (Vector3.Dot(forward, toObject) < 0f) continue;

                float distance = Vector3.Distance(_playerTransform.position, col.transform.position);

                // Falloff inversé : plus l'objet est LOIN, plus il reçoit de force
                // pour compenser la distance et arriver avec une vitesse cohérente
                float falloff = Mathf.Lerp(1f - _distanceFalloff, 1f, distance / _pullLength);
                float finalForce = _pullForce * falloff;

                // Direction vers le joueur + légère composante vers le bas
                Vector3 toPlayer = (_playerTransform.position - col.transform.position).normalized;
                Vector3 pullDir = (toPlayer - Vector3.up * (_downwardForce / _pullForce)).normalized;

                if (_useGustWave)
                    StartCoroutine(ApplyPullWave(rb, pullDir, finalForce, distance));
                else
                    rb.AddForce(pullDir * finalForce, ForceMode.Impulse);
            }
        }
        private IEnumerator ApplyPullWave(Rigidbody rb, Vector3 direction, float totalForce, float startDistance)
        {
            float forcePerPulse = totalForce / _gustPulseCount;
            float interval = _gustWaveDuration / _gustPulseCount;

            for (int i = 0; i < _gustPulseCount; i++)
            {
                if (rb == null) yield break;

                // Recalcule la direction à chaque impulsion (l'objet bouge)
                Vector3 toPlayer = (_playerTransform.position - rb.position).normalized;
                Vector3 currentDir = (toPlayer - Vector3.up * (_downwardForce / _pullForce)).normalized;

                rb.AddForce(currentDir * forcePerPulse, ForceMode.Impulse);
                yield return new WaitForSeconds(interval);
            }

            // Freinage à l'arrivée
            if (_dampOnArrival && rb != null)
                yield return StartCoroutine(DampOnArrival(rb));
        }

        private IEnumerator DampOnArrival(Rigidbody rb)
        {
            float timeout = 3f;
            float elapsed = 0f;

            while (rb != null && elapsed < timeout)
            {
                elapsed += Time.deltaTime;

                float dist = Vector3.Distance(rb.position, _playerTransform.position);
                if (dist <= _arrivalRadius)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    yield break;
                }

                yield return null;
            }
        }
        #endregion

        #region Editor
#if UNITY_EDITOR
        /// <summary>
        /// Visualise le cône de vent dans la Scene View (Editor uniquement).
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (_playerTransform == null) return;

            Vector3 forward = new Vector3(
                _playerTransform.forward.x,
                0f,
                _playerTransform.forward.z
            ).normalized;

            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.25f);

            // Représentation simplifiée du cône avec des sphères et une ligne
            int steps = 8;
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float radius = Mathf.Lerp(0.1f, _gustRadius, t);
                Vector3 pos = _playerTransform.position + forward * (_gustLength * t);
                Gizmos.DrawWireSphere(pos, radius);
            }

            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.8f);
            Gizmos.DrawRay(_playerTransform.position, forward * _gustLength);
        }
#endif
        #endregion
    }
}
