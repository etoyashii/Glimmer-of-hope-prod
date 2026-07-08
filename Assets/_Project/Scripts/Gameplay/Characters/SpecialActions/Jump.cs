using GlimmerOfHope.Gameplay.Character.SpecialActions;
using System;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// To make the player Jump
    /// </summary>
    #region Dependancies

    [RequireComponent(typeof(Rigidbody))]

    #endregion
    public class Jump : MonoBehaviour
    {
        #region SerializedField

        [Header("Refs")]
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private Movement _playerMovement;

        [Tooltip("VFX of Jump and Impulse")]
        [SerializeField] private ParticleSystem _jumpVFX;
        [SerializeField] private ParticleSystem _impulseVFX;

        [Tooltip("Maximum jump strength Value of a Jump / Minimum jump strength value of an Impulse")]
        [SerializeField] private float _jumpImpulseLimit;

        #endregion

        [SerializeField]
        private Animator _animator;

        public event Action OnStartJumping;

        
        #region Public Methods

        // Give the player a vertical impulse of the jumpForce value
        public void PerformJump(float jumpForce)
        {
            Debug.Log($"[Jump] PerformJump called with jumpForce: {jumpForce}");
            if (!_playerMovement.IsGrounded()) return;
            if (jumpForce < _jumpImpulseLimit)
            {
                if (_jumpVFX != null)
                {
                    _jumpVFX.transform.position = transform.position;
                    _jumpVFX.transform.rotation = Quaternion.LookRotation(transform.up);
                    _jumpVFX.Play();
                }
            }
            else
            {
                if (_impulseVFX != null)
                {
                    _impulseVFX.transform.position = transform.position;
                    _impulseVFX.transform.rotation = Quaternion.LookRotation(transform.up);
                    _impulseVFX.Play();
                }
            }
            _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            if (_animator != null)
                _animator.SetTrigger("Jump");
            OnStartJumping?.Invoke();
        }

        #endregion
    }
}