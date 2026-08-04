using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using UnityEngine.VFX;

namespace GlimmerOfHope.Gameplay
{
    public class ArmController : MonoBehaviour
    {
        [Header("References")]
        public SplineContainer splineContainer;
        public VisualEffect visualEffect;

        [Header("VFX")]
        [Range(8, 256)]
        public int sampleCount = 64;

        [Header("Index des points concernés")]
        public int greenIndex = 3;
        public int[] yellowIndices = { 5, 6 };
        public int redIndex = 7;

        [Header("Green")]
        public float greenLoopRadius = 0.2f;
        public float greenNoiseSpeed = 0.5f;

        [Header("Yellow")]
        public float yellowAmplitude = 0.3f;
        public float yellowSpeed = 1f;

        [Header("Red")]
        public float redRadius = 0.25f;
        public float redSpeed = 1f;

        private float3[] basePositions;

        private GraphicsBuffer splineBuffer;
        private Vector3[] sampledPositions;

        void Start()
        {
            var spline = splineContainer.Spline;

            basePositions = new float3[spline.Count];

            for (int i = 0; i < spline.Count; i++)
                basePositions[i] = spline[i].Position;

            sampledPositions = new Vector3[sampleCount];

            splineBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                sampleCount,
                sizeof(float) * 3);

            if (visualEffect != null)
            {
                visualEffect.SetGraphicsBuffer("SplinePoints", splineBuffer);
                visualEffect.SetInt("PointCount", sampleCount);
            }
        }

        void Update()
        {
            AnimateSpline();

            UpdateSplineBuffer();
        }

        void AnimateSpline()
        {
            var spline = splineContainer.Spline;
            float t = Time.time;

            // GREEN
            {
                float nx = Mathf.PerlinNoise(t * greenNoiseSpeed, 0f) * 2f - 1f;
                float ny = Mathf.PerlinNoise(0f, t * greenNoiseSpeed) * 2f - 1f;

                float3 offset = new float3(nx, ny, 0f) * greenLoopRadius;

                SetPoint(spline, greenIndex, basePositions[greenIndex] + offset);
            }

            // YELLOW
            {
                float x = Mathf.Sin(t * yellowSpeed) * yellowAmplitude;

                foreach (int idx in yellowIndices)
                {
                    float3 offset = new float3(x, 0f, 0f);
                    SetPoint(spline, idx, basePositions[idx] + offset);
                }
            }

            // RED
            {
                float x = Mathf.Cos(t * redSpeed) * redRadius;
                float y = Mathf.Sin(t * redSpeed) * redRadius;

                float3 offset = new float3(x, y, 0f);

                SetPoint(spline, redIndex, basePositions[redIndex] + offset);
            }
        }

        void UpdateSplineBuffer()
        {
            var spline = splineContainer.Spline;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)(sampleCount - 1);

                float3 pos = SplineUtility.EvaluatePosition(spline, t);

                sampledPositions[i] = new Vector3(pos.x, pos.y, pos.z);
            }

            splineBuffer.SetData(sampledPositions);
        }

        void SetPoint(Spline spline, int index, float3 newPos)
        {
            var knot = spline[index];
            knot.Position = newPos;
            spline[index] = knot;
        }

        void OnDestroy()
        {
            splineBuffer?.Release();
        }
    }
}