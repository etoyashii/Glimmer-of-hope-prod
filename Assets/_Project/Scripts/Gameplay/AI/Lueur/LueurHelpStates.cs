using System;
using UnityEngine;
using GlimmerOfHope.Gameplay.AI;

namespace GlimmerOfHope.Gameplay.Lueur
{
    public class PlayerHelpState : State
    {
        private readonly LueurContext _context;
        private readonly StateMachine _subMachine = new();
        private readonly MoveToPointState _moveToPoint;
        private readonly AttractAttentionState _attractAttention;
        private Vector3 _poiPosition;

        public PlayerHelpState(LueurContext context)
        {
            _context = context;
            _moveToPoint = new MoveToPointState(_context, GetAttentionPoint, OnPointReached);
            _attractAttention = new AttractAttentionState(_context, GetAttentionPoint);
        }

        public override void Enter()
        {
            _context.PoiDetector.TryGetPointOfInterest(out _poiPosition);
            _subMachine.ChangeState(_moveToPoint);
        }

        public override void Tick()
        {
            _subMachine.Tick();
        }

        public override void Exit()
        {
            _context.AttentionCue.Stop();
        }

        private Vector3 GetAttentionPoint()
        {
            return Vector3.Lerp(_poiPosition, _context.Player.position, 0.5f);
        }

        private void OnPointReached()
        {
            _subMachine.ChangeState(_attractAttention);
        }
    }

    public class MoveToPointState : State
    {
        private readonly LueurContext _context;
        private readonly Func<Vector3> _getTarget;
        private readonly Action _onReached;

        public MoveToPointState(LueurContext context, Func<Vector3> getTarget, Action onReached)
        {
            _context = context;
            _getTarget = getTarget;
            _onReached = onReached;
        }

        public override void Tick()
        {
            Vector3 target = _getTarget();
            _context.Mover.MoveTo(target);

            if (_context.Mover.HasReached(target, _context.ArriveThreshold))
            {
                _onReached?.Invoke();
            }
        }
    }

    public class AttractAttentionState : State
    {
        private readonly LueurContext _context;
        private readonly Func<Vector3> _getTarget;
        private float _timer;

        public AttractAttentionState(LueurContext context, Func<Vector3> getTarget)
        {
            _context = context;
            _getTarget = getTarget;
        }

        public override void Enter()
        {
            _timer = 0.0f;
            _context.AttentionCue.Play();
        }

        public override void Tick()
        {
            _timer += Time.deltaTime;

            Vector3 basePosition = _getTarget();
            float hopOffset = Mathf.Abs(Mathf.Sin(_timer * _context.HopFrequency)) * _context.HopHeight;
            _context.Mover.MoveTo(new Vector3(basePosition.x, basePosition.y + hopOffset, basePosition.z));
        }

        public override void Exit()
        {
            _context.AttentionCue.Stop();
        }
    }
}
