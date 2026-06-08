using System.Collections;
using UnityEngine;

/// <summary>
/// Fait suivre au flock une spline Catmull-Rom.
/// 
/// Mode Loop :
///   - Le tracker avance jusqu'à la fin de la spline
///   - Après un délai (resetDelay), chaque oiseau est téléporté au début
///     avec un offset aléatoire pour que le reset soit invisible
///   - Le tracker repart depuis le début → boucle infinie
/// </summary>
public class SplineFlockController : MonoBehaviour
{
    [Header("Références")]
    public FlockManager flockManager;

    [Header("Spline")]
    [Tooltip("Points de contrôle Catmull-Rom (minimum 4)")]
    public Transform[] controlPoints;

    [Header("Suivi")]
    public float trackerSpeed = 6f;
    [Tooltip("Distance de lookahead — le flock vise un point en avance")]
    public float lookahead = 8f;
    [Range(0.5f, 10f)] public float turnSpeed = 2f;

    [Header("Boucle")]
    [Tooltip("Active le mode boucle (TP des oiseaux à la fin)")]
    public bool loopMode = true;
    [Tooltip("Délai après la fin de la spline avant de TP les oiseaux")]
    public float resetDelay = 1.5f;
    [Tooltip("Rayon d'étalement aléatoire au spawn du TP")]
    public Vector3 spawnScatter = new Vector3(6f, 2f, 4f);

    [Header("Debug")]
    public bool showSpline = true;
    public int splineResolution = 50;

    // ── État ──────────────────────────────────────────────────────────────
    private float t = 0f;
    private float totalLength;
    private float[] segmentLengths;

    private bool reachedEnd = false;
    private bool isResetting = false;

    // Exposé au FlockManager
    [HideInInspector] public Vector3 desiredDirection;
    [HideInInspector] public bool hasDesiredDirection;
    [HideInInspector] public Vector3 trackerPosition;

    // Nombre de segments actifs (jamais en boucle spline — on TP à la place)
    private int NumSegments => controlPoints.Length - 3;

    // ─────────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (!ValidatePoints()) return;
        PrecomputeLength();
        trackerPosition = EvaluateSpline(0f);
    }

    private void Update()
    {
        if (!ValidatePoints() || isResetting) { hasDesiredDirection = false; return; }

        // ── Avancer le tracker ──────────────────────────────────────────
        if (!reachedEnd)
        {
            t = AdvanceByDistance(t, trackerSpeed * Time.deltaTime);
            trackerPosition = EvaluateSpline(t);

            // Fin de spline détectée
            if (loopMode && t >= NumSegments - 0.01f)
            {
                reachedEnd = true;
                StartCoroutine(ResetRoutine());
            }
        }

        // ── Direction vers le lookahead ────────────────────────────────
        float tTarget = Mathf.Min(AdvanceByDistance(t, lookahead), NumSegments - 0.001f);
        Vector3 target = EvaluateSpline(tTarget);
        Vector3 desired = (target - trackerPosition).normalized;

        if (desired == Vector3.zero) { hasDesiredDirection = false; return; }

        desiredDirection = desired;
        hasDesiredDirection = true;
    }

    // ── Reset / TP ────────────────────────────────────────────────────────

    private IEnumerator ResetRoutine()
    {
        isResetting = true;

        // Délai pendant lequel les oiseaux continuent sur leur lancée
        yield return new WaitForSeconds(resetDelay);

        // Position de départ de la spline
        Vector3 splineStart = EvaluateSpline(0f);

        // TP de chaque oiseau avec scatter aléatoire
        for (int i = 0; i < flockManager.BirdCount; i++)
        {
            Vector3 scatter = new Vector3(
                Random.Range(-spawnScatter.x, spawnScatter.x),
                Random.Range(-spawnScatter.y, spawnScatter.y),
                Random.Range(-spawnScatter.z, spawnScatter.z)
            );
            flockManager.TeleportBird(i, splineStart + scatter);
        }

        // Reset du tracker
        t = 0f;
        reachedEnd = false;
        trackerPosition = EvaluateSpline(0f);

        // Court délai pour laisser le bounds se repositionner avant de relancer
        yield return new WaitForSeconds(0.1f);
        isResetting = false;
    }

    // ── Spline Catmull-Rom ────────────────────────────────────────────────

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

    // ── Arc-length ────────────────────────────────────────────────────────

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

    // ── Utils ─────────────────────────────────────────────────────────────

    private bool ValidatePoints() => controlPoints != null && controlPoints.Length >= 4;

    // ── Gizmos ────────────────────────────────────────────────────────────

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

        // Start / End marqués
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

        // Zone de spawn TP
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.3f);
        Gizmos.DrawWireCube(EvaluateSpline(0f), spawnScatter * 2f);
    }
}