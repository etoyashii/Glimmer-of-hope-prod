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
        [SerializeField] private Jump jump;
        [SerializeField] private Movement movement;

        [SerializeField] private float startTensionDistance = 3f; //commence a tirer sur le player a partir de cette distance
        [SerializeField] private float maxDistance = 10f;//tension graduel avec la distance jusqu'a bloquer le joueur a la distance max
        [SerializeField] private float targetedYOffset = 5f;
        [SerializeField] private float acceleration = 1f;

        [SerializeField] private bool isAttached = false;

        #endregion

        #region Private Properties

        private Spring playerSpring;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            jump.OnStartJumping += PlayerJump;            

            playerSpring = _player.GetComponent<Spring>();
        }

        private void Update()
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame) //to jump because button doesnt work
            {
                jump.PerformJump(4);
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
            isAttached = true;

            playerSpring.isActive = true;
            playerSpring.SetMinDistance(startTensionDistance);
            playerSpring.SetMaxDistance(maxDistance);
            playerSpring.SetAnchor(transform);
        }

        #endregion

        #region Public Methods

        public void PlayerJump()//function called with an invoke in jump
        {
            if (!isAttached) return;

            isAttached = false;

            //player try to jump will being attached so throw him in the correct dir
            playerSpring.isActive = false;
            
            Vector3 offsetPoint = transform.position + new Vector3(0, targetedYOffset, 0);
            Vector3 dir = offsetPoint - _player.transform.position;

            Vector3 force = dir.normalized * playerSpring.currentPullForce * 200 * acceleration;

            _player.GetComponent<Rigidbody>().AddForce(force, ForceMode.Acceleration);
        }

        #endregion
    }
}
