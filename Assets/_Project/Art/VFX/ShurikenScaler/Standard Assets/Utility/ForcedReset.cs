using UnityEngine;
using UnityEngine.SceneManagement;
using UnityStandardAssets.CrossPlatformInput;

public class ForcedReset : MonoBehaviour
{
    private void Update()
    {
        // If the reset button is pressed, reload the first scene
        if (CrossPlatformInputManager.GetButtonDown("ResetObject"))
        {
            SceneManager.LoadScene(SceneManager.GetSceneAt(0).name);
        }
    }
}