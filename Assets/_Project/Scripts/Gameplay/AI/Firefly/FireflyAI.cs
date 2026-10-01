using System;
using UnityEngine;
using UnityEngine.Events;
using GlimmerOfHope.Gameplay.AI;

namespace GlimmerOfHope.Gameplay.Firefly
{
    [DisallowMultipleComponent]
    public partial class FireflyAI : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [Tooltip("Player transform. Leave empty to resolve it by tag at runtime.")]
        [SerializeField] private Transform _playerTransform;
        [Tooltip("Tag used to find the player when no transform is assigned.")]
        [SerializeField] private string _playerTag = "Player";
        [Tooltip("Lantern the firefly flies into. Required.")]
        [SerializeField] private Transform _lanternTransform;
        [Tooltip("Offset added to the lantern center (collider bounds) to aim at its glass.")]
        [SerializeField] private Vector3 _entryOffset = Vector3.zero;

        [Header("Wake")]
        [Tooltip("Distance from the player that wakes the firefly up.")]
        [Range(0.1f, 30.0f)]
        [SerializeField] private float _wakeRadius = 1.5f;

        [Header("Follow")]
        [Range(0.1f, 20.0f)]
        [SerializeField] private float _maxSpeed = 6.0f;
        [Range(0.05f, 2.0f)]
        [SerializeField] private float _smoothTime = 0.3f;

        [Header("Orbit")]
        [Tooltip("Distance kept from the player while orbiting around him.")]
        [Range(0.05f, 10.0f)]
        [SerializeField] private float _orbitRadius = 0.25f;
        [Tooltip("Orbit speed in degrees per second. Negative turns the other way.")]
        [Range(-360.0f, 360.0f)]
        [SerializeField] private float _orbitSpeed = 75.0f;
        [Tooltip("Height offset from the player body center. 0 = mid height of the player.")]
        [Range(-2.0f, 3.0f)]
        [SerializeField] private float _orbitHeightOffset = 0.0f;
        [Tooltip("Height of the up and down wave along the orbit. 0 = flat circle.")]
        [Range(0.0f, 2.0f)]
        [SerializeField] private float _orbitWaveHeight = 0.2f;
        [Tooltip("Number of up and down waves in one full turn.")]
        [Range(0.0f, 6.0f)]
        [SerializeField] private float _orbitWaveCount = 3.0f;

        [Header("Lantern Approach")]
        [Tooltip("Distance from the lantern that makes the firefly leave the player.")]
        [Range(0.1f, 20.0f)]
        [SerializeField] private float _lanternRadius = 1.0f;
        [Range(0.01f, 2.0f)]
        [SerializeField] private float _arriveThreshold = 0.05f;
        [Range(0.1f, 5.0f)]
        [SerializeField] private float _absorbDuration = 0.6f;

        [Header("Idle Hover")]
        [Range(0.05f, 10.0f)]
        [SerializeField] private float _hoverMaxSpeed = 0.3f;
        [Range(0.05f, 2.0f)]
        [SerializeField] private float _hoverSmoothTime = 0.5f;
        [Range(0.0f, 2.0f)]
        [SerializeField] private float _hoverHeight = 0.05f;
        [Range(0.0f, 10.0f)]
        [SerializeField] private float _hoverFrequency = 2.0f;

        [Header("Feedback")]
        [SerializeField] private AudioSource _wakeAudio;
        [SerializeField] private AudioSource _absorbAudio;

        [Header("Safety")]
        [Tooltip("Disables own colliders so the firefly never pushes the player.")]
        [SerializeField] private bool _disableOwnColliders = true;

        [Header("Events")]
        public UnityEvent OnAwakened;
        public UnityEvent OnAbsorbed;

        #endregion

        #region Events

        public event Action EnteredLantern;

        #endregion

        #region Public Properties

        public bool IsAwake => _isAwake;
        public bool HasEnteredLantern => _hasEnteredLantern;
        public Vector3 LanternEntryPoint => _lanternTransform == null
            ? transform.position
            : GetLanternCenter() + _entryOffset;

        #endregion

        #region Private Fields

        private StateMachine _stateMachine;
        private FireflyContext _context;

        private FireflyDormantState _dormantState;
        private FireflyFollowPlayerState _followState;
        private FireflyEnterLanternState _enterState;
        private FireflyAbsorbedState _absorbedState;

        private Vector3 _homePosition;
        private Vector3 _initialScale;

        private bool _isAwake;
        private bool _isCommitted;
        private bool _hasEnteredLantern;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _homePosition = transform.position;
            _initialScale = transform.localScale;

