using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class FlockManager : MonoBehaviour
{
    public static FlockManager FM;

    public Transform playerTransform;
    public Camera playerCamera;

    [Header("Hauteur de nage")]
    public float minHeight = 13f;
    public float maxHeight = 17f;

    [Header("Zone autour du joueur (XZ)")]
    [Tooltip("Doit être >= spawnRadius du FlockPooler pour que les poissons ne soient pas rappelés dès le départ")]
    public float swimRadius = 28f;
    [Tooltip("Force de rappel quand un poisson sort du rayon")]
    public float boundaryStrength = 3f;

    [Header("Séparation")]
    public float separationRadius = 1.8f;
    public float separationStrength = 2.5f;

    [Header("Wander individuel")]
    public float wanderStrength = 1.2f;
    public float wanderInterval = 4f;

    [Header("Vitesse")]
    public float minSpeed = 0.8f;
    public float maxSpeed = 2.2f;
    public float rotationSpeed = 2f;

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

        for (int i = 0; i < fishCount; i++)
        {
            var fish = pool[i];

            // FlockPooler a déjà réparti les positions en anneau — on les respecte
            // On force juste Y dans la bonne plage au cas où
            Vector3 spawnPos = fish.transform.position;
            if (spawnPos.y < minHeight || spawnPos.y > maxHeight)
                spawnPos.y = Random.Range(minHeight, maxHeight);
            fish.transform.position = spawnPos;

            // Direction initiale aléatoire, différente pour chaque poisson
            Vector3 dir = new Vector3(
                Random.Range(-1f, 1f),
                0f,
                Random.Range(-1f, 1f)).normalized;

            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);
            fish.transform.rotation = rot;
            fish.SetActive(true);

            fishTransforms[i] = fish.transform;
            positions[i] = spawnPos;
            velocities[i] = dir * Random.Range(minSpeed, maxSpeed);
            phaseOffsets[i] = Random.Range(0f, Mathf.PI * 2f);
            // Décalage aléatoire du timer = chaque poisson change de direction à un moment différent
            wanderTimers[i] = Random.Range(0f, wanderInterval);
            wanderDirs[i] = dir;
            currentRotations[i] = rot;
        }

        Debug.Log($"[FlockManager v4] {fishCount} poissons initialisés.");
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

        for (int i = 0; i < fishCount; i++)
        {
            wanderTimers[i] -= Time.deltaTime;
            if (wanderTimers[i] <= 0f)
            {
                wanderDirs[i] = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-0.1f, 0.1f),
                    Random.Range(-1f, 1f)).normalized;
                // Intervalle légèrement variable = encore plus d'asynchronisme entre poissons
                wanderTimers[i] = wanderInterval + Random.Range(-1f, 2f);
            }
        }

        var job = new FlockJob
        {
            playerPos = playerTransform.position,
            swimRadius = swimRadius,
            boundaryStrength = boundaryStrength,
            minHeight = minHeight,
            maxHeight = maxHeight,
            separationRadius = separationRadius,
            separationStrength = separationStrength,
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
            outVelocities = outVelocities,
        };

        jobHandle = job.Schedule(fishCount, 32);
    }

    void OnDestroy()
    {
        jobHandle.Complete();
        positions.Dispose(); velocities.Dispose();
        phaseOffsets.Dispose(); wanderTimers.Dispose();
        wanderDirs.Dispose(); currentRotations.Dispose();
        outPositions.Dispose(); outRotations.Dispose();
        outVelocities.Dispose();
    }

    [BurstCompile]
    struct FlockJob : IJobParallelFor
    {
        [ReadOnly] public Vector3 playerPos;
        [ReadOnly] public float swimRadius, boundaryStrength;
        [ReadOnly] public float minHeight, maxHeight;
        [ReadOnly] public float separationRadius, separationStrength;
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

            // ── Séparation ───────────────────────────────────────────────────
            Vector3 separate = Vector3.zero;
            for (int j = 0; j < positions.Length; j++)
            {
                if (j == i) continue;
                Vector3 away = pos - positions[j];
                float d = away.magnitude;
                if (d < separationRadius && d > 0.001f)
                    separate += away.normalized * ((separationRadius - d) / separationRadius);
            }

            // ── Wander ───────────────────────────────────────────────────────
            Vector3 wander = wanderDirs[i] * wanderStrength;

            // ── Rappel XZ dans le rayon autour du joueur ─────────────────────
            Vector3 toPlayer = playerPos - pos;
            toPlayer.y = 0f;
            float distXZ = toPlayer.magnitude;
            Vector3 boundary = Vector3.zero;
            if (distXZ > swimRadius)
                boundary = toPlayer.normalized * (distXZ - swimRadius) * boundaryStrength;

            // ── Rappel vertical ──────────────────────────────────────────────
            Vector3 heightForce = Vector3.zero;
            if (pos.y < minHeight)
                heightForce.y = (minHeight - pos.y) * boundaryStrength;
            else if (pos.y > maxHeight)
                heightForce.y = -(pos.y - maxHeight) * boundaryStrength;

            // ── Direction désirée ────────────────────────────────────────────
            Vector3 desired = wander
                            + separate * separationStrength
                            + boundary
                            + heightForce;

            float speed = vel.magnitude;
            if (speed < 0.001f) speed = minSpeed;

            Vector3 dir = vel.normalized;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;

            if (desired.sqrMagnitude > 0.001f)
                dir = Vector3.Slerp(dir, desired.normalized, rotationSpeed * deltaTime);

            // ── Vitesse ondulante ────────────────────────────────────────────
            float wave = Mathf.Sin(time * 1.2f + phase);
            float targetSpeed = Mathf.Lerp(minSpeed, maxSpeed, 0.5f + wave * 0.3f);
            speed = Mathf.Lerp(speed, targetSpeed, deltaTime * 2.5f);
            speed = Mathf.Clamp(speed, minSpeed, maxSpeed);

            // ── Rotation douce, jamais tête en bas ───────────────────────────
            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
            Quaternion rot = Quaternion.Slerp(currentRotations[i], targetRot, rotationSpeed * deltaTime);
            Vector3 euler = rot.eulerAngles;
            euler.x = 0f;
            rot = Quaternion.Euler(euler);

            // ── Position finale ──────────────────────────────────────────────
            Vector3 newPos = pos + (rot * Vector3.forward) * speed * deltaTime;
            newPos.y = Mathf.Clamp(newPos.y, minHeight, maxHeight);

            outPositions[i] = newPos;
            outRotations[i] = rot;
            outVelocities[i] = (rot * Vector3.forward) * speed;
        }
    }
}