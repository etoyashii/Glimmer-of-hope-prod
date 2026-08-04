using System.Runtime.InteropServices;
using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    [StructLayout(LayoutKind.Sequential)]
    public struct GrayZoneData
    {
        public Matrix4x4 worldToLocal;
        public Vector2 size;
        public float threshold;
        public float contrast;
        public int maskIndex;
        public Vector2 paintOrigin;
        public float paintRadius;
    }

    public interface IGrayZoneReceiver
    {
        void EnterGrayZone(GrayZone zone);
        void ExitGrayZone(GrayZone zone);
    }
}