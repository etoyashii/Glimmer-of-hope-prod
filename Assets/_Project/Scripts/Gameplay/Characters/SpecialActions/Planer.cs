using GlimmerOfHope.Gameplay.Character.SpecialActions;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// a planer limiting falling speed,
    /// trigered by the jump button if pressed while not being grounded.
    /// </summary>
    public class Planer : MonoBehaviour
    {
        #region Serialized Field
        [Header("Refs")]
        [Tooltip("Controller of the Player")]
        [SerializeField] private CharacterController _controller;
        [SerializeField] private Movement _playerMovement;

        [Tooltip("maximum falling speed while planer is active")]
        [SerializeField] private float _planningVerticalVelocity = -1;
        #endregion

        #region Private fields
        private bool _isPlanningActive = false;
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            //Make sure that the planner is always desabled while the player is on the ground
            if (_controller.isGrounded)
            {
                if (_isPlanningActive) _isPlanningActive = false;
            }
            
            //Lerps the vertical velocity of the player to the maximum falling speed when planer is active
            if (_isPlanningActive)
            {
                if (_playerMovement.verticalVelocity <= _planningVerticalVelocity)
                {
                    _playerMovement.verticalVelocity = Mathf.Lerp(_playerMovement.verticalVelocity, _planningVerticalVelocity, 0.5f);
                }
            }
        }
        #endregion

        #region Public Methods
        // Method called on the button that triggers and cancel the planner 
        public void PerformPlaner()
        {
            if (_controller.isGrounded) return;
            if (!_isPlanningActive)
            {
                _isPlanningActive = true;
            }
            else _isPlanningActive = false;
        }
        #endregion
    }
}
