using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Controls AI movement by calculating direction and updating position towards a target in the XZ plane.
    /// </summary>
    public class AIMovement : MonoBehaviour
    {
        #region SerializeField

        [SerializeField] private float _speed;

        #endregion

        #region PrivateFields

        private Vector3 _direction;

        #endregion

        #region PublicMethods

        public void FollowTarget(Vector3 targetPosXZ)
        {
            _direction = targetPosXZ - transform.position;

            Vector3 velocity = _direction.normalized * _speed * Time.deltaTime;
            transform.position += velocity;
        }

        #endregion

        #region Helpers

        public Vector3 GetDirection()
        {
            return _direction; 
        }

        #endregion
    }
}
