using System;
using UnityEngine;

namespace GlimmerOfHope.Gameplay.AI
{
    public interface IMover
    {
        Vector3 Position { get; }
        void MoveTo(Vector3 target);
        bool HasReached(Vector3 target, float threshold);
    }

    public class SmoothMover : IMover
    {
        private readonly Transform _transform;
        private readonly Func<float> _smoothTimeProvider;
        private readonly Func<float> _maxSpeedProvider;
        private Vector3 _velocity;

        public SmoothMover(Transform transform, Func<float> smoothTimeProvider, Func<float> maxSpeedProvider)
        {
            _transform = transform;
            _smoothTimeProvider = smoothTimeProvider;
            _maxSpeedProvider = maxSpeedProvider;
        }

        public Vector3 Position => _transform.position;

        public void MoveTo(Vector3 target)
        {
            _transform.position = Vector3.SmoothDamp(
                _transform.position,
                target,
                ref _velocity,
                _smoothTimeProvider(),
                _maxSpeedProvider()
            );
        }

        public bool HasReached(Vector3 target, float threshold)
        {
            return Vector3.Distance(_transform.position, target) <= threshold;
        }
    }
}