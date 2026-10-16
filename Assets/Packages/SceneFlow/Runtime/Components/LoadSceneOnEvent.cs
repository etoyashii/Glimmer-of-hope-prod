using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kebab.SceneFlow.Components
{
    /// <summary>
    /// Load a scene on an event.
    /// </summary>
    [AddComponentMenu("SceneFlow/Load Scene On Event")]
    public class LoadSceneOnEvent : MonoBehaviour
    {
        [Tooltip("The scene to load.")]
        [SerializeField] [Scene] private string sceneToLoad;
        [Tooltip("Show loading screen if true.")]
        [SerializeField] private bool showLoadingScreen = true;

        public void LoadScene()
        {
            SceneFlowManager.Load(sceneToLoad, showLoadingScreen);
        }
    }

}