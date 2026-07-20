using DG.Tweening;
using GlimmerOfHope.Gameplay.Character.SpecialActions;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Script to manage the catapult rod
    /// </summary>
    public class CatapultRod : MonoBehaviour
    {
        #region SerializeField

        [SerializeField] private GameObject _player;
        [SerializeField] private Jump _jump;
        [SerializeField] private Movement _movement;

        [SerializeField] private float _startTensionDistance = 3f; //commence a tirer sur le player a partir de cette distance
        [SerializeField] private float _maxDistance = 10f;//tension graduel avec la distance jusqu'a bloquer le joueur a la distance max
        [SerializeField] private float _targetedYOffset = 5f;
        [SerializeField] private float _acceleration = 1f;

        [SerializeField] private bool _isAttached = false;

        #endregion

        #region Private Properties

        private Spring _playerSpring;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _jump.OnStartJumping += PlayerJump;            

            _playerSpring = _player.GetComponent<Spring>();
        }

        private void Update()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame) //to jump because button doesnt work
            {
                _jump.PerformJump(10);
            }

            if (Keyboard.current.eKey.wasPressedThisFrame) //to start the link, to do differently with the spell
            {
                //Start link
                AttachToPlayer();
            }
        }

        #endregion

        #region Private Methods

        private void AttachToPlayer()
        {
            _isAttached = true;

            _playerSpring.isActive = true;
            _playerSpring.SetMinDistance(_startTensionDistance);
            _playerSpring.SetMaxDistance(_maxDistance);
            _playerSpring.SetAnchor(transform);
        }

        #endregion

        #region Public Methods

        public void PlayerJump()//function called with an invoke in jump
        {
            if (!_isAttached) return;

            _isAttached = false;

            //player try to jump will being attached so throw him in the correct dir
            _playerSpring.isActive = false;
            
            Vector3 offsetPoint = transform.position + new Vector3(0, _targetedYOffset, 0);
            Vector3 dir = offsetPoint - _player.transform.position;

            Vector3 force = dir.normalized * _playerSpring.currentPullForce * 200 * _acceleration;

            _player.GetComponent<Rigidbody>().AddForce(force, ForceMode.Acceleration);
        }

        #endregion
    }
}
