using UnityEngine;

namespace GlimmerOfHope.Gameplay.GCamera
{
    /// <summary>
    /// Make an object look to an other
    /// Possibility to add a permanante rotate to it
    /// </summary>
    public class LookAt : MonoBehaviour
    {
        #region Public Properties

        public Transform transformToLook;
        public Vector3 rotate = Vector3.zero;

        #endregion

        #region Unity LifeCycle

        private void FixedUpdate()
        {
            transform.LookAt(transformToLook);
            transform.RotateAround(transform.position, transform.right, rotate.x);
        }

        #endregion
    }
}
