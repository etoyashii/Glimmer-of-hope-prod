using UnityEngine;
using GlimmerOfHope.Core.Events;
using GlimmerOfHope.Core.Services;
using GlimmerOfHope.Core.Localization;

namespace GlimmerOfHope.Gameplay.Dialogue
{
    [DefaultExecutionOrder(100)]
    public class DialogueBootstrapper : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private bool _initializeOnAwake = true;

        [Header("Action Event Channels")]
        [SerializeField] private StringEventChannel _onDialogueEvent;
        [SerializeField] private StringEventChannel _onCharacterShow;
        [SerializeField] private StringEventChannel _onCharacterHide;

        #endregion

        #region Private Fields

        private static bool _isInitialized;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_initializeOnAwake)
            {
                Initialize();
                ConfigureActionHandlers();
            }
        }

        private void ConfigureActionHandlers()
        {
            if (ServiceLocator.TryGet<DialogueRunner>(out var runner))
            {
                runner.SetActionEventChannels(_onDialogueEvent, _onCharacterShow, _onCharacterHide);
            }
        }

        #endregion

        #region Public Methods

        public static void Initialize()
        {
            if (_isInitialized)
            {
                Debug.Log("[DialogueBootstrapper] Already initialized, skipping.");
                return;
            }

            Debug.Log("[DialogueBootstrapper] Initializing dialogue services...");

            if (!ServiceLocator.IsRegistered<LocalizationManager>())
            {
                var lm = new LocalizationManager();
                lm.Initialize();
                ServiceLocator.Register(lm);
                Debug.Log("[DialogueBootstrapper] LocalizationManager registered.");
            }

            if (!ServiceLocator.IsRegistered<FlagManager>())
            {
                var fm = new FlagManager();
                ServiceLocator.Register(fm);
                Debug.Log("[DialogueBootstrapper] FlagManager registered.");
            }

            if (!ServiceLocator.IsRegistered<DialogueRunner>())
            {
                var dr = new DialogueRunner();
                ServiceLocator.Register(dr);
                Debug.Log("[DialogueBootstrapper] DialogueRunner registered.");
            }

            _isInitialized = true;
            Debug.Log("[DialogueBootstrapper] Dialogue services initialized successfully!");
        }

        #endregion
    }
}
