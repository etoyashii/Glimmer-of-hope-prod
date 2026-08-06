using UnityEngine;
using GlimmerOfHope.Gameplay.AI;

namespace GlimmerOfHope.Gameplay.Lueur
{
    public class FollowPlayerState : State
    {
        private readonly LueurContext _context;

        public FollowPlayerState(LueurContext context)
        {
            _context = context;
        }

        public override void Tick()
        {
            Vector3 elevatedPlayerPosition = _context.Player.position + Vector3.up * _context.FollowHeightOffset;
            Vector3 target = elevatedPlayerPosition;
            float distance = Vector3.Distance(_context.Mover.Position, elevatedPlayerPosition);

            if (distance > _context.FollowRadius)
            {
                Vector3 direction = (_context.Mover.Position - elevatedPlayerPosition).normalized;
                target = elevatedPlayerPosition + direction * _context.FollowRadius;
            }

            _context.Mover.MoveTo(target);
        }
    }

    public class GoFrontOfPlayerState : State
    {
        private readonly LueurContext _context;
        private readonly StateMachine _owner;
        private readonly IState _idleState;

        public GoFrontOfPlayerState(LueurContext context, StateMachine owner, IState idleState)
        {
            _context = context;
            _owner = owner;
            _idleState = idleState;
        }

        public override void Tick()
        {
            _context.Mover.MoveTo(_context.StartPoint.position);

            if (_context.Mover.HasReached(_context.StartPoint.position, _context.ArriveThreshold))
            {
                _owner.ChangeState(_idleState);
            }
        }
    }
}