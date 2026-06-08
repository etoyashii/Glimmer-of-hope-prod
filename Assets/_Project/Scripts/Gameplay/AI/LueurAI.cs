using GlimmerOfHope.Gameplay.Character.SpecialActions;
using System.Collections;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    /// <summary>
    /// The Lueur Behavior (Game AI) with naive State Machine
    /// </summary>
    public class LueurAI : MonoBehaviour
    {
        #region Enums

        //The Lueur behavior isn't completely defined right now, so the states may changed in the future
        public enum LueurState
        {
            Idle,
            FollowPlayer,
            GoFrontOfPlayer,
            MoveTo,
            Speak
        }

        public enum IdleBehaviorState
        {
            TurnAround,
            Random,
            Stay
        }

        private LueurState _currentState = LueurState.Idle;

        #endregion

        #region SerializeFields

        [Header("References")]
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private Transform _lueurStartPoint;
        [SerializeField] private Movement _playerMovement;

        [Header("General Movement")]
        [Range(1.0f, 100.0f)]
        [SerializeField] private float _maxSpeed = 50.0f;

        [Header("Follow Movement")]
        [Range(10.0f, 100.0f)]
        [SerializeField] private float _followRadius = 50.0f;
        [Range(0.0f, 5.0f)]
        [SerializeField] private float _smoothTime = 0.3f;

        [Header("Satellite Movement")]
        [Range(2.0f, 25.0f)]
        [SerializeField] private float _radius = 2.0f;
        [Range(1,4)]
        [SerializeField] private int _maxLaps = 1;
        [Range(2.0f, 100.0f)]
        [SerializeField] private float _lapSpeed = 50.0f;

        [Header("Behavior")]
        [SerializeField] private IdleBehaviorState _idleBehaviorState;

        #endregion

        #region PrivateFields

        private Vector3 _velocity = Vector3.zero;
        private Vector3 _satellitePoint;

        private const float _fullCircleAngle = 2.0f * Mathf.PI;


        private Coroutine _satelliteCoroutine;

        #endregion

        #region UnityLifecycle

        private void Awake()
        {
            _playerMovement.OnPlayerStartMoving += OnPlayerMoving;
            _playerMovement.OnPlayerStopMoving += OnPlayerStopMoving;
        }

        //LateUpdate because Lueur is reacting to the player so we need its update after the player one
        private void LateUpdate()
        {
            UpdateState();
        }

        private void OnDisable()
        {
            _playerMovement.OnPlayerStartMoving -= OnPlayerMoving;
            _playerMovement.OnPlayerStopMoving -= OnPlayerStopMoving;
        }

        #endregion

        #region PrivateMethods

        private void UpdateState()
        {
                switch (_currentState)
                {
                    case (LueurState.Idle):
                        SatelliteAroundPlayer();
                        break;

                    case (LueurState.FollowPlayer):
                        FollowTarget(_playerTransform.position);
                        break;

                    case (LueurState.GoFrontOfPlayer):
                        if (IsTargetTooFar(_lueurStartPoint.position, 1.0f))
                        {
                            FollowTarget(_lueurStartPoint.position);
                        }
                        else
                            ChangeState(LueurState.Idle);
                        break;

                    case (LueurState.MoveTo):
                        break;

                    case (LueurState.Speak):
                        break;
                }
        }

        private void ChangeState(LueurState newState)
        {
            if (newState == _currentState) return;
            
            if (_satelliteCoroutine != null)
            {
                StopCoroutine(_satelliteCoroutine);
                _satelliteCoroutine = null;
            }

            _currentState = newState;
        }

        private void FollowTarget(Vector3 newTarget)
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                newTarget,
                ref _velocity,
                _smoothTime,
                _maxSpeed
            );
        }

        private void SatelliteAroundPlayer()
        {
            if (_satelliteCoroutine != null) return;

            switch (_idleBehaviorState)
            {
                case(IdleBehaviorState.Stay):
                    ChangeState(LueurState.GoFrontOfPlayer);
                    break;
                case(IdleBehaviorState.TurnAround):
                    _satelliteCoroutine = StartCoroutine(LockSatellitePerfectCircleBehavior());
                    break;
                case (IdleBehaviorState.Random):
                    _satelliteCoroutine = StartCoroutine(LockRandomDestinationBehavior(1.0f));
                    break;
            }
        }

        private void OnPlayerMoving()
        {
            ChangeState(LueurState.FollowPlayer);
        }
        
        private void OnPlayerStopMoving()
        {
            ChangeState(LueurState.GoFrontOfPlayer);
        }

        #endregion

        #region Coroutines

        IEnumerator LockRandomDestinationBehavior(float delay)
        {
            float elapsedTime = 0f;

            _satellitePoint = transform.position + Random.insideUnitSphere * _radius;

            while (elapsedTime < delay)
            {
                elapsedTime += Time.deltaTime;
                FollowTarget(_satellitePoint);
                yield return null;
            }

            _satelliteCoroutine = null;
        }

        IEnumerator LockSatellitePerfectCircleBehavior()
        {
            float elapsedTime = 0.0f;
            float angle = 0.0f;
            int completedLaps = 0;

            while (completedLaps < _maxLaps)
            {
                angle += Time.deltaTime * _lapSpeed;

                if (angle >= _fullCircleAngle)
                {
                    angle -= _fullCircleAngle;
                    completedLaps++;
                }

                Vector3 newCirclePoint = new(Mathf.Cos(angle) * _radius, 0.0f, Mathf.Sin(angle) * _radius);

                _satellitePoint = _playerTransform.position + newCirclePoint;
                FollowTarget(_satellitePoint);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            _satelliteCoroutine = null;
        }

        #endregion

        #region Helpers

        private bool IsTargetTooFar(Vector3 targetPosition, float radius)
        {
            return Vector3.Distance(targetPosition, transform.position) > radius ? true : false;
        }

        #endregion

        #region Editor

        private void OnDrawGizmos()
        {
            if (_playerTransform != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_playerTransform.position, _followRadius);
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, _radius);
            }
        }

        #endregion
    }
}
