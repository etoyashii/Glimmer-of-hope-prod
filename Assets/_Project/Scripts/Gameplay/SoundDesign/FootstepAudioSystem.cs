using System.Collections.Generic;
using UnityEngine;
using GlimmerOfHope.Gameplay.Character.SpecialActions;

namespace GlimmerOfHope.Gameplay.Audio
{
    [System.Serializable]
    public class FootstepEntry
    {
        #region Public Field 
        [Tooltip("Drag the relevant Terrain Layer asset directly.")]
        public TerrainLayer layer;

        [Header("Walk")]
        [Tooltip("Several variants to avoid repetition — one is picked at random on each step.")]
        public AudioClip[] walkClips;

        [Header("Run")]
        [Tooltip("Leave empty to fall back to the walk clips if you don't have a run variant for this surface yet.")]
        public AudioClip[] runClips;

        [Range(0f, 1f)] public float volume = 1f;
        #endregion
    }


    [RequireComponent(typeof(AudioSource))]
    public class FootstepAudioSystem : MonoBehaviour
    {

        #region Public Fields
        [Header("Sounds per Terrain Layer")]
        public List<FootstepEntry> footstepSounds = new List<FootstepEntry>();

        [Header("Default sounds (non-Terrain ground)")]
        public AudioClip[] defaultWalkClips;
        public AudioClip[] defaultRunClips;
        [Range(0f, 1f)] public float defaultVolume = 1f;

        [Header("Cadence")]
        [Tooltip("Distance travelled (in meters) between two steps while walking.")]
        public float walkStepDistance = 2f;
        [Tooltip("Distance travelled (in meters) between two steps while running (usually larger: wider stride).")]
        public float runStepDistance = 3.2f;
        [Tooltip("Minimum horizontal speed (m/s) required to trigger footsteps.")]
        public float minSpeedToStep = 0.3f;
        [Tooltip("Horizontal speed (m/s) above which the player is considered to be running instead of walking.")]
        public float runSpeedThreshold = 5f;

        [Header("Variation")]
        [Tooltip("Random pitch variation to avoid a too-robotic sound.")]
        [Range(0f, 0.3f)] public float pitchVariation = 0.1f;
        #endregion

        #region Private Properties
        [Header("References")]
        [Tooltip("Leave empty to auto-detect on the same GameObject.")]
        [SerializeField] private Movement movement;
        [Tooltip("Leave empty to auto-detect on the same GameObject.")]
        [SerializeField] private Rigidbody rb;

        private AudioSource audioSource;
        private float distanceAccumulator;
        private Vector3 lastPosition;
        private bool isRunning;
        #endregion

        #region Unity LifeCycle
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
        #endregion

        #region Private Methods
        private void ResetStepTracking()
        {
            distanceAccumulator = 0f;
            lastPosition = transform.position;
        }

        private void PlayFootstep()
        {
            // Use the ground raycast hit point (more accurate than the player's
            // position, especially on sloped terrain).
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

        /// <summary>Selects the right clip array (walk/run) for the given entry,
        /// falling back to the walk clips if no run variant has been provided for
        /// this surface.</summary>
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
        #endregion
    }
}