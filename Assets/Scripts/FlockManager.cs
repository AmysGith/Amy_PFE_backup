using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class FlockManager : MonoBehaviour
{
    public static FlockManager FM;

    public Transform playerTransform;
    public Camera playerCamera;

    [Header("Zone de nage")]
    public Vector3 swimLimits = new Vector3(6f, 2f, 6f);
    public float minHeight = 11f;
    public float maxHeight = 15f;

    [Header("Objectif")]
    public float goalChangeChance = 0.01f;
    public float goalFollowStrength = 2f;

    [Header("Boids")]
    public float cohesionStrength = 0.8f;
    public float alignmentStrength = 1.2f;
    public float separationRadius = 1.2f;
    public float separationStrength = 3f;
    public float neighbourDistance = 4f;

    [Header("Mouvement")]
    public float minSpeed = 1f;
    public float maxSpeed = 3f;
    public float rotationSpeed = 4f;
    public float wanderStrength = 0.5f;
    public float wanderChangeInterval = 3f;

    [HideInInspector] public Vector3 goalPos;

    NativeArray<Vector3> positions;
    NativeArray<Vector3> velocities;
    NativeArray<float> phaseOffsets;
    NativeArray<float> wanderTimers;
    NativeArray<Vector3> wanderDirs;
    NativeArray<Quaternion> currentRotations;

    NativeArray<Vector3> outPositions;
    NativeArray<Quaternion> outRotations;
    NativeArray<Vector3> outVelocities;

    Transform[] fishTransforms;
    int fishCount;
    JobHandle jobHandle;

    void Awake() => FM = this;

    void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;

        var pool = FlockPooler.Instance.pooledFish;
        fishCount = pool.Count;
        fishTransforms = new Transform[fishCount];

        positions = new NativeArray<Vector3>(fishCount, Allocator.Persistent);
        velocities = new NativeArray<Vector3>(fishCount, Allocator.Persistent);
        phaseOffsets = new NativeArray<float>(fishCount, Allocator.Persistent);
        wanderTimers = new NativeArray<float>(fishCount, Allocator.Persistent);
        wanderDirs = new NativeArray<Vector3>(fishCount, Allocator.Persistent);
        currentRotations = new NativeArray<Quaternion>(fishCount, Allocator.Persistent);
        outPositions = new NativeArray<Vector3>(fishCount, Allocator.Persistent);
        outRotations = new NativeArray<Quaternion>(fishCount, Allocator.Persistent);
        outVelocities = new NativeArray<Vector3>(fishCount, Allocator.Persistent);

        // Goal initial = position joueur
        goalPos = playerTransform.position;

        for (int i = 0; i < fishCount; i++)
        {
            var fish = pool[i];

            // Les poissons sont déjà positionnés par FlockPooler,
            // on récupère juste leurs positions existantes
            Vector3 spawnPos = fish.transform.position;
            Vector3 dir = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

            fish.transform.rotation = rot;
            fish.SetActive(true);

            fishTransforms[i] = fish.transform;
            positions[i] = spawnPos;
            velocities[i] = dir * Random.Range(minSpeed, maxSpeed);
            phaseOffsets[i] = Random.Range(0f, Mathf.PI * 2f);
            wanderTimers[i] = Random.Range(0f, wanderChangeInterval);
            wanderDirs[i] = dir;
            currentRotations[i] = rot;
        }
        Debug.Log("FlockManager: fishCount = " + fishCount);
        if (fishCount > 0)
            Debug.Log("Premier poisson à " + positions[0]);
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
            currentRotations[i] = outRotations[i];
        }

        // Goal suit le joueur + bouge aléatoirement autour de lui
        if (Random.value < goalChangeChance)
        {
            goalPos = playerTransform.position + new Vector3(
                Random.Range(-swimLimits.x, swimLimits.x),
                0f,
                Random.Range(-swimLimits.z, swimLimits.z));
            goalPos.y = Random.Range(minHeight, maxHeight);
        }

        // Colle au joueur en permanence quand il se déplace
        goalPos += (playerTransform.position - goalPos) * (Time.deltaTime * 2f);

        // Wander timers
        for (int i = 0; i < fishCount; i++)
        {
            wanderTimers[i] -= Time.deltaTime;
            if (wanderTimers[i] <= 0f)
            {
                wanderDirs[i] = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized;
                wanderTimers[i] = wanderChangeInterval + Random.Range(-1f, 1f);
            }
        }

        var job = new FlockJob
        {
            goalPos = goalPos,
            playerPos = playerTransform.position,
            swimLimits = swimLimits,
            minHeight = minHeight,
            maxHeight = maxHeight,
            goalFollowStrength = goalFollowStrength,

            cohesionStrength = cohesionStrength,
            alignmentStrength = alignmentStrength,
            separationRadius = separationRadius,
            separationStrength = separationStrength,
            neighbourDistance = neighbourDistance,

            wanderStrength = wanderStrength,
            minSpeed = minSpeed,
            maxSpeed = maxSpeed,
            rotationSpeed = rotationSpeed,

            deltaTime = Time.deltaTime,
            time = Time.time,

            positions = positions,
            velocities = velocities,
            phaseOffsets = phaseOffsets,
            wanderDirs = wanderDirs,
            currentRotations = currentRotations,

            outPositions = outPositions,
            outRotations = outRotations,
            outVelocities = outVelocities
        };

        jobHandle = job.Schedule(fishCount, 32);
    }

    void OnDestroy()
    {
        jobHandle.Complete();
        positions.Dispose();
        velocities.Dispose();
        phaseOffsets.Dispose();
        wanderTimers.Dispose();
        wanderDirs.Dispose();
        currentRotations.Dispose();
        outPositions.Dispose();
        outRotations.Dispose();
        outVelocities.Dispose();
    }

    [BurstCompile]
    struct FlockJob : IJobParallelFor
    {
        [ReadOnly] public Vector3 goalPos;
        [ReadOnly] public Vector3 playerPos;
        [ReadOnly] public Vector3 swimLimits;
        [ReadOnly] public float minHeight, maxHeight;
        [ReadOnly] public float goalFollowStrength;

        [ReadOnly] public float cohesionStrength, alignmentStrength;
        [ReadOnly] public float separationRadius, separationStrength, neighbourDistance;

        [ReadOnly] public float wanderStrength;
        [ReadOnly] public float minSpeed, maxSpeed, rotationSpeed;
        [ReadOnly] public float deltaTime, time;

        [ReadOnly] public NativeArray<Vector3> positions;
        [ReadOnly] public NativeArray<Vector3> velocities;
        [ReadOnly] public NativeArray<float> phaseOffsets;
        [ReadOnly] public NativeArray<Vector3> wanderDirs;
        [ReadOnly] public NativeArray<Quaternion> currentRotations;

        public NativeArray<Vector3> outPositions;
        public NativeArray<Quaternion> outRotations;
        public NativeArray<Vector3> outVelocities;

        public void Execute(int i)
        {
            Vector3 pos = positions[i];
            Vector3 vel = velocities[i];
            float phase = phaseOffsets[i];

            // ── Boids ────────────────────────────────────────────────────────
            Vector3 cohesionSum = Vector3.zero;
            Vector3 alignSum = Vector3.zero;
            Vector3 separate = Vector3.zero;
            int neighbors = 0;

            for (int j = 0; j < positions.Length; j++)
            {
                if (j == i) continue;
                Vector3 diff = positions[j] - pos;
                float d = diff.magnitude;

                if (d < separationRadius && d > 0f)
                    separate -= diff.normalized * (separationRadius - d);

                if (d < neighbourDistance)
                {
                    cohesionSum += positions[j];
                    alignSum += velocities[j];
                    neighbors++;
                }
            }

            Vector3 cohesion = Vector3.zero;
            Vector3 alignment = Vector3.zero;

            if (neighbors > 0)
            {
                cohesion = ((cohesionSum / neighbors) - pos).normalized * cohesionStrength;
                alignment = (alignSum / neighbors).normalized * alignmentStrength;
            }

            // ── Retour dans les bounds autour du joueur ──────────────────────
            Vector3 relativePos = pos - playerPos;
            bool outOfBounds =
                Mathf.Abs(relativePos.x) > swimLimits.x ||
                Mathf.Abs(relativePos.z) > swimLimits.z;

            Vector3 boundsForce = Vector3.zero;
            if (outOfBounds)
            {
                boundsForce = playerPos - pos;
                boundsForce.y = 0f;
                boundsForce = boundsForce.normalized * 5f;
            }

            // ── Suivi du goal ────────────────────────────────────────────────
            Vector3 toGoal = goalPos - pos;
            toGoal.y = 0f;
            Vector3 goalForce = toGoal.normalized * goalFollowStrength;

            // ── Wander ───────────────────────────────────────────────────────
            Vector3 wander = wanderDirs[i] * wanderStrength;

            // ── Direction finale sur plan XZ ─────────────────────────────────
            Vector3 desired = cohesion
                            + alignment
                            + separate * separationStrength
                            + goalForce
                            + boundsForce
                            + wander;
            desired.y = 0f;

            float speed = vel.magnitude;
            if (speed < 0.001f) speed = minSpeed;

            Vector3 dir = vel;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
            dir = dir.normalized;

            if (desired.sqrMagnitude > 0.001f)
                dir = Vector3.Slerp(dir, desired.normalized, rotationSpeed * deltaTime);

            float targetSpeed = Mathf.Lerp(minSpeed, maxSpeed,
                0.5f + Mathf.Sin(time + phase) * 0.3f);
            speed = Mathf.Lerp(speed, targetSpeed, deltaTime * 2f);
            speed = Mathf.Clamp(speed, minSpeed, maxSpeed);

            // ── Rotation — euler.x = 0, jamais de tête en bas ───────────────
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            Quaternion rot = Quaternion.Slerp(
                currentRotations[i],
                targetRot,
                rotationSpeed * deltaTime);

            Vector3 euler = rot.eulerAngles;
            euler.x = 0f;
            rot = Quaternion.Euler(euler);

            // ── Position + contrainte hauteur ────────────────────────────────
            Vector3 newPos = pos + (rot * Vector3.forward) * speed * deltaTime;
            newPos.y = Mathf.Clamp(newPos.y, minHeight, maxHeight);

            outPositions[i] = newPos;
            outRotations[i] = rot;
            outVelocities[i] = (rot * Vector3.forward) * speed;
        }
    }
}