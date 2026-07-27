using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Owns all bird data (positions, velocities as NativeArrays).
/// Each LateUpdate: completes previous job → applies velocities to Transforms
/// → rebuilds spatial hash map → schedules new FlockJob.
/// FlockJob (IJobParallelFor + Burst) computes Boids forces + direction + bounds
/// entirely off the main thread.
/// Reads desired direction from SplineFlockController before scheduling.
/// Exposes TeleportBird() with job safety for loop resets.
/// Requires: Unity.Burst, Unity.Collections, Unity.Jobs, Unity.Mathematics
/// </summary>
namespace GlimmerOfHope.Gameplay
{
    public class FlockManager : MonoBehaviour
    {
        #region Inspector propreties
        [Header("Prefab & Spawn")]
        [SerializeField]
        private GameObject birdPrefab;
        [SerializeField]
        private int birdCount = 80;
        [SerializeField]
        private Vector3 spawnArea = new Vector3(20f, 5f, 20f);

        [Header("Flux direction")]
        [SerializeField]
        private Vector3 flockDirection = Vector3.forward;
        [Range(0f, 5f)] public float directionWeight = 1.5f;

        [Header("Speed")]
        [SerializeField]
        private float minSpeed = 5f;
        [SerializeField]
        private float maxSpeed = 12f;

        [Header("Boids")]
        [SerializeField]
        private float neighborRadius = 5f;
        [SerializeField]
        private float separationDistance = 1.8f;
        [Range(0f, 5f)]
        [SerializeField]
        private float cohesionWeight = 1f;
        [Range(0f, 5f)]
        [SerializeField]
        private float alignmentWeight = 1.5f;
        [Range(0f, 5f)]
        [SerializeField]
        private float separationWeight = 2f;

        [Header("Limites")]
        [SerializeField]
        private bool useBounds = true;
        [SerializeField]
        private float boundsRadius = 40f;
        [Range(0f, 10f)]
        [SerializeField]
        private float boundsWeight = 3f;

        [Header("Turbulence")]
        [Range(0f, 2f)]
        [SerializeField]
        private float turbulence = 0.3f;

        [Header("Direction Override")]
        [Tooltip("Asign SplineFlockController, it would override the direction")]
        [SerializeField]
        private SplineFlockController directionOverride;
        [Range(0.5f, 10f)]
        [SerializeField]
        private float turnSpeed = 2f;
        #endregion

        #region Propreties
        private NativeArray<float3> positions;
        private NativeArray<float3> velocities;
        private NativeArray<float3> newVelocities;

        private NativeParallelMultiHashMap<int, int> spatialMap;

        private Transform[] birdTransforms;
        private JobHandle jobHandle;
        private bool jobScheduled;
        #endregion

        #region Methods
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
            if (jobScheduled)
            {
                jobHandle.Complete();
                jobScheduled = false;

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

            if (directionOverride != null && directionOverride.hasDesiredDirection)
            {
                flockDirection = Vector3.Slerp(
                    flockDirection,
                    directionOverride.desiredDirection,
                    Time.deltaTime * turnSpeed
                ).normalized;
            }

            spatialMap.Clear();
            float cellSize = neighborRadius;
            for (int i = 0; i < birdCount; i++)
            {
                int hash = SpatialHash(positions[i], cellSize);
                spatialMap.Add(hash, i);
            }

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

            jobHandle = job.Schedule(birdCount, 8);
            jobScheduled = true;
        }

        public int BirdCount => birdCount;

        public void TeleportBird(int index, Vector3 worldPos)
        {
            if (index < 0 || index >= birdCount) return;

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

        private static int SpatialHash(float3 pos, float cellSize)
        {
            int x = (int)math.floor(pos.x / cellSize);
            int y = (int)math.floor(pos.y / cellSize);
            int z = (int)math.floor(pos.z / cellSize);
            return x * 73856093 ^ y * 19349663 ^ z * 83492791;
        }
        #endregion

        #region Gizmos
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
        #endregion

        #region FlockJob
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

                float3 noise = new float3(
                    math.sin(time * 1.3f + pos.x * 0.7f),
                    math.sin(time * 0.9f + pos.y * 1.1f),
                    math.sin(time * 1.7f + pos.z * 0.5f)
                ) * turbulence;

                float3 force = cohesion * cohesionWeight
                             + alignment * alignmentWeight
                             + separation * separationWeight
                             + flockDirection * directionWeight
                             + bounds
                             + noise;

                float3 newVel = vel + force * deltaTime;

                float speed = math.length(newVel);
                if (speed > maxSpeed) newVel = newVel / speed * maxSpeed;
                if (speed < minSpeed) newVel = newVel / speed * minSpeed;

                newVelocities[i] = newVel;
            }
        }
        #endregion
    }
}