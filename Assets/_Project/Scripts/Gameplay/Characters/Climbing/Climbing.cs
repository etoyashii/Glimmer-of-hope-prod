using GlimmerOfHope.Gameplay.Character.SpecialActions;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// Use to allow the player to climb object with the choosen layers
    /// </summary>
    public class Climbing : MonoBehaviour
    {
        #region Public Properties

        [Header("References")]
        public Transform orientation;
        public Movement movement;
        public LayerMask whatIsClimbable;
        public GameObject freeCam;

        [Header("Climbing")]
        public float climbSpeed;
        public float detectFloorTime = 0.5f;
        public bool climbing;

        [Header("Detection")]
        public float detectionLength;
        public float sphereCastRadius;
        public float maxWallLookAngle;
        public RaycastHit frontWallHit;//public for movement

        #endregion

        #region Private Properties

        private bool _wallFront;
        private CinemachineRotationComposer _rotationComposer;
        private CinemachineOrbitalFollow _orbitalFollow;
        private float _detectFloorProgress = 0f;
        private float _wallLookAngle;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            _rotationComposer = freeCam.GetComponent<CinemachineRotationComposer>();
            _rotationComposer.enabled = false;
            _orbitalFollow = freeCam.GetComponent<CinemachineOrbitalFollow>();
        }

        private void FixedUpdate()
        {
            WallCheck();
            StateMachine();

            if (climbing) ClimbingMovement();
        }

        #endregion

        #region Private Methods

        private void StateMachine()
        {
            //State 1 - Climbing

            if (_wallFront && _wallLookAngle < maxWallLookAngle)
            {
                if (!climbing) StartClimbing();

                if (_detectFloorProgress > 0f) _detectFloorProgress -= Time.deltaTime;
                if (movement.IsGrounded() && _detectFloorProgress <= 0f) StopClimbing();
            }

            //State 3 - None
            else
            {
                if (climbing) StopClimbing();
            }
        }

        private void WallCheck()
        {
            _wallFront = Physics.SphereCast(transform.position, sphereCastRadius, orientation.forward, out frontWallHit, detectionLength, whatIsClimbable);

            _wallLookAngle = Vector3.Angle(orientation.forward, -frontWallHit.normal);
        }

        private void ClimbingMovement()
        {

        }

        private void StopClimbing()
        {
            climbing = false;

            //update cam
            _rotationComposer.enabled = false;
            _orbitalFollow.HorizontalAxis.Recentering.Enabled = false;
        }

        #endregion

        #region PublicMethod

        public void StartClimbing()
        {
            climbing = true;

            _detectFloorProgress = detectFloorTime;

            //rotate player to have the correct rotate with the wall
            transform.forward = -frontWallHit.normal;

            //update cam
            _rotationComposer.enabled = true;
            _orbitalFollow.HorizontalAxis.Recentering.Enabled = true;
            _orbitalFollow.HorizontalAxis.Recentering.Wait = 0f;
            _orbitalFollow.HorizontalAxis.Recentering.Time = 2f;
        }

        #endregion
    }
}
