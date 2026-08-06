using System;
using UnityEngine;
using GlimmerOfHope.Gameplay.AI;

namespace GlimmerOfHope.Gameplay.Lueur
{
    public class IdleState : State
    {
        private readonly LueurContext _context;
        private readonly StateMachine _subMachine = new();
        private readonly PatrolState _patrol;
        private readonly HopState _hop;

        public IdleState(LueurContext context)
        {
            _context = context;
            _patrol = new PatrolState(_context, OnPatrolPointReached);
            _hop = new HopState(_context, _subMachine, _patrol);
        }

        public override void Enter()
        {
            _subMachine.ChangeState(_patrol);
        }

        public override void Tick()
        {
            _subMachine.Tick();
        }

        private void OnPatrolPointReached()
        {
            if (UnityEngine.Random.value < _context.HopChance)
            {
                _subMachine.ChangeState(_hop);
            }
        }
    }

    public class PatrolState : State
    {
        private readonly LueurContext _context;
        private readonly Action _onHopCheck;
        private float _noiseSeedX;
        private float _noiseSeedY;
        private float _noiseSeedZ;
        private float _elapsedTime;
        private float _nextHopCheckTime;

        public PatrolState(LueurContext context, Action onHopCheck)
        {
            _context = context;
            _onHopCheck = onHopCheck;
        }

        public override void Enter()
        {
            _noiseSeedX = UnityEngine.Random.value * 100.0f;
            _noiseSeedY = UnityEngine.Random.value * 100.0f;
            _noiseSeedZ = UnityEngine.Random.value * 100.0f;
            _elapsedTime = 0.0f;
            ScheduleNextHopCheck();
        }

        public override void Tick()
        {
            _elapsedTime += Time.deltaTime;

            Vector3 destination = _context.StartPoint.position + GetWanderOffset();
            _context.Mover.MoveTo(destination);

            if (_elapsedTime >= _nextHopCheckTime)
            {
                ScheduleNextHopCheck();
                _onHopCheck?.Invoke();
            }
        }

        private void ScheduleNextHopCheck()
        {
            _nextHopCheckTime = _elapsedTime + UnityEngine.Random.Range(1.5f, 3.5f);
        }

        private Vector3 GetWanderOffset()
        {
            float t = _elapsedTime * _context.WanderSpeed;

            float x = (Mathf.PerlinNoise(_noiseSeedX, t) - 0.5f) * 2.0f;
            float y = Mathf.PerlinNoise(_noiseSeedY, t);
            float z = (Mathf.PerlinNoise(_noiseSeedZ, t) - 0.5f) * 2.0f;

            Vector3 direction = new Vector3(x, y, z);
            return Vector3.ClampMagnitude(direction, 1.0f) * _context.PatrolRadius;
        }
    }

    public class HopState : State
    {
        private readonly LueurContext _context;
        private readonly StateMachine _owner;
        private readonly IState _returnState;
        private float _timer;
        private float _duration;
        private Vector3 _hopCenter;

        public HopState(LueurContext context, StateMachine owner, IState returnState)
        {
            _context = context;
            _owner = owner;
            _returnState = returnState;
        }

        public override void Enter()
        {
            _timer = 0.0f;
            _duration = UnityEngine.Random.Range(0.6f, 1.2f);
            _hopCenter = _context.Mover.Position;
        }

        public override void Tick()
        {
            _timer += Time.deltaTime;

            float hopOffset = Mathf.Abs(Mathf.Sin(_timer * _context.HopFrequency)) * _context.HopHeight;
            _context.Self.position = new Vector3(_hopCenter.x, _hopCenter.y + hopOffset, _hopCenter.z);

            if (_timer >= _duration)
            {
                _owner.ChangeState(_returnState);
            }
        }
    }
}