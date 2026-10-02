#ifndef WORLD_INSTANCING_INDIRECT_INCLUDED
#define WORLD_INSTANCING_INDIRECT_INCLUDED

#define WORLD_INSTANCING_INDIRECT_CONTRACT_VERSION 4
#define WORLD_INSTANCING_INDIRECT_LAYOUT_VERSION 4
#define WORLD_INSTANCING_INDIRECT_LAYOUT_HASH 12012013
#define WORLD_INSTANCING_INDIRECT_CONTRACT_VERSION_PROPERTY _WorldInstancingIndirectContractVersion
#define WORLD_INSTANCING_INDIRECT_LAYOUT_VERSION_PROPERTY _WorldInstancingIndirectLayoutVersion
#define WORLD_INSTANCING_INDIRECT_LAYOUT_HASH_PROPERTY _WorldInstancingIndirectLayoutHash
#define WORLD_INSTANCING_INDIRECT_SETUP_FUNCTION WorldInstancingIndirect_Setup

#ifndef HAS_INSTANCE_STATE_BUFFER
#error HAS_INSTANCE_STATE_BUFFER must be defined by the generated indirect shader variant.
#endif

#ifndef HAS_CHUNK_STATE_BUFFER
#error HAS_CHUNK_STATE_BUFFER must be defined by the generated indirect shader variant.
#endif

#if HAS_INSTANCE_STATE_BUFFER != 1
#error HAS_INSTANCE_STATE_BUFFER must be 1 for indirect chunk-state layout v4.
#endif

#if HAS_CHUNK_STATE_BUFFER != 1
#error HAS_CHUNK_STATE_BUFFER must be 1 for indirect chunk-state layout v4.
#endif

#define WORLD_INSTANCING_INSTANCE_STATE_RELEASED 0u
#define WORLD_INSTANCING_INSTANCE_STATE_ACTIVE 1u
#define WORLD_INSTANCING_INSTANCE_STATE_INACTIVE 2u
#define WORLD_INSTANCING_INSTANCE_STATE_DEAD_PENDING_COMPACTION 3u

#if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED) || defined(PROCEDURAL_INSTANCING_ON)
#define WORLD_INSTANCING_INDIRECT_PROCEDURAL_ENABLED 1
#else
#define WORLD_INSTANCING_INDIRECT_PROCEDURAL_ENABLED 0
#endif

float _WorldInstancingIndirectContractVersion;
float _WorldInstancingIndirectLayoutVersion;
float _WorldInstancingIndirectLayoutHash;

StructuredBuffer<float4x4> _InstanceObjectToWorld;
StructuredBuffer<float4x4> _InstanceWorldToObject;
StructuredBuffer<float4x4> _PrevInstanceObjectToWorld;
StructuredBuffer<float4x4> _PrevInstanceWorldToObject;
StructuredBuffer<uint> _VisibleInstanceIndices;
StructuredBuffer<float> _VisibleLodFades;
StructuredBuffer<float4> _InstanceLightmapST;
StructuredBuffer<uint> _InstanceChunkKeys;
StructuredBuffer<uint> _InstanceLifecycleStates;
StructuredBuffer<uint> _ChunkStates;
uint _ChunkStateCount;

float4x4 _ChildLocalToWorld;
float4x4 _ChildWorldToLocal;

float4x4 WorldInstancingIndirect_GetIdentityMatrix()
{
    return float4x4(
        1.0, 0.0, 0.0, 0.0,
        0.0, 1.0, 0.0, 0.0,
        0.0, 0.0, 1.0, 0.0,
        0.0, 0.0, 0.0, 1.0
    );
}

float4x4 WorldInstancingIndirect_GetFallbackObjectToWorld()
{
#if defined(UNITY_MATRIX_M)
    return UNITY_MATRIX_M;
#else
    return WorldInstancingIndirect_GetIdentityMatrix();
#endif
}

float4x4 WorldInstancingIndirect_GetFallbackWorldToObject()
{
#if defined(UNITY_MATRIX_I_M)
    return UNITY_MATRIX_I_M;
#else
    return WorldInstancingIndirect_GetIdentityMatrix();
#endif
}

uint WorldInstancingIndirect_GetVisibleInstanceId()
{
#if WORLD_INSTANCING_INDIRECT_PROCEDURAL_ENABLED
    return unity_InstanceID;
#else
    return 0u;
#endif
}

uint WorldInstancingIndirect_GetInstanceIndex(uint visibleInstanceId)
{
#if WORLD_INSTANCING_INDIRECT_PROCEDURAL_ENABLED
    return _VisibleInstanceIndices[visibleInstanceId];
#else
    return visibleInstanceId;
#endif
}

