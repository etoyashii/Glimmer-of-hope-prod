using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Audio
{
    [System.Serializable]
    public class ZoneMusicEntry
    {
        [Tooltip("Glisse directement l'asset Terrain Layer concerné (celui peint sur le Terrain).")]
        public TerrainLayer layer;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    /// <summary>
    /// Joue une musique d'ambiance par zone, avec un crossfade fluide entre les
    /// morceaux. Le mapping se fait directement par référence à l'asset
    /// TerrainLayer — pas de texte à faire correspondre entre deux listes, pas
    /// d'ordre à respecter.
    ///
    /// Mise en place :
    /// 1. GameObject vide (ex: "AudioManager") dans la scène, persistant.
    /// 2. Attacher ce script.
    /// 3. Remplir la liste "Zone Musics" : une entrée par Terrain Layer, en
    ///    glissant l'asset TerrainLayer directement (Project > ton dossier de
    ///    Terrain Layers) et le clip audio correspondant.
    /// </summary>
    public class AmbientMusicManager : MonoBehaviour
    {
        public static AmbientMusicManager Instance;

        [Header("Sons par Terrain Layer")]
        public List<ZoneMusicEntry> zoneMusics = new List<ZoneMusicEntry>();

        [Header("Crossfade")]
        [Tooltip("Durée du fondu enchaîné entre deux morceaux, en secondes.")]
        public float crossfadeDuration = 2f;

        private AudioSource sourceA;
        private AudioSource sourceB;
        private AudioSource activeSource;
        private AudioSource inactiveSource;
        private TerrainLayer currentLayer;
        private Coroutine crossfadeRoutine;

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
                s.spatialBlend = 0f; // son 2D, indépendant de la position (musique d'ambiance)
            }

            activeSource = sourceA;
            inactiveSource = sourceB;
        }

        /// <summary>Change la zone musicale actuelle en fonction du Terrain Layer dominant
        /// (ne fait rien si c'est déjà le layer actif).</summary>
        public void SetZone(TerrainLayer layer)
        {
            if (layer == currentLayer) { return; }

            ZoneMusicEntry entry = FindEntry(layer);
            if (entry == null || entry.clip == null)
            {
                string layerName = layer != null ? layer.name : "null";
                Debug.LogWarning($"AmbientMusicManager : aucun clip configuré pour le Terrain Layer '{layerName}'.");
                return;
            }

            currentLayer = layer;

            if (crossfadeRoutine != null) { StopCoroutine(crossfadeRoutine); }
            crossfadeRoutine = StartCoroutine(CrossfadeTo(entry.clip, entry.volume));
        }

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
    }
}