using System.Collections;
using UnityEngine;

/// <summary>
/// Drives the flock direction along a Catmull-Rom spline (min 4 control points).
/// Advances a tracker at constant speed using arc-length parameterization.
/// Exposes desiredDirection (lookahead point) read by FlockManager in LateUpdate.
/// Loop mode: on reaching the end, waits resetDelay then teleports all birds
/// back to the start with random scatter, then resets tracker to t=0.
/// Exposes trackerPosition so FlockManager bounds follow the spline, not the origin.
/// Execution order: Update() here runs before FlockManager LateUpdate().
/// </summary>
public class SplineFlockController : MonoBehaviour
{
    #region Inspector propreties
    [Header("Ref")]
    [SerializeField]
    private FlockManager flockManager;

    [Header("Spline")]
    [Tooltip("Controle points Catmull-Rom (min 4)")]
    [SerializeField]
    private Transform[] controlPoints;

    [Header("Follow")]
    [SerializeField]
    private float trackerSpeed = 6f;
    [Tooltip("Distance of lookahead")]
    [SerializeField]
    private float lookahead = 8f;
    [Range(0.5f, 10f)]
    [SerializeField]
    private float turnSpeed = 2f;

    [Header("Loop")]
    [Tooltip("Enable looping (birds are tp to the begin)")]
    [SerializeField]
    private bool loopMode = true;
    [Tooltip("Delay before tp")]
    [SerializeField]
    private float resetDelay = 1.5f;
    [Tooltip("Spwan radius respwan")]
    [SerializeField]
    private Vector3 spawnScatter = new Vector3(6f, 2f, 4f);

    [Header("Debug")]
    [SerializeField]
    private bool showSpline = true;
    [SerializeField]
    private int splineResolution = 50;
    #endregion

    #region Propreties
    private float t = 0f;
    private float totalLength;
    private float[] segmentLengths;

    private bool reachedEnd = false;
    private bool isResetting = false;

    [HideInInspector]
    public Vector3 desiredDirection;
    [HideInInspector]
    public bool hasDesiredDirection;
    [HideInInspector]
    public Vector3 trackerPosition;

    private int NumSegments => controlPoints.Length - 3;
    private bool ValidatePoints() => controlPoints != null && controlPoints.Length >= 4;


    #endregion

    #region Methods

    private void Start()
    {
        if (!ValidatePoints()) return;
        PrecomputeLength();
        trackerPosition = EvaluateSpline(0f);
    }

    private void Update()
    {
        if (!ValidatePoints() || isResetting) 
        { 
            hasDesiredDirection = false; 
            return; 
        }

        if (!reachedEnd)
        {
            t = AdvanceByDistance(t, trackerSpeed * Time.deltaTime);
            trackerPosition = EvaluateSpline(t);

            if (loopMode && t >= NumSegments - 0.01f)
            {
                reachedEnd = true;
                StartCoroutine(ResetRoutine());
            }
        }

        float tTarget = Mathf.Min(AdvanceByDistance(t, lookahead), NumSegments - 0.001f);
        Vector3 target = EvaluateSpline(tTarget);
        Vector3 desired = (target - trackerPosition).normalized;

        if (desired == Vector3.zero) 
        { 
            hasDesiredDirection = false; 
            return; 
        }

        desiredDirection = desired;
        hasDesiredDirection = true;
    }


    private IEnumerator ResetRoutine()
    {
        isResetting = true;

        yield return new WaitForSeconds(resetDelay);

        Vector3 splineStart = EvaluateSpline(0f);

        for (int i = 0; i < flockManager.BirdCount; i++)
        {
            Vector3 scatter = new Vector3(
                Random.Range(-spawnScatter.x, spawnScatter.x),
                Random.Range(-spawnScatter.y, spawnScatter.y),
                Random.Range(-spawnScatter.z, spawnScatter.z)
            );
            flockManager.TeleportBird(i, splineStart + scatter);
        }

        t = 0f;
        reachedEnd = false;
        trackerPosition = EvaluateSpline(0f);

        yield return new WaitForSeconds(0.1f);
        isResetting = false;
    }

    private Vector3 EvaluateSpline(float t)
    {
        int n = controlPoints.Length;
        t = Mathf.Clamp(t, 0, NumSegments - 0.001f);

        int seg = Mathf.Clamp(Mathf.FloorToInt(t), 0, NumSegments - 1);
        float localT = t - seg;

        int p0 = Mathf.Clamp(seg - 1, 0, n - 1);
        int p1 = Mathf.Clamp(seg, 0, n - 1);
        int p2 = Mathf.Clamp(seg + 1, 0, n - 1);
        int p3 = Mathf.Clamp(seg + 2, 0, n - 1);

        return CatmullRom(
            controlPoints[p0].position,
            controlPoints[p1].position,
            controlPoints[p2].position,
            controlPoints[p3].position,
            localT
        );
    }

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return 0.5f * (
              2f * p1
            + (-p0 + p2) * t
            + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
            + (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }


    private void PrecomputeLength()
    {
        int samples = splineResolution;
        segmentLengths = new float[NumSegments * samples];
        totalLength = 0f;

        for (int seg = 0; seg < NumSegments; seg++)
        {
            Vector3 prev = EvaluateSpline(seg);
            for (int s = 1; s <= samples; s++)
            {
                Vector3 curr = EvaluateSpline(seg + (float)s / samples);
                float d = Vector3.Distance(prev, curr);
                segmentLengths[seg * samples + (s - 1)] = d;
                totalLength += d;
                prev = curr;
            }
        }
    }

    private float AdvanceByDistance(float currentT, float distance)
    {
        int samples = splineResolution;
        float remaining = distance;
        float newT = currentT;

        while (remaining > 0f)
        {
            int seg = Mathf.Clamp(Mathf.FloorToInt(newT), 0, NumSegments - 1);
            int sample = Mathf.Clamp(Mathf.FloorToInt((newT - seg) * samples), 0, samples - 1);

            float sampleLen = segmentLengths[seg * samples + sample];
            if (remaining <= sampleLen)
            {
                newT += (remaining / sampleLen) * (1f / samples);
                remaining = 0f;
            }
            else
            {
                remaining -= sampleLen;
                newT += 1f / samples;
            }

            if (newT >= NumSegments) { newT = NumSegments; break; }
        }

        return newT;
    }
    #endregion

    #region Gizmo
    private void OnDrawGizmosSelected()
    {
        if (!showSpline || !ValidatePoints()) return;

        int steps = splineResolution * NumSegments;
        Gizmos.color = Color.green;
        Vector3 prev = EvaluateSpline(0f);
        for (int i = 1; i <= steps; i++)
        {
            Vector3 curr = EvaluateSpline((float)i / steps * NumSegments);
            Gizmos.DrawLine(prev, curr);
            prev = curr;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(EvaluateSpline(0f), 0.6f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(EvaluateSpline(NumSegments - 0.001f), 0.6f);

        foreach (var cp in controlPoints)
        {
            if (cp == null) continue;
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(cp.position, 0.3f);
        }

        if (!Application.isPlaying) return;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(trackerPosition, 0.6f);

        float tTarget = Mathf.Min(AdvanceByDistance(t, lookahead), NumSegments - 0.001f);
        Vector3 ahead = EvaluateSpline(tTarget);
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(ahead, 0.5f);
        Gizmos.DrawLine(trackerPosition, ahead);

        Gizmos.color = new Color(1f, 0.4f, 0f, 0.3f);
        Gizmos.DrawWireCube(EvaluateSpline(0f), spawnScatter * 2f);
    }
    #endregion
}