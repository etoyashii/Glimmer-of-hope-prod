using UnityEngine;
using GlimmerOfHope.Core.Services;
using GlimmerOfHope.Core.Audio;
using GlimmerOfHope.Core.Localization;
using GlimmerOfHope.Core.Save;

namespace GlimmerOfHope.Core.Bootstrap
{
    /// <summary>
    /// Dev-only bootstrapper. Permet de Play depuis n'importe quelle scène
    /// sans passer par _Bootstrap. S'auto-détruit si services déjà initialisés.
    /// </summary>
    public class DevBootstrapper : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField] private bool _useSecureSave;

        private void Awake()
        {
            if (ServiceLocator.IsRegistered<AudioManager>())
            {
                Debug.Log("[DevBootstrapper] Services already initialized, skipping.");
                Destroy(gameObject);
                return;
            }

            Debug.Log("[DevBootstrapper] DEV MODE — Initializing services...");
            DontDestroyOnLoad(gameObject);

            ServiceLocator.Register(new AudioManager());
            ServiceLocator.Register(new LocalizationManager());

            if (_useSecureSave)
                ServiceLocator.Register(new SecureSaveManager());
            else
                ServiceLocator.Register(new SaveManager());

            Debug.Log("[DevBootstrapper] Services initialized (dev mode).");
        }

        private void OnApplicationQuit()
        {
            ServiceLocator.Clear();
        }
#endif
    }
}
