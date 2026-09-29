using System;
using UnityEngine;
using GlimmerOfHope.Gameplay.AI;

namespace GlimmerOfHope.Gameplay.Firefly
{
    public class FireflyDormantState : State
    {
        private readonly FireflyContext _context;

        private float _timer;

        public FireflyDormantState(FireflyContext context)
        {
            _context = context;
        }

        public override void Enter()
        {
            _timer = 0.0f;
        }

        public override void Tick()
        {
            _timer += Time.deltaTime;

            float height = Mathf.Sin(_timer * _context.HoverFrequency) * _context.HoverHeight;

            _context.Mover.MoveTo(_context.HomePosition + Vector3.up * height);
        }
    }

    public class FireflyFollowPlayerState : State
    {
        private readonly FireflyContext _context;

        private float _angle;

        public FireflyFollowPlayerState(FireflyContext context)
        {
            _context = context;
        }

        public override void Enter()
        {
            Vector3 dir = _context.Mover.Position - _context.PlayerCenter;

            _angle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
        }

        public override void Tick()
        {
            _angle += _context.OrbitSpeed * Time.deltaTime;

            float rad = _angle * Mathf.Deg2Rad;
            float bob = Mathf.Sin(rad * _context.OrbitWaveCount) * _context.OrbitWaveHeight;

            Vector3 ring = new Vector3(Mathf.Cos(rad), 0.0f, Mathf.Sin(rad)) * _context.OrbitRadius;
            Vector3 target = _context.PlayerCenter + ring + Vector3.up * (_context.OrbitHeight + bob);

            _context.Mover.MoveTo(target);
        }
    }

    public class FireflyEnterLanternState : State
    {
        private readonly FireflyContext _context;
        private readonly Action _onReached;

        private bool _hasReached;

        public FireflyEnterLanternState(FireflyContext context, Action onReached)
        {
            _context = context;
            _onReached = onReached;
        }

        public override void Enter()
        {
            _hasReached = false;
        }

        public override void Tick()
        {
            _context.Mover.MoveTo(_context.LanternEntry);

            if (_hasReached) return;
            if (!_context.Mover.HasReached(_context.LanternEntry, _context.ArriveThreshold)) return;

            _hasReached = true;
            _onReached?.Invoke();
        }
    }

    public class FireflyAbsorbedState : State
    {
        private readonly FireflyContext _context;
        private readonly Action _onFinished;

        private Vector3 _startScale;
        private float _timer;
        private bool _hasFinished;

        public FireflyAbsorbedState(FireflyContext context, Action onFinished)
        {
            _context = context;
            _onFinished = onFinished;
        }

        public override void Enter()
        {
            _timer = 0.0f;
            _hasFinished = false;
            _startScale = _context.Self.localScale;
        }

        public override void Tick()
        {
            if (_hasFinished) return;

            _timer += Time.deltaTime;

            float ratio = Mathf.Clamp01(_timer / _context.AbsorbDuration);

            _context.Mover.MoveTo(_context.LanternEntry);
            _context.Self.localScale = Vector3.Lerp(_startScale, Vector3.zero, ratio);

            if (ratio < 1.0f) return;

            _hasFinished = true;
            _onFinished?.Invoke();
        }
    }
}