            if (_lanternTransform == null)
            {
                Debug.LogWarning($"[FireflyAI] '{name}' has no lantern assigned, disabling.", this);
                enabled = false;
                return;
            }

            if (_disableOwnColliders)
            {
                DisableOwnColliders();
            }

            BuildContext();
            BuildStates();

            _stateMachine.ChangeState(_dormantState);
        }

        private void LateUpdate()
        {
            if (_stateMachine == null) return;
            if (!TryResolvePlayer()) return;

            SyncContext();
            EvaluateTransitions();

            _stateMachine.Tick();
        }

        #endregion

        #region Public Methods

        public void SetLantern(Transform lantern)
        {
            _lanternTransform = lantern;
        }

        public void ResetFirefly()
        {
            _isAwake = false;
            _isCommitted = false;
            _hasEnteredLantern = false;

            transform.position = _homePosition;
            transform.localScale = _initialScale;

            gameObject.SetActive(true);

            if (_stateMachine == null) return;

            _stateMachine.ChangeState(_dormantState);
        }

        #endregion

        #region Private Methods

        private void BuildContext()
        {
            _context = new FireflyContext
            {
                Self = transform,
                HomePosition = _homePosition,
                ArriveThreshold = _arriveThreshold,
                AbsorbDuration = _absorbDuration
            };

            _context.Mover = new SmoothMover(
                transform,
                () => _context.CurrentSmoothTime,
                () => _context.CurrentMaxSpeed);
        }

        private void BuildStates()
        {
            _stateMachine = new StateMachine();

            _dormantState = new FireflyDormantState(_context);
            _followState = new FireflyFollowPlayerState(_context);
            _enterState = new FireflyEnterLanternState(_context, OnReachedLantern);
            _absorbedState = new FireflyAbsorbedState(_context, OnAbsorbFinished);
        }

        private bool TryResolvePlayer()
        {
            if (_playerTransform != null)
            {
                _context.Player = _playerTransform;
                return true;
            }

            GameObject found = GameObject.FindGameObjectWithTag(_playerTag);

            if (found == null) return false;

            _playerTransform = found.transform;
            _context.Player = _playerTransform;

            return true;
        }

        private void SyncContext()
        {
            _context.LanternEntry = LanternEntryPoint;
            _context.OrbitRadius = _orbitRadius;
            _context.OrbitSpeed = _orbitSpeed;
            _context.OrbitHeight = _orbitHeightOffset;
            _context.OrbitWaveHeight = _orbitWaveHeight;
            _context.OrbitWaveCount = _orbitWaveCount;
            _context.PlayerCenter = GetPlayerCenter();
            _context.ArriveThreshold = _arriveThreshold;
            _context.HoverHeight = _hoverHeight;
            _context.HoverFrequency = _hoverFrequency;
            _context.AbsorbDuration = _absorbDuration;

            bool hovering = _stateMachine.IsInState(_dormantState);

            _context.CurrentMaxSpeed = hovering ? _hoverMaxSpeed : _maxSpeed;
            _context.CurrentSmoothTime = hovering ? _hoverSmoothTime : _smoothTime;
        }

        private void EvaluateTransitions()
        {
            if (_isCommitted) return;

            if (!_isAwake)
            {
                if (!IsWithin(_context.PlayerCenter, transform.position, _wakeRadius)) return;

                _isAwake = true;
                _stateMachine.ChangeState(_followState);

                PlayCue(_wakeAudio);
                OnAwakened?.Invoke();

                return;
            }

            if (!IsWithin(transform.position, _context.LanternEntry, _lanternRadius)) return;

            _isCommitted = true;
            _stateMachine.ChangeState(_enterState);
        }

        private void OnReachedLantern()
        {
            if (_hasEnteredLantern) return;

            _hasEnteredLantern = true;

            PlayCue(_absorbAudio);

            EnteredLantern?.Invoke();
            OnAbsorbed?.Invoke();

            _stateMachine.ChangeState(_absorbedState);
        }

        private void OnAbsorbFinished()
        {
            gameObject.SetActive(false);
        }

        private void DisableOwnColliders()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

        private static bool IsWithin(Vector3 from, Vector3 to, float radius)
        {
            return (from - to).sqrMagnitude <= radius * radius;
        }

        private static void PlayCue(AudioSource source)
        {
            if (source == null) return;
            if (source.isPlaying) return;

            source.Play();
        }

        #endregion
    }
}
