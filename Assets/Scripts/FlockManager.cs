using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class FlockManager : MonoBehaviour
{
    public static FlockManager FM;
    public Transform playerTransform;

    [Header("Orbit")]
    public Vector3 swimLimits = new Vector3(8f, 3f, 8f);
    [Range(0.5f, 5f)] public float orbitRadius = 3f;
    [Range(0.1f, 3f)] public float orbitAttractions = 1.2f;

    [Header("Follow")]
    public float forwardOffset = 10f;
    public float followSmooth = 6f;

    [Header("Separation")]
    public float separationDistance = 1.2f;
    public float separationStrength = 2f;

    [Header("Front Lock")]
    public float frontPushStrength = 8f;

    [Header("Fish Settings")]
    [Range(0.1f, 5f)] public float minSpeed = 1f;
    [Range(0.1f, 8f)] public float maxSpeed = 3f;
    [Range(1f, 10f)] public float rotationSpeed = 3f;

    [Header("Natural Motion")]
    [Range(0f, 1f)] public float verticalDamping = 0.7f;
    [Range(0f, 2f)] public float wanderStrength = 0.6f;
    [Range(0f, 1f)] public float speedVariation = 0.4f;

    Flock[] flockComponents;
    Transform[] fishTransforms;
    int fishCount;

    NativeArray<Vector3> positions;
    NativeArray<Vector3> velocities;
    NativeArray<float> orbitAngles;
    NativeArray<float> orbitSpeeds;
    NativeArray<float> phaseOffsets;

    NativeArray<Vector3> outPositions;
    NativeArray<Quaternion> outRotations;
    NativeArray<Vector3> outVelocities;

    JobHandle jobHandle;

    private Vector3 smoothedForward;
    private Vector3 smoothCenter;

    void Awake() => FM = this;

    void Start()
    {
        var pool = FlockPooler.Instance.pooledFish;
        fishCount = pool.Count;

        flockComponents = new Flock[fishCount];
        fishTransforms = new Transform[fishCount];

        positions = new NativeArray<Vector3>(fishCount, Allocator.Persistent);
        velocities = new NativeArray<Vector3>(fishCount, Allocator.Persistent);
        orbitAngles = new NativeArray<float>(fishCount, Allocator.Persistent);
        orbitSpeeds = new NativeArray<float>(fishCount, Allocator.Persistent);
        phaseOffsets = new NativeArray<float>(fishCount, Allocator.Persistent);

        outPositions = new NativeArray<Vector3>(fishCount, Allocator.Persistent);
        outRotations = new NativeArray<Quaternion>(fishCount, Allocator.Persistent);
        outVelocities = new NativeArray<Vector3>(fishCount, Allocator.Persistent);

        smoothedForward = Vector3.ProjectOnPlane(playerTransform.forward, Vector3.up).normalized;
        smoothCenter = playerTransform.position;

        for (int i = 0; i < fishCount; i++)
        {
            var fish = pool[i];

            float angle = (i / (float)fishCount) * Mathf.PI * 2f + Random.Range(-0.3f, 0.3f);
            float r = orbitRadius + Random.Range(-0.5f, 0.5f);
            float yOffset = Random.Range(-swimLimits.y, swimLimits.y);

            Vector3 spawnPos = playerTransform.position
                             + new Vector3(Mathf.Cos(angle) * r, yOffset, Mathf.Sin(angle) * r);

            fish.transform.position = spawnPos;
            fish.SetActive(true);

            fishTransforms[i] = fish.transform;
            flockComponents[i] = fish.GetComponent<Flock>();
            flockComponents[i].Init(i);

            Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)).normalized;
            float spd = Random.Range(minSpeed, maxSpeed);

            positions[i] = spawnPos;
            velocities[i] = tangent * spd;
            orbitAngles[i] = angle;
            orbitSpeeds[i] = (Random.value > 0.5f ? 1f : -1f) * Random.Range(0.2f, 0.5f);
            phaseOffsets[i] = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    void Update()
    {
        jobHandle.Complete();

        for (int i = 0; i < fishCount; i++)
        {
            fishTransforms[i].position = outPositions[i];
            fishTransforms[i].rotation = outRotations[i];
            positions[i] = outPositions[i];
            velocities[i] = outVelocities[i];
        }

        Vector3 targetForward = Vector3.ProjectOnPlane(playerTransform.forward, Vector3.up).normalized;
        smoothedForward = Vector3.Slerp(smoothedForward, targetForward, Time.deltaTime * 5f);

        Vector3 targetCenter = playerTransform.position + smoothedForward * forwardOffset;
        smoothCenter = Vector3.Lerp(smoothCenter, targetCenter, Time.deltaTime * followSmooth);

        var job = new FlockJob
        {
            playerPos = smoothCenter,
            playerForwardDir = smoothedForward,

            swimLimits = swimLimits,
            orbitRadius = orbitRadius,
            orbitAttractions = orbitAttractions,

            separationDistance = separationDistance,
            separationStrength = separationStrength,
            frontPushStrength = frontPushStrength,

            minSpeed = minSpeed,
            maxSpeed = maxSpeed,
            rotationSpeed = rotationSpeed,

            verticalDamping = verticalDamping,
            wanderStrength = wanderStrength,
            speedVariation = speedVariation,

            deltaTime = Time.deltaTime,
            time = Time.time,

            positions = positions,
            velocities = velocities,
            orbitAngles = orbitAngles,
            orbitSpeeds = orbitSpeeds,
            phaseOffsets = phaseOffsets,

            outPositions = outPositions,
            outRotations = outRotations,
            outVelocities = outVelocities,
        };

        jobHandle = job.Schedule(fishCount, 32);
    }

    void OnDestroy()
    {
        jobHandle.Complete();
        positions.Dispose();
        velocities.Dispose();
        orbitAngles.Dispose();
        orbitSpeeds.Dispose();
        phaseOffsets.Dispose();
        outPositions.Dispose();
        outRotations.Dispose();
        outVelocities.Dispose();
    }

    // ─── JOB ─────────────────────────────────────────────────────────────────
    [BurstCompile]
    struct FlockJob : IJobParallelFor
    {
        [ReadOnly] public Vector3 playerPos;
        [ReadOnly] public Vector3 playerForwardDir;
        [ReadOnly] public Vector3 swimLimits;

        [ReadOnly] public float orbitRadius, orbitAttractions;
        [ReadOnly] public float separationDistance, separationStrength;
        [ReadOnly] public float frontPushStrength;
        [ReadOnly] public float minSpeed, maxSpeed, rotationSpeed;
        [ReadOnly] public float verticalDamping, wanderStrength, speedVariation;
        [ReadOnly] public float deltaTime, time;

        [ReadOnly] public NativeArray<Vector3> positions;
        [ReadOnly] public NativeArray<Vector3> velocities;

        public NativeArray<float> orbitAngles;
        public NativeArray<float> orbitSpeeds;
        public NativeArray<float> phaseOffsets;

        public NativeArray<Vector3> outPositions;
        public NativeArray<Quaternion> outRotations;
        public NativeArray<Vector3> outVelocities;

        public void Execute(int i)
        {
            Vector3 pos = positions[i];
            Vector3 vel = velocities[i];
            float phase = phaseOffsets[i];

            // ── ORBIT ANGLE ───────────────────────────────────────────────────
            float angle = orbitAngles[i] + orbitSpeeds[i] * deltaTime;
            orbitAngles[i] = angle;

            Vector3 center = playerPos + Vector3.up * (swimLimits.y * 0.5f);
            Vector3 playerRight = Vector3.Cross(Vector3.up, playerForwardDir).normalized;

            float rWave = orbitRadius
                        + Mathf.Sin(time * 0.3f + phase * 0.7f) * orbitRadius * 0.25f;

            Vector3 orbitPos = center
                + playerForwardDir * (Mathf.Sin(angle) * rWave)
                + playerRight * (Mathf.Cos(angle) * rWave);

            float targetY = center.y
                + Mathf.Sin(time * 0.4f + phase) * swimLimits.y * 0.25f
                + Mathf.Sin(time * 0.17f + phase * 1.3f) * swimLimits.y * 0.1f;

            Vector3 orbitTarget = new Vector3(orbitPos.x, targetY, orbitPos.z);

            // ── ATTRACTION ───────────────────────────────────────────────────
            Vector3 toTarget = orbitTarget - pos;
            float distToTarget = toTarget.magnitude;

            Vector3 attraction = distToTarget > 0.01f
                ? toTarget.normalized * orbitAttractions * distToTarget
                : Vector3.zero;

            // ── FRONT LOCK ───────────────────────────────────────────────────
            Vector3 toFish = pos - playerPos;
            float forwardDot = Vector3.Dot(toFish, playerForwardDir);

            Vector3 frontPush = Vector3.zero;
            if (forwardDot < 0f)
                frontPush = playerForwardDir * (-forwardDot) * frontPushStrength;

            // ── SEPARATION ───────────────────────────────────────────────────
            Vector3 separation = Vector3.zero;
            for (int j = 0; j < positions.Length; j++)
            {
                if (j == i) continue;
                Vector3 diff = pos - positions[j];
                float dist = diff.magnitude;
                if (dist > 0f && dist < separationDistance)
                    separation += diff.normalized / dist;
            }
            separation *= separationStrength;

            // ── WANDER (double fréquence, V2) ─────────────────────────────────
            float wx = (Mathf.Sin(time * 1.1f + phase) * 0.7f
                      + Mathf.Sin(time * 0.37f + phase * 1.7f) * 0.3f) * wanderStrength;

            float wy = (Mathf.Sin(time * 0.6f + phase * 2.3f) * 0.7f
                      + Mathf.Sin(time * 0.19f + phase * 0.8f) * 0.3f) * wanderStrength * 0.2f;

            float wz = (Mathf.Cos(time * 0.9f + phase * 1.2f) * 0.7f
                      + Mathf.Cos(time * 0.28f + phase * 2.1f) * 0.3f) * wanderStrength;

            Vector3 wander = new Vector3(wx, wy, wz);

            // ── DESIRED DIRECTION ─────────────────────────────────────────────
            Vector3 desired = attraction + wander + separation + frontPush;

            // ── VELOCITY / DIRECTION ──────────────────────────────────────────
            float spd = vel.magnitude;
            if (spd < 0.001f) { vel = Vector3.forward; spd = minSpeed; }

            Vector3 dir = vel / spd;

            if (desired.sqrMagnitude > 0.001f)
                dir = Vector3.Slerp(dir, desired.normalized, rotationSpeed * deltaTime).normalized;

            // ── SPEED (lissée, V2) ────────────────────────────────────────────
            float spdPhase = time * 0.6f + phase;
            float t = 0.5f
                    + Mathf.Sin(spdPhase) * speedVariation * 0.5f
                    + Mathf.Sin(spdPhase * 0.41f) * speedVariation * 0.2f;

            float targetSpd = Mathf.Lerp(minSpeed, maxSpeed, Mathf.Clamp01(t));
            spd = Mathf.Lerp(spd, targetSpd, deltaTime * 2f);
            spd = Mathf.Clamp(spd, minSpeed, maxSpeed);

            // ── ROTATION (lissée, V2) ─────────────────────────────────────────
            Quaternion targetRot = Quaternion.LookRotation(dir);
            Quaternion currentRot = outRotations[i] == default ? targetRot : outRotations[i];
            Quaternion newRot = Quaternion.Slerp(currentRot, targetRot, rotationSpeed * deltaTime);

            Vector3 forward = newRot * Vector3.forward;
            pos += forward * spd * deltaTime;

            // ── PLANCHER ──────────────────────────────────────────────────────
            float minY = playerPos.y + 0.5f;
            if (pos.y < minY)
                pos.y = Mathf.Lerp(pos.y, minY, deltaTime * 5f);

            outPositions[i] = pos;
            outRotations[i] = newRot;
            outVelocities[i] = forward * spd;
        }
    }
}