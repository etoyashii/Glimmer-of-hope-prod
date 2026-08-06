using System.Collections.Generic;
using UnityEngine;
using GlimmerOfHope.Gameplay.Character.SpecialActions;

namespace GlimmerOfHope.Audio
{
    [System.Serializable]
    public class FootstepEntry
    {
        [Tooltip("Glisse directement l'asset Terrain Layer concerné.")]
        public TerrainLayer layer;

        [Header("Marche")]
        [Tooltip("Plusieurs variantes pour éviter la répétition — une est choisie au hasard à chaque pas.")]
        public AudioClip[] walkClips;

        [Header("Course")]
        [Tooltip("Laisser vide pour retomber sur les clips de marche si tu n'as pas encore de variante course pour cette surface.")]
        public AudioClip[] runClips;

        [Range(0f, 1f)] public float volume = 1f;
    }

    /// <summary>
    /// Joue un bruit de pas aléatoire selon le Terrain Layer sous le joueur, avec des
    /// clips différents (et une cadence différente) selon que le joueur marche ou
    /// court. Cadencé sur la distance parcourue, donc automatiquement plus rapide en
    /// courant qu'en marchant, sans avoir besoin d'Animation Events.
    ///
    /// S'appuie sur Movement.IsGrounded() pour savoir si le joueur est au sol, et sur
    /// le Rigidbody pour la vitesse horizontale (qui détermine aussi marche vs course).
    ///
    /// Mise en place :
    /// 1. Attacher sur le même GameObject que le composant Movement (le Player).
    /// 2. Remplir "Footstep Sounds" : une entrée par Terrain Layer, avec l'asset
    ///    TerrainLayer glissé + les clips de marche ET de course (2-4 variantes
    ///    recommandées chacun).
    /// 3. Remplir "Default Walk/Run Clips" pour les sols qui ne sont pas des Terrain.
    /// 4. Ajuster "Run Speed Threshold" pour que le passage marche→course se déclenche
    ///    au bon moment par rapport à la vitesse réelle de ton personnage.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class FootstepAudioSystem : MonoBehaviour
    {
        [Header("Références")]
        [Tooltip("Laisser vide pour auto-détecter sur le même GameObject.")]
        [SerializeField] private Movement movement;
        [Tooltip("Laisser vide pour auto-détecter sur le même GameObject.")]
        [SerializeField] private Rigidbody rb;

        [Header("Sons par Terrain Layer")]
        public List<FootstepEntry> footstepSounds = new List<FootstepEntry>();

        [Header("Sons par défaut (sol non-Terrain)")]
        public AudioClip[] defaultWalkClips;
        public AudioClip[] defaultRunClips;
        [Range(0f, 1f)] public float defaultVolume = 1f;

        [Header("Cadence")]
        [Tooltip("Distance parcourue (en mètres) entre deux pas en marchant.")]
        public float walkStepDistance = 2f;
        [Tooltip("Distance parcourue (en mètres) entre deux pas en courant (généralement plus grand : foulée plus large).")]
        public float runStepDistance = 3.2f;
        [Tooltip("Vitesse horizontale minimale (m/s) pour déclencher des pas.")]
        public float minSpeedToStep = 0.3f;
        [Tooltip("Vitesse horizontale (m/s) à partir de laquelle le joueur est considéré comme en train de courir plutôt que marcher.")]
        public float runSpeedThreshold = 5f;

        [Header("Variation")]
        [Tooltip("Variation aléatoire de pitch pour éviter un son trop robotique.")]
        [Range(0f, 0.3f)] public float pitchVariation = 0.1f;

        private AudioSource audioSource;
        private float distanceAccumulator;
        private Vector3 lastPosition;
        private bool isRunning;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;

            if (movement == null) { movement = GetComponent<Movement>(); }
            if (rb == null) { rb = GetComponent<Rigidbody>(); }

            lastPosition = transform.position;
        }

        private void Update()
        {
            if (movement == null || rb == null) { return; }

            if (!movement.IsGrounded())
            {
                ResetStepTracking();
                return;
            }

            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float speed = horizontalVelocity.magnitude;

            if (speed < minSpeedToStep)
            {
                ResetStepTracking();
                return;
            }

            isRunning = speed >= runSpeedThreshold;
            float currentStepDistance = isRunning ? runStepDistance : walkStepDistance;

            float distanceThisFrame = Vector3.Distance(transform.position, lastPosition);
            lastPosition = transform.position;
            distanceAccumulator += distanceThisFrame;

            if (distanceAccumulator >= currentStepDistance)
            {
                distanceAccumulator = 0f;
                PlayFootstep();
            }
        }

        private void ResetStepTracking()
        {
            distanceAccumulator = 0f;
            lastPosition = transform.position;
        }

        private void PlayFootstep()
        {
            // On utilise le point d'impact du raycast au sol (plus précis que la
            // position du joueur, notamment sur terrain en pente).
            Vector3 groundPoint = movement.lastHit.collider != null ? movement.lastHit.point : transform.position;
            TerrainLayer layer = TerrainLayerUtility.GetDominantTerrainLayer(groundPoint);

            FootstepEntry entry = FindEntry(layer);
            AudioClip[] clips = GetClipsFor(entry);
            float volume = entry != null ? entry.volume : defaultVolume;

            if (clips == null || clips.Length == 0) { return; }

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            audioSource.PlayOneShot(clip, volume);
        }

        /// <summary>Sélectionne le bon tableau de clips (marche/course) pour l'entrée
        /// donnée, avec repli sur les clips de marche si aucune variante course n'a
        /// été fournie pour cette surface.</summary>
        private AudioClip[] GetClipsFor(FootstepEntry entry)
        {
            if (entry != null)
            {
                if (isRunning && entry.runClips != null && entry.runClips.Length > 0) { return entry.runClips; }
                return entry.walkClips;
            }

            if (isRunning && defaultRunClips != null && defaultRunClips.Length > 0) { return defaultRunClips; }
            return defaultWalkClips;
        }

        private FootstepEntry FindEntry(TerrainLayer layer)
        {
            if (layer == null) { return null; }
            foreach (FootstepEntry entry in footstepSounds)
            {
                if (entry.layer == layer) { return entry; }
            }
            return null;
        }
    }
}