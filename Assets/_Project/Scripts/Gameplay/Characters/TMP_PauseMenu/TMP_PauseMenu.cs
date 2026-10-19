using UnityEngine;

namespace GlimmerOfHope.UI
{
    public class TMP_PauseMenu : MonoBehaviour
    {
        public bool IsPaused {get; private set;} = false;

        public void Pause()
        {
            gameObject.SetActive(true);
            IsPaused = true;
        }

        public void Resume()
        {
            gameObject.SetActive(false);
            IsPaused = false;
        }
        
        public void Toggle()
        {
            if (IsPaused)
                Resume();
            else
                Pause();
        }
    }
}
