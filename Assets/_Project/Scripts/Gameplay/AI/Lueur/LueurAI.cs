using UnityEngine;
using GlimmerOfHope.Gameplay.AI;
using GlimmerOfHope.Gameplay.Character.SpecialActions;

namespace GlimmerOfHope.Gameplay.Lueur
{
    public class LueurAI : MonoBehaviour, IPointOfInterestDetector, IAttentionCue
    {
        [Header("References")]
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private Transform _lueurStartPoint;
        [SerializeField] private Movement _playerMovement;
        [SerializeField] private AudioSource _attentionAudioSource;

        [Header("Follow / Help Movement")]
        [Range(1.0f, 100.0f)]
        [SerializeField] private float _maxSpeed = 2.0f;
        [Range(0.0f, 5.0f)]
        [SerializeField] private float _smoothTime = 0.3f;
        [Range(0.0f, 100.0f)]
        [SerializeField] private float _followRadius = 50.0f;
        [SerializeField] private float _followHeightOffset = 1.5f;
        [SerializeField] private float _followRightOffset = 1.5f;

        [Header("Idle Movement")]
        [Range(1.0f, 100.0f)]
        [SerializeField] private float _idleMaxSpeed = 1.0f;
        [Range(0.0f, 5.0f)]
        [SerializeField] private float _idleSmoothTime = 0.5f;
        [Range(0.0f, 5.0f)]
        [SerializeField] private float _patrolRadius = 5.0f;
        [Range(0.0f, 1.0f)]
        [SerializeField] private float _hopChance = 0.3f;
        [SerializeField] private float _hopHeight = 0.5f;
        [SerializeField] private float _hopFrequency = 6.0f;
        [Range(0.05f, 2.0f)]
        [SerializeField] private float _wanderSpeed = 0.3f;

        [Header("Player Help")]
        [SerializeField] private bool _playerHelpEnabled = true;
        [SerializeField] private float _poiDetectionRadius = 10.0f;
        [SerializeField] private LayerMask _poiLayerMask;

        private StateMachine _stateMachine;
        private LueurContext _context;

        private IdleState _idleState;
        private FollowPlayerState _followState;
        private GoFrontOfPlayerState _goFrontState;
        private PlayerHelpState _helpState;

        private bool _playerIsMoving;

        private void Awake()
        {
            _playerMovement.OnPlayerStartMoving += OnPlayerMoving;
            _playerMovement.OnPlayerStopMoving += OnPlayerStopMoving;

            transform.position = _lueurStartPoint.position;

            _context = new LueurContext
            {
                Self = transform,
                Player = _playerTransform,
                StartPoint = _lueurStartPoint,
                PoiDetector = this,
                AttentionCue = this,
                PatrolRadius = _patrolRadius,
                FollowRadius = _followRadius,
                FollowHeightOffset = _followHeightOffset,
                FollowRightOffset = _followRightOffset,
                HopChance = _hopChance,
                HopHeight = _hopHeight,
                HopFrequency = _hopFrequency,
                WanderSpeed = _wanderSpeed
            };

            _context.Mover = new SmoothMover(transform, () => _context.CurrentSmoothTime, () => _context.CurrentMaxSpeed);

            _stateMachine = new StateMachine();
            _idleState = new IdleState(_context);
            _followState = new FollowPlayerState(_context);
            _helpState = new PlayerHelpState(_context);
            _goFrontState = new GoFrontOfPlayerState(_context, _stateMachine, _idleState);

            _stateMachine.ChangeState(_idleState);
        }

        private void LateUpdate()
        {
            SyncContextFromInspector();
            EvaluateTransitions();
            _stateMachine.Tick();
        }

        private void SyncContextFromInspector()
        {
            _context.PatrolRadius = _patrolRadius;
            _context.FollowRadius = _followRadius;
            _context.FollowHeightOffset = _followHeightOffset;
            _context.FollowRightOffset = _followRightOffset;
            _context.HopChance = _hopChance;
            _context.HopHeight = _hopHeight;
            _context.HopFrequency = _hopFrequency;
            _context.WanderSpeed = _wanderSpeed;

            bool isIdle = _stateMachine.IsInState(_idleState);
            _context.CurrentMaxSpeed = isIdle ? _idleMaxSpeed : _maxSpeed;
            _context.CurrentSmoothTime = isIdle ? _idleSmoothTime : _smoothTime;
        }

        private void OnDisable()
        {
            _playerMovement.OnPlayerStartMoving -= OnPlayerMoving;
            _playerMovement.OnPlayerStopMoving -= OnPlayerStopMoving;
        }

        private void EvaluateTransitions()
        {
            if (_playerHelpEnabled && TryGetPointOfInterest(out _))
            {
                if (!_stateMachine.IsInState(_helpState))
                {
                    _stateMachine.ChangeState(_helpState);
                }
                return;
            }

            if (_playerIsMoving)
            {
                if (!_stateMachine.IsInState(_followState))
                {
                    _stateMachine.ChangeState(_followState);
                }
                return;
            }

            if (_stateMachine.IsInState(_followState) || _stateMachine.IsInState(_helpState))
            {
                _stateMachine.ChangeState(_goFrontState);
            }
        }

        private void OnPlayerMoving()
        {
            _playerIsMoving = true;
        }

        private void OnPlayerStopMoving()
        {
            _playerIsMoving = false;
        }

        public bool TryGetPointOfInterest(out Vector3 position)
        {
            Collider[] hits = Physics.OverlapSphere(_playerTransform.position, _poiDetectionRadius, _poiLayerMask);

            if (hits.Length > 0)
            {
                position = hits[0].transform.position;
                return true;
            }

            position = Vector3.zero;
            return false;
        }

        public void Play()
        {
            if (_attentionAudioSource != null && !_attentionAudioSource.isPlaying)
            {
                _attentionAudioSource.Play();
            }
        }

        public void Stop()
        {
            if (_attentionAudioSource != null)
            {
                _attentionAudioSource.Stop();
            }
        }

        private void OnDrawGizmos()
        {
            if (_playerTransform != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_playerTransform.position, _followRadius);
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(transform.position, _patrolRadius);
            }
        }
    }
}