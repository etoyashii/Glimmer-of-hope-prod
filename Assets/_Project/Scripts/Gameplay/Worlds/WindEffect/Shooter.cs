using UnityEngine;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Use to create sphere for cloth reaction to the wind
    /// </summary>
    public class Shooter : MonoBehaviour
    {
        #region Serialized Fields

        [SerializeField] private Object _sphere;

        #endregion

        #region Unity Lifecycle

        void Update()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                SpawnSphere();
            }
        }

        #endregion

        #region Public Methods

        public void SpawnSphere()
        {
            Object obj = Instantiate(_sphere, transform.position, Quaternion.identity);
        }

        #endregion
    }
}
