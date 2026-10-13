using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kebab.SceneFlow.Components
{
    /// <summary>
    /// Load a scene after a certain amount of time.
    /// </summary>
    [AddComponentMenu("SceneFlow/Load Scene After Time")]
    public class LoadSceneAfterTime : MonoBehaviour
    {
        [Tooltip("The scene to load after the timer is up.")]
        [SerializeField] [Scene] private string sceneToLoad;
        [Space]
        [Tooltip("The time in seconds before loading the scene.")]
        [SerializeField] [Min(0f)] private float waitingTime = 1f;
        [Space]
        [Tooltip("Start the timer on start if true.")]
        [SerializeField] private bool startTimerOnStart = true;
        [Tooltip("Show loading screen if true.")]
        [SerializeField] private bool showLoadingScreen = true;

        private bool isTimerStarted = false;

        private void Start()
        {
            if (startTimerOnStart)
                StartTimer();
        }

        /// <summary>
        /// Start the timer.
        /// </summary>
        public void StartTimer()
        {
            if (isTimerStarted)

                return;
            isTimerStarted = true;
            Invoke(nameof(LoadScene), waitingTime);
        }

        private void LoadScene()
        {
            SceneFlowManager.Load(sceneToLoad, showLoadingScreen);
        }
    }
}
