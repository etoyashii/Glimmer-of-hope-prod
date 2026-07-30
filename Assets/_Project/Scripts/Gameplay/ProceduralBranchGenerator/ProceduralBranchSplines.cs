using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;


namespace GlimmerOfHope.Editor
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(SplineContainer))]
    public class ProceduralBranchSplines : MonoBehaviour
    {
        #region Inspector
        [Header("Main branch")]
        [SerializeField] private int _pointsPerBranch = 12;
        [SerializeField] private float _distanceBetweenPoints = 2f;
        [SerializeField] private Vector3 _initialDirection = Vector3.forward;

        [Header("Sub branches")]
        [SerializeField] private int _subBranchCount = 3;
        [SerializeField][Range(0f, 1f)] private float _branchSplitStart = 0.5f;
        [SerializeField] private int _reduceCountMin = 0;
        [SerializeField] private int _reduceCountMax = 1;
        [SerializeField][Range(5f, 90f)] private float _branchAngleSpread = 35f;
        [SerializeField][Range(0f, 30f)] private float _branchAngleVariance = 10f;
        [SerializeField][Range(0, 3)] private int _recursionDepth = 2;
        [SerializeField][Range(0f, 1f)] private float _maxVerticalAngle = 0.2f;

        [Header("Noise and twist")]
        [SerializeField][Range(0f, 90f)] private float _maxSlopeDeg = 20f;
        [SerializeField][Range(0f, 2f)] private float _noiseForce = 0.4f;
        [SerializeField][Range(0.1f, 5f)] private float _noiseFrequency = 1.2f;
        [SerializeField][Range(0f, 90f)] private float _twistDegreesPerStep = 15f;
        [SerializeField][Range(0f, 1f)] private float _gravityInfluence = 0.08f;

        [Header("Scale by depth")]
        [SerializeField][Range(0.3f, 1f)] private float _lengthMultiplierPerDepth = 0.65f;
        [SerializeField][Range(0.3f, 1f)] private float _pointCountMultiplierPerDepth = 0.5f;

        [Header("Advanced")]
        [SerializeField] private int _seed = 42;
        [SerializeField] private bool _autoRegenerate = false;
        [SerializeField][Range(0f, 1f)] private float _splineTension = 0.4f;
        [SerializeField] private bool _smooth = false;
        #endregion

        #region Private
        private SplineContainer _container;
        private System.Random _rng;
        private int _nextId;

        private struct PendingSpline
        {
            public int Id;
            public Spline Spline;
            public int Depth;
            public int ParentId;
            public int ParentKnotIndex;
        }

        private List<PendingSpline> _pending;
        #endregion

        void OnValidate()
        {
            if (!_autoRegenerate) return;
            UnityEditor.EditorApplication.delayCall -= Generate;
            UnityEditor.EditorApplication.delayCall += Generate;
        }

        [ContextMenu("Generate Branches")]
        public void Generate()
        {
            _container = GetComponent<SplineContainer>();
            _rng = new System.Random(_seed);
            _pending = new List<PendingSpline>();
            _nextId = 0;

            for (int i = _container.Splines.Count - 1; i >= 0; i--)
                _container.RemoveSplineAt(i);

            GenerateBranch(Vector3.zero, _initialDirection.normalized,
                           0, _pointsPerBranch, _distanceBetweenPoints,
                           parentId: -1, parentKnotIndex: 0);

            _pending.Sort((a, b) => a.Depth.CompareTo(b.Depth));

            var idToIndex = new Dictionary<int, int>();
            for (int i = 0; i < _pending.Count; i++)
            {
                _container.AddSpline(_pending[i].Spline);
                idToIndex[_pending[i].Id] = i;
            }

            _container.KnotLinkCollection.Clear();
            for (int i = 0; i < _pending.Count; i++)
            {
                var ps = _pending[i];
                if (ps.ParentId == -1) continue;
                _container.KnotLinkCollection.Link(
                    new SplineKnotIndex(idToIndex[ps.ParentId], ps.ParentKnotIndex),
                    new SplineKnotIndex(idToIndex[ps.Id], 0));
            }

            _pending = null;
        }

        void GenerateBranch(Vector3 origin, Vector3 direction,
                            int depth, int numPoints, float stepDist,
                            int parentId, int parentKnotIndex)
        {
            if (depth > _recursionDepth) return;

            int myId = _nextId++;
            var knots = new List<BezierKnot>();

            Vector3 pos = origin;
            Vector3 dir = direction.normalized;

            float noiseOffsetX = (float)(_rng.NextDouble() * 1000.0);
            float noiseOffsetZ = (float)(_rng.NextDouble() * 1000.0);
            float twistAccum = (float)(_rng.NextDouble() * 360.0);

            int splitStartMin = Mathf.Clamp(Mathf.RoundToInt(_branchSplitStart * (numPoints - 1)), 1, numPoints - 2);
            int splitStartIndex = _rng.Next(splitStartMin, numPoints - 1);
            int branchCount = _subBranchCount;

            var knotPos = new Vector3[numPoints];
            var knotDir = new Vector3[numPoints];

            for (int i = 0; i < numPoints; i++)
            {
                float t = (float)i / Mathf.Max(numPoints - 1, 1);
                float nx = (Mathf.PerlinNoise(t * _noiseFrequency + noiseOffsetX, 0.3f) - 0.5f) * 2f;
                float nz = (Mathf.PerlinNoise(0.7f, t * _noiseFrequency + noiseOffsetZ) - 0.5f) * 2f;

                Vector3 noise = new Vector3(nx, 0f, nz) * _noiseForce + Vector3.down * _gravityInfluence;
                noise = Quaternion.AngleAxis(twistAccum, dir) * noise;
                twistAccum += _twistDegreesPerStep;

                Vector3 newDir = (dir + noise).normalized;
                float maxY = Mathf.Sin(_maxSlopeDeg * Mathf.Deg2Rad) * (1f + depth * _maxVerticalAngle);
                newDir.y = Mathf.Clamp(newDir.y, -maxY, maxY);
                dir = newDir.normalized;

                knotPos[i] = pos;
                knotDir[i] = dir;

                float tangentLen = stepDist * _splineTension;
                float3 tf = (float3)(dir * tangentLen);
                knots.Add(new BezierKnot((float3)pos, -tf, tf, quaternion.identity));
                pos += dir * stepDist;
            }

            var spline = new Spline(knots, false);
            if (_smooth) spline.SetTangentMode(TangentMode.AutoSmooth);

            _pending.Add(new PendingSpline
            {
                Id = myId,
                Spline = spline,
                Depth = depth,
                ParentId = parentId,
                ParentKnotIndex = parentKnotIndex
            });

            if (depth < _recursionDepth)
            {
                for (int i = splitStartIndex; i < numPoints && branchCount > 0; i++)
                {
                    SpawnSubBranches(knotPos[i], knotDir[i], depth + 1, stepDist, branchCount,
                                     parentId: myId, parentKnotIndex: i);
                    branchCount -= _rng.Next(_reduceCountMin, _reduceCountMax + 1);
                }
            }
        }

        void SpawnSubBranches(Vector3 origin, Vector3 parentDir,
                              int depth, float parentStepDist, int count,
                              int parentId, int parentKnotIndex)
        {
            Vector3 perp = Vector3.Cross(parentDir, Vector3.up).normalized;
            if (perp.sqrMagnitude < 0.001f)
                perp = Vector3.Cross(parentDir, Vector3.right).normalized;

            float angleStep = 360f / Mathf.Max(count, 1);

            for (int b = 0; b < count; b++)
            {
                float jitter = (float)((_rng.NextDouble() - 0.5) * angleStep * 0.4);
                float rotAngle = angleStep * b + jitter;
                Vector3 spreadDir = Quaternion.AngleAxis(rotAngle, parentDir) * perp;

                float spread = Mathf.Clamp(
                    _branchAngleSpread + (float)((_rng.NextDouble() - 0.5) * _branchAngleVariance * 2.0),
                    5f, 175f);
                Vector3 spreadAxis = Vector3.Cross(parentDir, spreadDir).normalized;
                Vector3 branchDir = Quaternion.AngleAxis(spread, spreadAxis) * parentDir;

                int childPoints = Mathf.Max(3, Mathf.RoundToInt(_pointsPerBranch * Mathf.Pow(_pointCountMultiplierPerDepth, depth)));
                float childStep = parentStepDist * Mathf.Pow(_lengthMultiplierPerDepth, depth);

                GenerateBranch(origin, branchDir, depth, childPoints, childStep, parentId, parentKnotIndex);
            }
        }
    }
}
#endif