uint WorldInstancingIndirect_GetInstanceIndexFromUnityInstanceId(uint unityInstanceId)
{
    return WorldInstancingIndirect_GetInstanceIndex(unityInstanceId);
}

uint WorldInstancingIndirect_GetCurrentInstanceIndex()
{
    return WorldInstancingIndirect_GetInstanceIndex(WorldInstancingIndirect_GetVisibleInstanceId());
}

uint WorldInstancingIndirect_GetInstanceChunkKey(uint instanceIndex)
{
    return _InstanceChunkKeys[instanceIndex];
}

uint WorldInstancingIndirect_GetCurrentInstanceChunkKey()
{
    return WorldInstancingIndirect_GetInstanceChunkKey(WorldInstancingIndirect_GetCurrentInstanceIndex());
}

uint WorldInstancingIndirect_GetInstanceLifecycleState(uint instanceIndex)
{
    return _InstanceLifecycleStates[instanceIndex];
}

uint WorldInstancingIndirect_GetCurrentInstanceLifecycleState()
{
    return WorldInstancingIndirect_GetInstanceLifecycleState(WorldInstancingIndirect_GetCurrentInstanceIndex());
}

bool WorldInstancingIndirect_IsInstanceActive(uint instanceIndex)
{
    return WorldInstancingIndirect_GetInstanceLifecycleState(instanceIndex) == WORLD_INSTANCING_INSTANCE_STATE_ACTIVE;
}

bool WorldInstancingIndirect_IsCurrentInstanceActive()
{
    return WorldInstancingIndirect_IsInstanceActive(WorldInstancingIndirect_GetCurrentInstanceIndex());
}

uint WorldInstancingIndirect_GetChunkState(uint chunkKey)
{
    if (chunkKey >= _ChunkStateCount)
        return 0u;

    return _ChunkStates[chunkKey];
}

uint WorldInstancingIndirect_GetInstanceChunkState(uint instanceIndex)
{
    return WorldInstancingIndirect_GetChunkState(WorldInstancingIndirect_GetInstanceChunkKey(instanceIndex));
}

uint WorldInstancingIndirect_GetCurrentInstanceChunkState()
{
    return WorldInstancingIndirect_GetInstanceChunkState(WorldInstancingIndirect_GetCurrentInstanceIndex());
}

bool WorldInstancingIndirect_IsInstanceChunkActive(uint instanceIndex)
{
    return WorldInstancingIndirect_GetInstanceChunkState(instanceIndex) != 0u;
}

bool WorldInstancingIndirect_IsCurrentInstanceChunkActive()
{
    return WorldInstancingIndirect_IsInstanceChunkActive(WorldInstancingIndirect_GetCurrentInstanceIndex());
}

bool WorldInstancingIndirect_IsInstanceRenderable(uint instanceIndex)
{
    return WorldInstancingIndirect_IsInstanceActive(instanceIndex) && WorldInstancingIndirect_IsInstanceChunkActive(instanceIndex);
}

bool WorldInstancingIndirect_IsCurrentInstanceRenderable()
{
    return WorldInstancingIndirect_IsInstanceRenderable(WorldInstancingIndirect_GetCurrentInstanceIndex());
}

float WorldInstancingIndirect_GetLodFade(uint visibleInstanceId)
{
#if WORLD_INSTANCING_INDIRECT_PROCEDURAL_ENABLED
    return _VisibleLodFades[visibleInstanceId];
#else
    return 1.0;
#endif
}

float WorldInstancingIndirect_GetCurrentLodFade()
{
    return WorldInstancingIndirect_GetLodFade(WorldInstancingIndirect_GetVisibleInstanceId());
}

float4 WorldInstancingIndirect_GetLightmapST(uint instanceIndex)
{
    return _InstanceLightmapST[instanceIndex];
}

float4 WorldInstancingIndirect_GetCurrentLightmapST()
{
    return WorldInstancingIndirect_GetLightmapST(WorldInstancingIndirect_GetCurrentInstanceIndex());
}

float2 WorldInstancingIndirect_TransformLightmapUV(float2 uv, uint instanceIndex)
{
    float4 lightmapST = WorldInstancingIndirect_GetLightmapST(instanceIndex);
    return uv * lightmapST.xy + lightmapST.zw;
}

float2 WorldInstancingIndirect_TransformCurrentLightmapUV(float2 uv)
{
    return WorldInstancingIndirect_TransformLightmapUV(uv, WorldInstancingIndirect_GetCurrentInstanceIndex());
}

