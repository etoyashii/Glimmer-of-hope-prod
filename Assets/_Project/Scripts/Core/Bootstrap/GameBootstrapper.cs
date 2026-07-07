using UnityEngine;
using UnityEngine.SceneManagement;
using GlimmerOfHope.Core.Services;
using GlimmerOfHope.Core.Audio;
using GlimmerOfHope.Core.Localization;
using GlimmerOfHope.Core.Save;

namespace GlimmerOfHope.Core.Bootstrap
{
    public class GameBootstrapper : MonoBehaviour
    {
        [SerializeField] private bool _useSecureSave;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            InitializeServices();
        }

        private void InitializeServices()
        {
            // Audio
            ServiceLocator.Register(new AudioManager());

            // Localization
            ServiceLocator.Register(new LocalizationManager());

            // Save System
            if (_useSecureSave)
            {
                ServiceLocator.Register(new SecureSaveManager());
            }
            else
            {
                ServiceLocator.Register(new SaveManager());
            }

            // Apply saved preferences
            ApplySavedPreferences();

            Debug.Log("[GameBootstrapper] Services initialized.");

            SceneManager.LoadScene("MainMenu");
        }

        private void ApplySavedPreferences()
        {
            if (ServiceLocator.TryGet<SaveManager>(out var saveManager))
            {
                var prefs = saveManager.CurrentSave.preferences;

                if (ServiceLocator.TryGet<LocalizationManager>(out var localization))
                {
                    localization.SetLanguage(prefs.language);
                }
            }
        }

        private void OnApplicationQuit()
        {
            ServiceLocator.Clear();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                // Auto-save on pause (mobile)
                if (ServiceLocator.TryGet<SaveManager>(out var saveManager))
                {
                    saveManager.SaveAll();
                }
            }
        }
    }
}
