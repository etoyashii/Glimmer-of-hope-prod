using UnityEngine;

namespace GlimmerOfHope.Gameplay
{
    public class GrassParamTarget : MonoBehaviour
    {
        public PointGrassRenderer target;

        public void SetPointCount(float value) { if (target != null) { target.pointCount = value; } }
        public void SetPointLODFactor(float value) { if (target != null) { target.pointLODFactor = value; } }
        public void SetMaxRenderDistance(float value) { if (target != null) { target.maxRenderDistance = value; } }
        public void SetFadeStartDistance(float value) { if (target != null) { target.fadeStartDistance = value; } }
        public void SetDensityCutoff(float value) { if (target != null) { target.densityCutoff = value; } }
    }
}