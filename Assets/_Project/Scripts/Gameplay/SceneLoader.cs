using UnityEngine;
using UnityEngine.SceneManagement;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Loads a scene by name, meant to be called from UI button OnClick
    /// events. The target scene must be added to Build Settings, otherwise
    /// LoadScene silently fails at runtime with a console error.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        #region Public Methods

        /// <summary>Loads the given scene, replacing the current one entirely.</summary>
        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>Reloads the currently active scene, useful for a Retry button.</summary>
        public void ReloadCurrentScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        #endregion
    }
}