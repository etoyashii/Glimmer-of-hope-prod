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
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitBeforeFirstScene()
        {
            InitializeServices();

            Application.quitting -= OnQuit;
            Application.quitting += OnQuit;
            Application.focusChanged -= OnFocusChanged;
            Application.focusChanged += OnFocusChanged;
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            InitializeServices();
            SceneManager.LoadScene("MainMenu");
        }

        private static void InitializeServices()
        {
            if (ServiceLocator.IsRegistered<ISaveService>())
                return;

            ServiceLocator.Register(new AudioManager());
            ServiceLocator.Register(new LocalizationManager());
            ServiceLocator.Register<ISaveService>(new SaveManager());

            ApplySavedPreferences();

            Debug.Log("[GameBootstrapper] Services initialized.");
        }

        private static void ApplySavedPreferences()
        {
            if (ServiceLocator.TryGet<ISaveService>(out var saveManager))
            {
                var prefs = saveManager.CurrentSave.preferences;

                if (ServiceLocator.TryGet<LocalizationManager>(out var localization))
                {
                    localization.SetLanguage(prefs.language);
                }
            }
        }

        private static void OnQuit()
        {
            ServiceLocator.Clear();
        }

        private static void OnFocusChanged(bool hasFocus)
        {
            if (hasFocus)
                return;

            if (ServiceLocator.TryGet<ISaveService>(out var saveManager))
            {
                saveManager.Save();
            }
        }
    }
}
