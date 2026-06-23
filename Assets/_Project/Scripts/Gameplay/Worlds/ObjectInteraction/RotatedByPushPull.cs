using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class RotatedByPushPull : MonoBehaviour
    {
        #region SerializeField

        [SerializeField] private Quaternion _targetRotation;

        #endregion

        #region PublicMethod

        public void Rotate()
        {
            transform.rotation = _targetRotation;
            //transform.Rotate(_targetRotation);
        }

        #endregion
    }
}
