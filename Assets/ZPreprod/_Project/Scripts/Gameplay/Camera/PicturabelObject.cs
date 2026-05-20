using UnityEngine;

namespace GlimmerOfHope.Gameplay.GCamera
{
    /// <summary>
    /// The object will have an effect if its visible on the camera at a right distance
    /// For the moment the effect need to be active as a child of this object
    /// </summary>
    public class PicturabelObject : MonoBehaviour
    {
        #region Private Properties

        private GameObject _child;

        #endregion

        #region Unity Lifecycle
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _child = transform.GetChild(0).gameObject;
            _child.SetActive(false);
        }

        #endregion

        #region Public Methods
        public void EffectVisible(bool isVisible)
        {
            _child.SetActive(isVisible);
        }
        #endregion
    }
}
