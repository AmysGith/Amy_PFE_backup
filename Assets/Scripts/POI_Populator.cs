using System;
using System.Collections.Generic;
using UnityEngine;

public class POIPopulator : MonoBehaviour
{
    // ── Injecté par POIRegistry ──────────────────────────────────────────────
    public InfiniteTerrainManager terrainManager;

    // ── Config ───────────────────────────────────────────────────────────────
    public List<InstancedObject> objects = new List<InstancedObject>();
    public int worldSeed = 1337;

    public float lodFarDistance = 80f;
    public float lodMidDistance = 40f;
    [Range(0f, 1f)] public float lodMidDensity = 0.4f;

    public float chunkSize = 50f;
    public float boundsHeight = 30f;

    // ── Privés ───────────────────────────────────────────────────────────────
    private const int BATCH_SIZE = 1023;
    private static readonly Matrix4x4[] batchBuffer = new Matrix4x4[BATCH_SIZE];

    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly Color DeadColor = new Color(0.95f, 0.93f, 0.90f, 1f);

    private Vector2Int chunkCoord;
    private bool isPopulated;

    private Camera mainCam;
    private Terrain terrain;
    private TerrainData tData;

    private Bounds chunkBounds;
    private Vector3 chunkCenter;

    private readonly Plane[] frustumFallback = new Plane[6];

    private Dictionary<Material, MaterialPropertyBlock> mpbs = new();
    private Dictionary<Material, Color> originalColors = new();

    private static readonly int BleachAmountID = Shader.PropertyToID("_BaseFloat");
    private class MeshGroup
    {
        public Mesh mesh;
        public Matrix4x4[] matrices;
        public int midCount;
    }

