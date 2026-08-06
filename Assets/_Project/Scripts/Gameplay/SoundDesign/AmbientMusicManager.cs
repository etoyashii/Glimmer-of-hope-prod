using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.Audio
{
    [System.Serializable]
    public class ZoneMusicEntry
    {
        #region Public Fields
        [Tooltip("Drag the relevant Terrain Layer asset directly (the one painted on the Terrain).")]
        public TerrainLayer layer;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        #endregion
    }

    public class AmbientMusicManager : MonoBehaviour
    {
        #region Public Fields
        public static AmbientMusicManager Instance;

        [Header("Sounds per Terrain Layer")]
        public List<ZoneMusicEntry> zoneMusics = new List<ZoneMusicEntry>();

        [Header("Crossfade")]
        [Tooltip("Crossfade duration between two tracks, in seconds.")]
        public float crossfadeDuration = 2f;
        #endregion

        #region Private Properties
        private AudioSource sourceA;
        private AudioSource sourceB;
        private AudioSource activeSource;
        private AudioSource inactiveSource;
        private TerrainLayer currentLayer;
        private Coroutine crossfadeRoutine;
        #endregion

        #region Unity LifeCycle
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            sourceA = gameObject.AddComponent<AudioSource>();
            sourceB = gameObject.AddComponent<AudioSource>();

            foreach (AudioSource s in new[] { sourceA, sourceB })
            {
                s.loop = true;
                s.playOnAwake = false;
                s.volume = 0f;
                s.spatialBlend = 0f; // 2D sound, independent of position (ambient music)
            }

            activeSource = sourceA;
            inactiveSource = sourceB;
        }
        #endregion 

        /// <summary>Changes the current musical zone based on the dominant Terrain Layer
        /// (does nothing if it's already the active layer).</summary>
        #region Public Methods

        public void SetZone(TerrainLayer layer)
        {
            if (layer == currentLayer) { return; }

            ZoneMusicEntry entry = FindEntry(layer);
            if (entry == null || entry.clip == null)
            {
                string layerName = layer != null ? layer.name : "null";
                Debug.LogWarning($"AmbientMusicManager: no clip configured for Terrain Layer '{layerName}'.");
                return;
            }

            currentLayer = layer;

            if (crossfadeRoutine != null) { StopCoroutine(crossfadeRoutine); }
            crossfadeRoutine = StartCoroutine(CrossfadeTo(entry.clip, entry.volume));
        }

        #endregion

        #region Private Methods
        private ZoneMusicEntry FindEntry(TerrainLayer layer)
        {
            foreach (ZoneMusicEntry entry in zoneMusics)
            {
                if (entry.layer == layer) { return entry; }
            }
            return null;
        }

        private IEnumerator CrossfadeTo(AudioClip newClip, float targetVolume)
        {
            inactiveSource.clip = newClip;
            inactiveSource.volume = 0f;
            inactiveSource.Play();

            float t = 0f;
            float startActiveVolume = activeSource.volume;

            while (t < crossfadeDuration)
            {
                t += Time.deltaTime;
                float ratio = Mathf.Clamp01(t / crossfadeDuration);
                inactiveSource.volume = Mathf.Lerp(0f, targetVolume, ratio);
                activeSource.volume = Mathf.Lerp(startActiveVolume, 0f, ratio);
                yield return null;
            }

            activeSource.Stop();
            activeSource.volume = 0f;

            AudioSource temp = activeSource;
            activeSource = inactiveSource;
            inactiveSource = temp;
        }
        #endregion
    }
}