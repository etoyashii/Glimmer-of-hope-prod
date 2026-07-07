using GlimmerOfHope.Core.Save;
using GlimmerOfHope.Core.Services;
using UnityEngine;

namespace GlimmerOfHope.Core
{
    public class TestSave : MonoBehaviour
    {
        private void Awake()
        {
            // Enregistre le SaveManager comme service
            #if DEVELOPMENT_BUILD || UNITY_EDITOR
                        ServiceLocator.Register(new SaveManager());      // JSON clair
            #else
                ServiceLocator.Register(new SecureSaveManager()); // Chiffré
            #endif
        }

        public void Save() {
#if DEVELOPMENT_BUILD || UNITY_EDITOR

            if (ServiceLocator.TryGet<SaveManager>(out var sm))
            {
                sm.NewGame();
            }
#else
            if (ServiceLocator.TryGet<SecureSaveManager>(out var sm))
            {
                sm.NewGame();
            }
#endif

        }
    }
}
