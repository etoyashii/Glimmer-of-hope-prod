using GlimmerOfHope.Core.Services;
using UnityEngine;

namespace GlimmerOfHope.Core.Audio
{
    public class AudioManager : IService
    {
        private float _masterVolume = 1f;
        private float _musicVolume = 0.8f;
        private float _sfxVolume = 1f;

        public float MasterVolume => _masterVolume;
        public float MusicVolume => _musicVolume;
        public float SFXVolume => _sfxVolume;

        public void Initialize()
        {
#if FMOD_AVAILABLE
            Debug.Log("[AudioManager] Initialized with FMOD backend.");
#else
            Debug.Log("[AudioManager] Initialized (FMOD not yet configured).");
#endif
        }

        public void Shutdown()
        {
            // FMOD cleanup handled automatically
        }

        public void PlaySFX(string eventPath)
        {
#if FMOD_AVAILABLE
            FMODUnity.RuntimeManager.PlayOneShot(eventPath);
#else
            Debug.LogWarning($"[AudioManager] FMOD not available. Cannot play: {eventPath}");
#endif
        }

        public void PlayMusic(string eventPath)
        {
#if FMOD_AVAILABLE
            // TODO: Store music instance for control
            FMODUnity.RuntimeManager.PlayOneShot(eventPath);
#else
            Debug.LogWarning($"[AudioManager] FMOD not available. Cannot play music: {eventPath}");
#endif
        }

        public void StopMusic()
        {
#if FMOD_AVAILABLE
            // TODO: Stop active music instance
#endif
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
        }

        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            ApplyVolumeSettings();
        }

        private void ApplyVolumeSettings()
        {
#if FMOD_AVAILABLE
            // Update FMOD bus volumes
            // var masterBus = FMODUnity.RuntimeManager.GetBus("bus:/");
            // masterBus.setVolume(_masterVolume);
#endif
        }
    }
}
