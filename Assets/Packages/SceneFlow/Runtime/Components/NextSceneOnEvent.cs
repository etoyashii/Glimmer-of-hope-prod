using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kebab.SceneFlow.Components
{
    /// <summary>
    /// Load the next scene on an event.
    /// </summary>
    [AddComponentMenu("SceneFlow/Next Scene On Event")]
    public class NextSceneOnEvent : MonoBehaviour
    {
        [SerializeField] private bool showLoadingScreen = true;

        public void LoadNextScene()
        {
            SceneFlowManager.LoadNextScene(showLoadingScreen);
        }
    }
}