float4x4 WorldInstancingIndirect_GetObjectToWorld(uint instanceIndex)
{
#if WORLD_INSTANCING_INDIRECT_PROCEDURAL_ENABLED
    return mul(_InstanceObjectToWorld[instanceIndex], _ChildLocalToWorld);
#else
    return WorldInstancingIndirect_GetFallbackObjectToWorld();
#endif
}

float4x4 WorldInstancingIndirect_GetWorldToObject(uint instanceIndex)
{
#if WORLD_INSTANCING_INDIRECT_PROCEDURAL_ENABLED
    return mul(_ChildWorldToLocal, _InstanceWorldToObject[instanceIndex]);
#else
    return WorldInstancingIndirect_GetFallbackWorldToObject();
#endif
}

float4x4 WorldInstancingIndirect_GetPreviousObjectToWorld(uint instanceIndex)
{
#if WORLD_INSTANCING_INDIRECT_PROCEDURAL_ENABLED
    return mul(_PrevInstanceObjectToWorld[instanceIndex], _ChildLocalToWorld);
#else
    return WorldInstancingIndirect_GetFallbackObjectToWorld();
#endif
}

float4x4 WorldInstancingIndirect_GetPreviousWorldToObject(uint instanceIndex)
{
#if WORLD_INSTANCING_INDIRECT_PROCEDURAL_ENABLED
    return mul(_ChildWorldToLocal, _PrevInstanceWorldToObject[instanceIndex]);
#else
    return WorldInstancingIndirect_GetFallbackWorldToObject();
#endif
}

float4x4 WorldInstancingIndirect_GetCurrentObjectToWorld()
{
    return WorldInstancingIndirect_GetObjectToWorld(WorldInstancingIndirect_GetCurrentInstanceIndex());
}

float4x4 WorldInstancingIndirect_GetCurrentWorldToObject()
{
    return WorldInstancingIndirect_GetWorldToObject(WorldInstancingIndirect_GetCurrentInstanceIndex());
}

float3 WorldInstancingIndirect_TransformObjectToWorldPosition(float3 positionOS, uint instanceIndex)
{
    return mul(WorldInstancingIndirect_GetObjectToWorld(instanceIndex), float4(positionOS, 1.0)).xyz;
}

float WorldInstancingIndirect_GetInstanceUniformScale(
    uint instanceIndex)
{
    float3 center =
        WorldInstancingIndirect_TransformObjectToWorldPosition(
            float3(0.0, 0.0, 0.0),
            instanceIndex);
    float sx = length(
        WorldInstancingIndirect_TransformObjectToWorldPosition(
            float3(1.0, 0.0, 0.0),
            instanceIndex) - center);
    float sy = length(
        WorldInstancingIndirect_TransformObjectToWorldPosition(
            float3(0.0, 1.0, 0.0),
            instanceIndex) - center);
    float sz = length(
        WorldInstancingIndirect_TransformObjectToWorldPosition(
            float3(0.0, 0.0, 1.0),
            instanceIndex) - center);
    return max(max(sx, sy), max(sz, 1e-5));
}

float3 WorldInstancingIndirect_BuildImpostorPositionWS(
    float2 quadPosition,
    float2 impostorSize,
    float2 impostorPivot,
    int facingMode,
    uint instanceIndex)
{
    float3 center =
        WorldInstancingIndirect_TransformObjectToWorldPosition(
            float3(0.0, 0.0, 0.0),
            instanceIndex);
    float3 worldUp = float3(0.0, 1.0, 0.0);
    float3 toCamera = _WorldSpaceCameraPos.xyz - center;
    float3 forward = facingMode == 0
        ? float3(toCamera.x, 0.0, toCamera.z)
        : toCamera;
    float forwardLength = length(forward);
    forward = forwardLength > 1e-5
        ? forward / forwardLength
        : float3(0.0, 0.0, 1.0);
    float3 right = cross(worldUp, forward);
    float rightLength = length(right);
    right = rightLength > 1e-5
        ? right / rightLength
        : float3(1.0, 0.0, 0.0);
    float3 up = facingMode == 0
        ? worldUp
        : normalize(cross(forward, right));
    float scale =
        WorldInstancingIndirect_GetInstanceUniformScale(
            instanceIndex);
    float2 pivoted =
        quadPosition + 0.5 - saturate(impostorPivot);
    float2 offset =
        pivoted * max(impostorSize, 1e-5) * scale;
    return center + right * offset.x + up * offset.y;
}

void WorldInstancingIndirect_ApplyImpostorVisibility(
    float coverage)
{
    // Switch the entire impostor at once. Screen-space dithering produces a
    // persistent grain pattern on dense vegetation and is especially
    // distracting in motion.
    clip(saturate(coverage) - 0.5);
}

