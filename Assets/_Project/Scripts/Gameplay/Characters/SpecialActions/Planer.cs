using GlimmerOfHope.Gameplay.Character.SpecialActions;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// A planer limiting falling speed,
    /// triggered by the jump button if pressed while not being grounded.
    /// </summary>
    public class Planer : MonoBehaviour
    {
        #region Serialized Field

        [Header("Refs")]
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private Movement _playerMovement;

        [Tooltip("Maximum falling speed while planer is active")]
        [SerializeField] private float _planningVerticalVelocity = -1f;

        [Header("Ground Check")]
        [SerializeField] private float _groundCheckDistance = 0.2f;
        [SerializeField] private LayerMask _groundLayer;

        #endregion

        #region Private Fields

        private bool _isPlanningActive = false;

        #endregion

        #region Private Methods

        private bool IsGrounded()
        {
            return Physics.Raycast(transform.position, Vector3.down, _groundCheckDistance, _groundLayer);
        }

        #endregion

        #region Unity Lifecycle

        private void FixedUpdate()
        {
            // Disable planer automatically when grounded
            if (_playerMovement.IsGrounded())
            {
                if (_isPlanningActive) _isPlanningActive = false;
                return;
            }

            // Clamp falling speed when planer is active
            if (_isPlanningActive && _rb.linearVelocity.y < _planningVerticalVelocity)
            {
                Vector3 vel = _rb.linearVelocity;
                vel.y = Mathf.Lerp(vel.y, _planningVerticalVelocity, 0.5f);
                _rb.linearVelocity = vel;
            }
            
            Debug.Log("Planner : " +  _isPlanningActive);
        }

        #endregion

        #region Public Methods

        // Method called on the button that triggers and cancels the planer
        public void PerformPlaner()
        {
            if (_playerMovement.IsGrounded()) return;

            _isPlanningActive = !_isPlanningActive;
        }

        #endregion
    }
}