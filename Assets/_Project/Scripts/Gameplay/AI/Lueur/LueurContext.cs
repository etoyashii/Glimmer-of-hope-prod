using UnityEngine;
using GlimmerOfHope.Gameplay.AI;

namespace GlimmerOfHope.Gameplay.Lueur
{
    public class LueurContext
    {
        public Transform Self;
        public Transform Player;
        public Transform StartPoint;
        public IMover Mover;
        public IPointOfInterestDetector PoiDetector;
        public IAttentionCue AttentionCue;

        public float PatrolRadius;
        public float FollowRadius;
        public float FollowHeightOffset;
        public float ArriveThreshold = 0.2f;
        public float HopChance;
        public float HopHeight;
        public float HopFrequency;
        public float WanderSpeed;
        public float CurrentMaxSpeed;
        public float CurrentSmoothTime;
    }
}