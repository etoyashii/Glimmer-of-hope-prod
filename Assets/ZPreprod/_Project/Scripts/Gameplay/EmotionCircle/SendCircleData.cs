using UnityEngine;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay.Emotion
{
    /// <summary>
    /// Class use to send the global data to the emotions shaders graph
    /// need to be present only once in the scene
    /// </summary>
    public class SendCircleData : MonoBehaviour
    {
        #region Public Properties

        public bool isRunning = false;
        public Transform playerTransform;

        #endregion

        #region Private Properties

        private float _radius = 0.0f;
        private float _maxradius = 15.0f;

        #endregion

        #region Unity Lifecycle

        void Update()
        {
            //if (Keyboard.current.tabKey.wasPressedThisFrame)
            //{
            //    isRunning = !isRunning;
            //}

            if (isRunning)
            {
                Shader.SetGlobalVector("_CenterCircle", playerTransform.position);
                Shader.SetGlobalFloat("_CircleActive", 1f);
                Shader.SetGlobalFloat("_Radius", _radius);
                Shader.SetGlobalFloat("_Radius2", _radius / 3.0f);

                if (_radius < _maxradius)
                {
                    _radius += Time.deltaTime * 20.0f;
                    if (_radius > _maxradius)
                        _radius = _maxradius;
                }
            }
            else
            {
                Shader.SetGlobalFloat("_CircleActive", 0f);
                _radius = 0.0f;
            }

        }

        #endregion

        #region Public Methods

        public void SwitchViewMode()
        {
            isRunning = !isRunning;
        }

        #endregion
    }
}