// Preserve source compatibility for shaders generated against contract v4.
// The legacy entry point now delegates to the stable, grain-free cutoff.
void WorldInstancingIndirect_ApplyImpostorDither(
    float4 positionCS,
    float coverage)
{
    WorldInstancingIndirect_ApplyImpostorVisibility(coverage);
}

float3 WorldInstancingIndirect_TransformCurrentObjectToWorldPosition(float3 positionOS)
{
    return WorldInstancingIndirect_TransformObjectToWorldPosition(positionOS, WorldInstancingIndirect_GetCurrentInstanceIndex());
}

float3 WorldInstancingIndirect_TransformPreviousObjectToWorldPosition(float3 positionOS, uint instanceIndex)
{
    return mul(WorldInstancingIndirect_GetPreviousObjectToWorld(instanceIndex), float4(positionOS, 1.0)).xyz;
}

float3 WorldInstancingIndirect_TransformCurrentPreviousObjectToWorldPosition(float3 positionOS)
{
    return WorldInstancingIndirect_TransformPreviousObjectToWorldPosition(positionOS, WorldInstancingIndirect_GetCurrentInstanceIndex());
}

float3 WorldInstancingIndirect_TransformObjectToWorldDirection(float3 directionOS, uint instanceIndex)
{
    return normalize(mul((float3x3) WorldInstancingIndirect_GetObjectToWorld(instanceIndex), directionOS));
}

float3 WorldInstancingIndirect_TransformCurrentObjectToWorldDirection(float3 directionOS)
{
    return WorldInstancingIndirect_TransformObjectToWorldDirection(directionOS, WorldInstancingIndirect_GetCurrentInstanceIndex());
}

float3 WorldInstancingIndirect_TransformPreviousObjectToWorldDirection(float3 directionOS, uint instanceIndex)
{
    return normalize(mul((float3x3) WorldInstancingIndirect_GetPreviousObjectToWorld(instanceIndex), directionOS));
}

float3 WorldInstancingIndirect_TransformCurrentPreviousObjectToWorldDirection(float3 directionOS)
{
    return WorldInstancingIndirect_TransformPreviousObjectToWorldDirection(directionOS, WorldInstancingIndirect_GetCurrentInstanceIndex());
}

float3 WorldInstancingIndirect_TransformObjectToWorldNormal(float3 normalOS, uint instanceIndex)
{
    return normalize(mul(normalOS, (float3x3) WorldInstancingIndirect_GetWorldToObject(instanceIndex)));
}

float3 WorldInstancingIndirect_TransformCurrentObjectToWorldNormal(float3 normalOS)
{
    return WorldInstancingIndirect_TransformObjectToWorldNormal(normalOS, WorldInstancingIndirect_GetCurrentInstanceIndex());
}

float3 WorldInstancingIndirect_TransformPreviousObjectToWorldNormal(float3 normalOS, uint instanceIndex)
{
    return normalize(mul(normalOS, (float3x3) WorldInstancingIndirect_GetPreviousWorldToObject(instanceIndex)));
}

float3 WorldInstancingIndirect_TransformCurrentPreviousObjectToWorldNormal(float3 normalOS)
{
    return WorldInstancingIndirect_TransformPreviousObjectToWorldNormal(normalOS, WorldInstancingIndirect_GetCurrentInstanceIndex());
}

void WorldInstancingIndirect_GetMotionVectorWorldPositions(float3 positionOS, uint instanceIndex, out float3 currentPositionWS, out float3 previousPositionWS)
{
    currentPositionWS = WorldInstancingIndirect_TransformObjectToWorldPosition(positionOS, instanceIndex);
    previousPositionWS = WorldInstancingIndirect_TransformPreviousObjectToWorldPosition(positionOS, instanceIndex);
}

void WorldInstancingIndirect_GetCurrentMotionVectorWorldPositions(float3 positionOS, out float3 currentPositionWS, out float3 previousPositionWS)
{
    WorldInstancingIndirect_GetMotionVectorWorldPositions(positionOS, WorldInstancingIndirect_GetCurrentInstanceIndex(), currentPositionWS, previousPositionWS);
}

void WorldInstancingIndirect_ApplyUnityMatrices()
{
#if WORLD_INSTANCING_INDIRECT_PROCEDURAL_ENABLED
    uint instanceIndex = WorldInstancingIndirect_GetInstanceIndex(unity_InstanceID);
    unity_ObjectToWorld = WorldInstancingIndirect_GetObjectToWorld(instanceIndex);
    unity_WorldToObject = WorldInstancingIndirect_GetWorldToObject(instanceIndex);
#endif
}

void WorldInstancingIndirect_Setup()
{
    WorldInstancingIndirect_ApplyUnityMatrices();
}

#endif
