using UnityEngine;

namespace GlimmerOfHope.Gameplay.Lueur
{
    public interface IPointOfInterestDetector
    {
        bool TryGetPointOfInterest(out Vector3 position);
    }

    public interface IAttentionCue
    {
        void Play();
        void Stop();
    }
}
