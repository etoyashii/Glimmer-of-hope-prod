using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// FlockManager optimisé — Jobs + Burst + Spatial Hashing
/// Prérequises : packages "Unity.Burst" et "Unity.Collections" installés
/// (Window > Package Manager > Unity Registry)
/// </summary>
public class FlockManager : MonoBehaviour
{
    [Header("Prefab & Spawn")]
    public GameObject birdPrefab;
    public int birdCount = 80;
    public Vector3 spawnArea = new Vector3(20f, 5f, 20f);

    [Header("Direction du flux")]
    public Vector3 flockDirection = Vector3.forward;
    [Range(0f, 5f)] public float directionWeight = 1.5f;

    [Header("Vitesse")]
    public float minSpeed = 5f;
    public float maxSpeed = 12f;

    [Header("Boids")]
    public float neighborRadius = 5f;
    public float separationDistance = 1.8f;
    [Range(0f, 5f)] public float cohesionWeight = 1f;
    [Range(0f, 5f)] public float alignmentWeight = 1.5f;
    [Range(0f, 5f)] public float separationWeight = 2f;

    [Header("Limites")]
    public bool useBounds = true;
    public float boundsRadius = 40f;
    [Range(0f, 10f)] public float boundsWeight = 3f;

    [Header("Turbulence")]
    [Range(0f, 2f)] public float turbulence = 0.3f;

    [Header("Direction Override")]
    [Tooltip("Assigne SplineFlockController ici — sa direction prend le dessus sur flockDirection")]
    public SplineFlockController directionOverride;
    [Range(0.5f, 10f)] public float turnSpeed = 2f;

    // ── Données natives (partagées avec le Job) ───────────────────────────
    private NativeArray<float3> positions;
    private NativeArray<float3> velocities;
    private NativeArray<float3> newVelocities;

    // Spatial hash
    private NativeParallelMultiHashMap<int, int> spatialMap;

    private Transform[] birdTransforms;
    private JobHandle jobHandle;
    private bool jobScheduled;

    // ─────────────────────────────────────────────────────────────────────

    private void Start()
    {
        flockDirection = flockDirection.normalized;
        InitBirds();
    }

    private void InitBirds()
    {
        positions = new NativeArray<float3>(birdCount, Allocator.Persistent);
        velocities = new NativeArray<float3>(birdCount, Allocator.Persistent);
        newVelocities = new NativeArray<float3>(birdCount, Allocator.Persistent);
        spatialMap = new NativeParallelMultiHashMap<int, int>(birdCount * 8, Allocator.Persistent);

        birdTransforms = new Transform[birdCount];

        for (int i = 0; i < birdCount; i++)
        {
            Vector3 pos = transform.position + new Vector3(
                UnityEngine.Random.Range(-spawnArea.x / 2f, spawnArea.x / 2f),
                UnityEngine.Random.Range(-spawnArea.y / 2f, spawnArea.y / 2f),
                UnityEngine.Random.Range(-spawnArea.z / 2f, spawnArea.z / 2f)
            );

            GameObject go = Instantiate(birdPrefab, pos, UnityEngine.Random.rotation, transform);
            birdTransforms[i] = go.transform;

            positions[i] = pos;
            velocities[i] = math.normalize((float3)flockDirection)
                            * UnityEngine.Random.Range(minSpeed, maxSpeed);
        }
    }

    private void LateUpdate()
    {
        // 1. Compléter le job de la frame précédente
        if (jobScheduled)
        {
            jobHandle.Complete();
            jobScheduled = false;

            // Appliquer les nouvelles vélocités + déplacer les transforms
            for (int i = 0; i < birdCount; i++)
            {
                velocities[i] = newVelocities[i];
                Vector3 vel = velocities[i];
                positions[i] = (float3)(birdTransforms[i].position + vel * Time.deltaTime);
                birdTransforms[i].position = positions[i];

                if (!vel.Equals(Vector3.zero))
                    birdTransforms[i].rotation = Quaternion.Slerp(
                        birdTransforms[i].rotation,
                        Quaternion.LookRotation(vel),
                        Time.deltaTime * 8f
                    );
            }
        }

        // Direction override depuis SplineFlockController (lu APRES son Update)
        if (directionOverride != null && directionOverride.hasDesiredDirection)
        {
            flockDirection = Vector3.Slerp(
                flockDirection,
                directionOverride.desiredDirection,
                Time.deltaTime * turnSpeed
            ).normalized;
        }

        // 2. Rebuild de la spatial hash map
        spatialMap.Clear();
        float cellSize = neighborRadius;
        for (int i = 0; i < birdCount; i++)
        {
            int hash = SpatialHash(positions[i], cellSize);
            spatialMap.Add(hash, i);
        }

        // 3. Scheduler le nouveau job (sera calculé en parallèle)
        var job = new FlockJob
        {
            positions = positions,
            velocities = velocities,
            newVelocities = newVelocities,
            spatialMap = spatialMap,
            cellSize = cellSize,
            flockDirection = (float3)flockDirection.normalized,
            directionWeight = directionWeight,
            minSpeed = minSpeed,
            maxSpeed = maxSpeed,
            neighborRadius = neighborRadius,
            separationDist = separationDistance,
            cohesionWeight = cohesionWeight,
            alignmentWeight = alignmentWeight,
            separationWeight = separationWeight,
            useBounds = useBounds ? 1 : 0,
            boundsCenter = GetBoundsCenter(),
            boundsRadius = boundsRadius,
            boundsWeight = boundsWeight,
            turbulence = turbulence,
            deltaTime = Time.deltaTime,
            time = Time.time,
        };

        // IJobParallelFor : chaque oiseau traité sur un thread séparé
        jobHandle = job.Schedule(birdCount, 8);
        jobScheduled = true;
    }

