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
        [Header("Refs")]
        [Tooltip("Controller of the Player")]
        [SerializeField] private CharacterController _controller;
        [SerializeField] private Movement _playerMovement;

        [Tooltip("VFX of Jump and Impulse")]
        [SerializeField] private  ParticleSystem _jumpVFX;
        [SerializeField] private  ParticleSystem _impulseVFX;

        [Tooltip("Maximum jump strength Value of a Jump / Minimum jump strength value of an Impulse")]
        [SerializeField] private float _jumpImpulseLimit;
        #endregion

        #region Public Methods
        public void PerformJump(float jumpForce)
        {

            if (!_controller.isGrounded) return;
            if (jumpForce < _jumpImpulseLimit)
            {
                if (_jumpVFX != null)
                {
                    _jumpVFX.transform.position = gameObject.transform.position;
                    _jumpVFX.transform.rotation = Quaternion.LookRotation(gameObject.transform.up);
                    _jumpVFX.Play();
                }
            }
            else
            {
                if (_impulseVFX != null)
                {
                    _impulseVFX.transform.position = gameObject.transform.position;
                    _impulseVFX.transform.rotation = Quaternion.LookRotation(gameObject.transform.up);
                    _impulseVFX.Play();
                }
            }
            _playerMovement.verticalVelocity += jumpForce;
        }
        #endregion 
    }
}
