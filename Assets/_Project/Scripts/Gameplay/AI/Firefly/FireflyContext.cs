using UnityEngine;
using GlimmerOfHope.Gameplay.AI;

namespace GlimmerOfHope.Gameplay.Firefly
{
    public class FireflyContext
    {
        public Transform Self;
        public Transform Player;
        public Vector3 PlayerCenter;
        public IMover Mover;

        public Vector3 HomePosition;
        public Vector3 LanternEntry;

        public float OrbitRadius;
        public float OrbitSpeed;
        public float OrbitHeight;
        public float OrbitWaveHeight;
        public float OrbitWaveCount;
        public float ArriveThreshold;

        public float HoverHeight;
        public float HoverFrequency;
        public float AbsorbDuration;

        public float CurrentMaxSpeed;
        public float CurrentSmoothTime;
    }
}
