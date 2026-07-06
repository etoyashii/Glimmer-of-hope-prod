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
            ServiceLocator.Register(new SaveManager());
        }

        public void Save() {
            if (ServiceLocator.TryGet<SaveManager>(out var sm))
            {
                sm.NewGame();
            } 
        }
    }
}
