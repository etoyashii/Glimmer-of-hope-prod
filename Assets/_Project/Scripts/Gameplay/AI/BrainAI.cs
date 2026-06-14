using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Script that decide what AI will do
    /// </summary>
    public class BrainAI : MonoBehaviour
    {
        #region SerializeField

        [SerializeField] private DetectTarget _detectTarget;
        [SerializeField] private AIMovement _movement;

        #endregion

        #region UnityLifecycle

        void Update()
        {
            if (_movement == null || _movement == null) return;

            _detectTarget.UpdateTargetPos();

            if (_detectTarget.IsTargetInSight() && _detectTarget.IsTargetInRange())
            {
                _detectTarget.LookTarget();
                _movement.FollowTarget(_detectTarget.GetTargetPosXZ());
            }
        }

        #endregion
    }
}