    // ── API publique ─────────────────────────────────────────────────────
    public int BirdCount => birdCount;

    /// Téléporte un oiseau à une nouvelle position (appelé par SplineFlockController au reset)
    public void TeleportBird(int index, Vector3 worldPos)
    {
        if (index < 0 || index >= birdCount) return;

        // Le job tourne peut-être encore — on doit le compléter avant d'écrire
        if (jobScheduled)
        {
            jobHandle.Complete();
            jobScheduled = false;
        }

        positions[index] = (Unity.Mathematics.float3)worldPos;
        birdTransforms[index].position = worldPos;
        velocities[index] = (Unity.Mathematics.float3)(flockDirection.normalized * ((minSpeed + maxSpeed) * 0.5f));
    }

    private Vector3 _boundsCenter;

    /// Centre du bounds : suit le tracker de la spline si disponible, sinon position du FlockManager
    private float3 GetBoundsCenter()
    {
        if (directionOverride != null && directionOverride.hasDesiredDirection)
            return (float3)directionOverride.trackerPosition;
        return (float3)transform.position;
    }

    private void OnDestroy()
    {
        if (jobScheduled) jobHandle.Complete();
        positions.Dispose();
        velocities.Dispose();
        newVelocities.Dispose();
        spatialMap.Dispose();
    }

    // Hash 3D → int (spatial hashing classique)
    private static int SpatialHash(float3 pos, float cellSize)
    {
        int x = (int)math.floor(pos.x / cellSize);
        int y = (int)math.floor(pos.y / cellSize);
        int z = (int)math.floor(pos.z / cellSize);
        return x * 73856093 ^ y * 19349663 ^ z * 83492791;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
        Gizmos.DrawWireCube(transform.position, spawnArea);
        if (useBounds)
        {
            Vector3 bc = directionOverride != null && directionOverride.hasDesiredDirection
                ? directionOverride.trackerPosition
                : transform.position;
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
            Gizmos.DrawWireSphere(bc, boundsRadius);
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, flockDirection.normalized * 5f);
    }

    // ═════════════════════════════════════════════════════════════════════
    // JOB — tourne entièrement hors du main thread, compilé en assembleur
    // natif par Burst. Zéro garbage, zéro allocation.
    // ═════════════════════════════════════════════════════════════════════
    [BurstCompile]
    private struct FlockJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> positions;
        [ReadOnly] public NativeArray<float3> velocities;
        [WriteOnly] public NativeArray<float3> newVelocities;

        [ReadOnly] public NativeParallelMultiHashMap<int, int> spatialMap;
        public float cellSize;

        public float3 flockDirection;
        public float directionWeight;
        public float minSpeed, maxSpeed;
        public float neighborRadius, separationDist;
        public float cohesionWeight, alignmentWeight, separationWeight;
        public int useBounds;
        public float3 boundsCenter;
        public float boundsRadius, boundsWeight;
        public float turbulence;
        public float deltaTime, time;

        public void Execute(int i)
        {
            float3 pos = positions[i];
            float3 vel = velocities[i];

            float3 cohesion = float3.zero;
            float3 alignment = float3.zero;
            float3 separation = float3.zero;
            int neighbors = 0;

            // Chercher uniquement dans les cellules voisines (3x3x3)
            int cx = (int)math.floor(pos.x / cellSize);
            int cy = (int)math.floor(pos.y / cellSize);
            int cz = (int)math.floor(pos.z / cellSize);

            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        int hash = (cx + dx) * 73856093
                                 ^ (cy + dy) * 19349663
                                 ^ (cz + dz) * 83492791;

                        if (!spatialMap.TryGetFirstValue(hash, out int j, out var it)) continue;
                        do
                        {
                            if (j == i) continue;
                            float dist = math.distance(pos, positions[j]);
                            if (dist > neighborRadius) continue;

                            cohesion += positions[j];
                            alignment += velocities[j];
                            neighbors++;

                            if (dist < separationDist && dist > 0.001f)
                                separation += (pos - positions[j]) / dist;

                        } while (spatialMap.TryGetNextValue(out j, ref it));
                    }

            if (neighbors > 0)
            {
                cohesion = math.normalize(cohesion / neighbors - pos);
                alignment = math.normalize(alignment);
            }

            // Retour dans les limites
            float3 bounds = float3.zero;
            if (useBounds == 1)
            {
                float d = math.distance(pos, boundsCenter);
                float threshold = boundsRadius * 0.7f;
                if (d > threshold)
                {
                    bounds = math.normalize(boundsCenter - pos)
                           * boundsWeight
                           * math.unlerp(threshold, boundsRadius, d);
                }
            }

            // Turbulence via noise pseudo-aléatoire (pas de Random dans Burst)
            float3 noise = new float3(
                math.sin(time * 1.3f + pos.x * 0.7f),
                math.sin(time * 0.9f + pos.y * 1.1f),
                math.sin(time * 1.7f + pos.z * 0.5f)
            ) * turbulence;

            // Somme des forces
            float3 force = cohesion * cohesionWeight
                         + alignment * alignmentWeight
                         + separation * separationWeight
                         + flockDirection * directionWeight
                         + bounds
                         + noise;

            float3 newVel = vel + force * deltaTime;

            // Clamp speed
            float speed = math.length(newVel);
            if (speed > maxSpeed) newVel = newVel / speed * maxSpeed;
            if (speed < minSpeed) newVel = newVel / speed * minSpeed;

            newVelocities[i] = newVel;
        }
    }
}