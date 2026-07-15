using UnityEngine;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay.Emotion
{
    /// <summary>
    /// Use to show or hide the particle system of emotions
    /// </summary>
    public class EmotionParticle : MonoBehaviour
    {
        #region Public Properties

        public float maxDist = 15.0f;

        #endregion

        #region Private Properties

        private GameObject _child;
        private bool _running = false;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _child = transform.GetChild(0).gameObject;
            _child.SetActive(false);
        }

        private void Update()
        {
            //if (Keyboard.current.tabKey.wasPressedThisFrame)
            //{
            //    _running = !_running;
            //    _child.SetActive(_running);
            //}
        }
        private void FixedUpdate()
        {
            if (_running)
            {
                Vector3 camPos = Camera.main.transform.position;
                Vector3 pos = transform.position;

                float distance = Vector3.Distance(camPos, pos);

                if (distance > maxDist)
                    _child.SetActive(false);
                else
                    _child.SetActive(true);
            }            
        }

        #endregion
    }
}
