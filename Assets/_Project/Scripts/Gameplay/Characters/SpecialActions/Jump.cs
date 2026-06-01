using GlimmerOfHope.Gameplay.Character.SpecialActions;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// To make the player Jump
    /// </summary>
    #region Dependancies

    [RequireComponent(typeof(CharacterController))]

    #endregion
    public class Jump : MonoBehaviour
    {
        #region SerializedField
        [SerializeField] private CharacterController _controller;
        [SerializeField] private Movement _playerMovement;
        [SerializeField] private float _jumpForce;
        #endregion

        #region Public Methods
        public void PerformJump()
        {
            if (!_controller.isGrounded) return;
            _playerMovement.verticalVelocity += _jumpForce;
        }
        #endregion 
    }
}
