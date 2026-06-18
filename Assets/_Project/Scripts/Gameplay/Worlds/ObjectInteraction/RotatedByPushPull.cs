using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class RotatedByPushPull : MonoBehaviour
    {
        #region SerializeField

        [SerializeField] private Vector3 _targetRotation;

        #endregion

        #region PublicMethod

        public void Rotate()
        {
            transform.Rotate(_targetRotation);
        }

        #endregion
    }
}