    private Dictionary<Material, List<MeshGroup>> materialToGroups;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        mainCam = Camera.main;
        terrain = GetComponent<Terrain>();
        if (terrain != null) tData = terrain.terrainData;
    }

    private void OnEnable()
    {
        RebuildBounds();
        isPopulated = false;
        TryPopulate();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // API publique
    // ─────────────────────────────────────────────────────────────────────────

    public void Init(Vector2Int coord, InfiniteTerrainManager manager)
    {
        chunkCoord = coord;
        terrainManager = manager;
    }

    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    public void SetBleachProgress(float progress)
    {
        foreach (var kvp in mpbs)
        {
            float brightness = Mathf.Lerp(1f, 8f, progress);
            Color bleached = new Color(brightness, brightness, brightness, 1f);
            kvp.Key.SetColor(BaseColorID, bleached);
        }
    }

    public void Clear()
    {
        materialToGroups = null;
        mpbs.Clear();
        originalColors.Clear();
        isPopulated = false;
    }

    // ─────────────────────────────────────────────────────────────────────────

    private void RebuildBounds()
    {
        chunkCenter = transform.position + new Vector3(chunkSize * 0.5f, 0f, chunkSize * 0.5f);
        chunkBounds = new Bounds(chunkCenter, new Vector3(chunkSize, boundsHeight, chunkSize));
    }

    private void TryPopulate()
    {
        if (terrain == null || tData == null) return;

        mpbs.Clear();
        originalColors.Clear();

        foreach (var obj in objects)
        {
            if (obj.material == null) continue;
            if (!originalColors.ContainsKey(obj.material))
            {
                obj.material.SetColor(BaseColorID, Color.white);
                originalColors[obj.material] = Color.white;
                mpbs[obj.material] = new MaterialPropertyBlock();
            }
        }

        BuildGroups();
        isPopulated = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Construction des groupes
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildGroups()
    {
        if (objects == null || objects.Count == 0) { materialToGroups = null; return; }

        materialToGroups = new Dictionary<Material, List<MeshGroup>>();

        Vector3 tPos = terrain.transform.position;
        float chunkWorldCx = transform.position.x + chunkSize * 0.5f;
        float chunkWorldCz = transform.position.z + chunkSize * 0.5f;

        for (int i = 0; i < objects.Count; i++)
        {
            InstancedObject obj = objects[i];
            if (obj.material == null || obj.count <= 0 ||
                obj.meshes == null || obj.meshes.Length == 0) continue;

            int seed = HashCoord(chunkCoord.x, chunkCoord.y, i, worldSeed);
            var rng = new System.Random(seed);

            int targetCount = obj.count;
            if (obj.usePerlinDensity)
            {
                float f = Mathf.Max(0.0001f, obj.perlinFrequency);
                float noise = Mathf.PerlinNoise(
                    (chunkWorldCx + worldSeed * 1000) * f,
                    (chunkWorldCz - worldSeed * 777) * f);
                targetCount = Mathf.Max(0, Mathf.RoundToInt(
                    obj.count * Mathf.Lerp(obj.perlinDensityMinMax.x, obj.perlinDensityMinMax.y, noise)));
            }
            if (targetCount <= 0) continue;

            Vector2[] centers = null;
            if (obj.useClusters)
            {
                centers = new Vector2[Mathf.Clamp(obj.clustersPerChunk, 1, 64)];
                for (int c = 0; c < centers.Length; c++)
                    centers[c] = new Vector2(NextFloat(rng, 0f, chunkSize), NextFloat(rng, 0f, chunkSize));
            }

            var tempMatrices = new Dictionary<Mesh, List<Matrix4x4>>();

            for (int j = 0; j < targetCount; j++)
            {
                Mesh mesh = obj.meshes[rng.Next(0, obj.meshes.Length)];
                float localX, localZ;

                if (obj.useClusters && NextFloat01(rng) < obj.clusterWeight)
                {
                    Vector2 ct = centers[rng.Next(0, centers.Length)];
                    localX = Mathf.Clamp(ct.x + NextGaussian(rng) * obj.clusterRadius, 0f, chunkSize);
                    localZ = Mathf.Clamp(ct.y + NextGaussian(rng) * obj.clusterRadius, 0f, chunkSize);
                }
                else
                {
                    localX = NextFloat(rng, 0f, chunkSize);
                    localZ = NextFloat(rng, 0f, chunkSize);
                }

                float worldX = transform.position.x + localX;
                float worldZ = transform.position.z + localZ;
                float y = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + tPos.y;

                float yaw = obj.snapYawTo90
                    ? 90f * rng.Next(0, 4)
                    : NextFloat(rng, obj.rotationMinMax.x, obj.rotationMinMax.y);

                Matrix4x4 mat = Matrix4x4.TRS(
                    new Vector3(worldX, y, worldZ),
                    Quaternion.Euler(obj.baseRotation) * Quaternion.Euler(0f, yaw, 0f),
                    Vector3.one * NextFloat(rng, obj.scaleMinMax.x, obj.scaleMinMax.y));

                if (!tempMatrices.ContainsKey(mesh)) tempMatrices[mesh] = new List<Matrix4x4>();
                tempMatrices[mesh].Add(mat);
            }

            foreach (var kv in tempMatrices)
            {
                var mg = new MeshGroup
                {
                    mesh = kv.Key,
                    matrices = kv.Value.ToArray(),
                    midCount = Mathf.Clamp(Mathf.RoundToInt(kv.Value.Count * lodMidDensity), 1, kv.Value.Count)
                };
                if (!materialToGroups.ContainsKey(obj.material))
                    materialToGroups[obj.material] = new List<MeshGroup>();
                materialToGroups[obj.material].Add(mg);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Rendu
    // ─────────────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!isPopulated || materialToGroups == null || mainCam == null) return;

        float distSqr = (mainCam.transform.position - chunkCenter).sqrMagnitude;
        if (distSqr > lodFarDistance * lodFarDistance) return;

        Plane[] planes = terrainManager != null ? terrainManager.FrustumPlanes : null;
        if (planes == null)
        {
            GeometryUtility.CalculateFrustumPlanes(mainCam, frustumFallback);
            planes = frustumFallback;
        }
        if (!GeometryUtility.TestPlanesAABB(planes, chunkBounds)) return;

        bool useMid = distSqr > lodMidDistance * lodMidDistance;

        foreach (var kvp in materialToGroups)
        {
            Material mat = kvp.Key;
            foreach (var group in kvp.Value)
            {
                int drawCount = useMid ? group.midCount : group.matrices.Length;
                if (drawCount > 0) DrawInstanced(group.mesh, mat, group.matrices, drawCount);
            }
        }
    }

    private void DrawInstanced(Mesh mesh, Material mat, Matrix4x4[] matrices, int count)
    {
        mpbs.TryGetValue(mat, out MaterialPropertyBlock mpb);

        int offset = 0;
        while (offset < count)
        {
            int batch = Mathf.Min(BATCH_SIZE, count - offset);
            Array.Copy(matrices, offset, batchBuffer, 0, batch);
            Graphics.DrawMeshInstanced(mesh, 0, mat, batchBuffer, batch, mpb);
            offset += batch;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Utilitaires
    // ─────────────────────────────────────────────────────────────────────────

    static int HashCoord(int x, int y, int i, int seed)
    {
        unchecked
        {
            uint h = (uint)seed;
            h = h * 31u + (uint)x; h = h * 31u + (uint)y; h = h * 31u + (uint)i;
            h ^= h >> 16; h *= 0x7feb352du;
            h ^= h >> 15; h *= 0x846ca68bu;
            h ^= h >> 16;
            return (int)h;
        }
    }

    static float NextFloat01(System.Random r) => (float)r.NextDouble();
    static float NextFloat(System.Random r, float min, float max) => (float)(min + (max - min) * r.NextDouble());
    static float NextGaussian(System.Random r)
    {
        double u1 = 1.0 - r.NextDouble(), u2 = 1.0 - r.NextDouble();
        return (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2));
    }
}