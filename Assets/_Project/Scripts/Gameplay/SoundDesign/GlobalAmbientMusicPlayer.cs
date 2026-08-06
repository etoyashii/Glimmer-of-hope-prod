using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.Audio
{
    public class GlobalAmbientMusicPlayer : MonoBehaviour
    {
        #region Public Fields
        [Header("Playlist")]
        [Tooltip("Morceaux joués en boucle, indépendamment de la zone/du Terrain Layer.")]
        public List<AudioClip> playlist = new List<AudioClip>();
        [Tooltip("Ordre aléatoire à chaque cycle de playlist plutôt que l'ordre de la liste.")]
        public bool shuffle = true;
        [Tooltip("Recommence la playlist en boucle une fois tous les morceaux joués.")]
        public bool loopPlaylist = true;

        [Header("Volume / Crossfade")]
        [Range(0f, 1f)] public float volume = 0.5f;
        [Tooltip("Durée du fondu enchaîné entre deux morceaux de la playlist, en secondes.")]
        public float crossfadeDuration = 3f;
        #endregion

        #region Private Properties
        private AudioSource sourceA;
        private AudioSource sourceB;
        private AudioSource activeSource;
        private AudioSource inactiveSource;

        private List<int> playOrder = new List<int>();
        private int playOrderIndex = -1;
        #endregion

        #region Unity LifeCycle
        private void Awake()
        {
            sourceA = gameObject.AddComponent<AudioSource>();
            sourceB = gameObject.AddComponent<AudioSource>();

            foreach (AudioSource s in new[] { sourceA, sourceB })
            {
                s.loop = false; // le passage au morceau suivant est géré manuellement (playlist)
                s.playOnAwake = false;
                s.volume = 0f;
                s.spatialBlend = 0f; // musique 2D, indépendante de la position
            }

            activeSource = sourceA;
            inactiveSource = sourceB;
        }

        private void Start()
        {
            if (playlist == null || playlist.Count == 0) { return; }
            BuildPlayOrder();
            StartCoroutine(PlaybackLoop());
        }
        #endregion

        #region Private Methods
        private void BuildPlayOrder()
        {
            playOrder.Clear();
            for (int i = 0; i < playlist.Count; i++) { playOrder.Add(i); }

            if (shuffle)
            {
                for (int i = playOrder.Count - 1; i > 0; i--)
                {
                    int j = Random.Range(0, i + 1);
                    (playOrder[i], playOrder[j]) = (playOrder[j], playOrder[i]);
                }
            }
            playOrderIndex = -1;
        }

        private IEnumerator PlaybackLoop()
        {
            while (true)
            {
                playOrderIndex++;
                if (playOrderIndex >= playOrder.Count)
                {
                    if (!loopPlaylist) { yield break; }
                    BuildPlayOrder();
                    playOrderIndex = 0;
                }

                AudioClip clip = playlist[playOrder[playOrderIndex]];
                if (clip == null) { continue; }

                yield return StartCoroutine(CrossfadeTo(clip));

                // On attend la fin du morceau, moins la durée du crossfade suivant
                // pour que la transition démarre avant la fin exacte du clip.
                float waitTime = Mathf.Max(0f, clip.length - crossfadeDuration);
                yield return new WaitForSeconds(waitTime);
            }
        }

        private IEnumerator CrossfadeTo(AudioClip newClip)
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
                inactiveSource.volume = Mathf.Lerp(0f, volume, ratio);
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