using UnityEngine;

namespace GlimmerOfHope.Editor.Tools
{
    public class PuzzleLucioleSpot
    {
        public Transform Root;
        public Transform Firefly;
        public Transform BrokenLantern;
        public Transform GrayZone;

        public bool HasManager;
        public bool HasFireflyAI;
        public bool HasLanternElement;
        public bool HasSwarm;
        public bool HasRepairedVisual;

        public string Path;
        public string SuggestedId;

        public bool IsWired => HasManager && HasFireflyAI && HasLanternElement && HasSwarm && HasRepairedVisual;

        public string Status
        {
            get
            {
                if (IsWired) return "deja cable";
                if (HasManager || HasFireflyAI || HasLanternElement || HasSwarm) return "partiellement cable";
                return "a cabler";
            }
        }
    }
}
