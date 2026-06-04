using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// The script that manage all behavior based on the light reception
    /// </summary>
    public class LightReaction : MonoBehaviour
    {
        #region Enums

        public enum EntityType
        {
            None,
            ElevateFlower
        }

        #endregion

        #region SerializeFields

        [SerializeField] private Transform _startPoint;
        [SerializeField] private Transform _endPoint;

        [SerializeField] private EntityType _entityType;

        #endregion

        #region PublicMethods

        public void ReactionToLight()
        {
            switch (_entityType)
            {
                case EntityType.None:
                    break;
                case EntityType.ElevateFlower:
                    MoveUpByLight();
                    break;
            }
        }

        #endregion

        #region PrivateMethods

        private void MoveUpByLight()
        {
            transform.position = _endPoint.position;
        }

        #endregion
    }
}
