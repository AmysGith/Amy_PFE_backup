using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InstancedObject
{
    public string label;
    public Mesh[] meshes;
    public Material material;

    [Min(0)] public int count = 200;

    public Vector2 scaleMinMax = new Vector2(0.8f, 1.2f);
    public Vector2 rotationMinMax = new Vector2(0f, 360f);

    public bool alignToSurface = false;

    public bool usePerlinDensity = true;
    [Range(0.01f, 1f)] public float perlinFrequency = 0.12f;
    public Vector2 perlinDensityMinMax = new Vector2(0.2f, 1.3f);

    public bool useClusters = true;
    [Range(1, 16)] public int clustersPerChunk = 6;
    [Range(0.2f, 20f)] public float clusterRadius = 4f;
    [Range(0f, 1f)] public float clusterWeight = 0.85f;

    public bool snapYawTo90 = true;
    public Vector3 baseRotation;
}

public class ChunkPopulator : MonoBehaviour
{
    public POIRegistry poiRegistry;
    public InfiniteTerrainManager terrainManager;

    public int worldSeed = 1337;
    public List<InstancedObject> objects = new List<InstancedObject>();

    public float lodFarDistance = 80f;
    public float lodMidDistance = 40f;
    [Range(0f, 1f)] public float lodMidDensity = 0.4f;

    public float chunkSize = 50f;
    public float boundsHeight = 30f;

    private const int BATCH_SIZE = 1023;
    private Vector2Int chunkCoord;
    private bool isPopulated;

    private Camera mainCam;
    private Terrain terrain;
    private TerrainData tData;

    private readonly Plane[] frustumPlanesFallback = new Plane[6];
    private static readonly Matrix4x4[] batchBuffer = new Matrix4x4[BATCH_SIZE];

    private Bounds chunkBounds;
    private Vector3 chunkCenter;

    private class MeshGroup
    {
        public Mesh mesh;
        public Matrix4x4[] matrices;
        public int midCount;
    }

    private Dictionary<Material, List<MeshGroup>> materialToGroups;

    private void Awake()
    {
        mainCam = Camera.main;
        terrain = GetComponent<Terrain>();
        if (terrain != null) tData = terrain.terrainData;
    }

    private void OnEnable()
    {
        if (!isPopulated && poiRegistry != null)
            TryPopulate();
    }

    public void Init(Vector2Int coord, POIRegistry registry)
    {
        chunkCoord = coord;
        poiRegistry = registry;
        isPopulated = false;
        RebuildChunkBounds();
        TryPopulate();
    }

    private void RebuildChunkBounds()
    {
        chunkCenter = transform.position + new Vector3(chunkSize * 0.5f, 0f, chunkSize * 0.5f);
        chunkBounds = new Bounds(chunkCenter, new Vector3(chunkSize, boundsHeight, chunkSize));
    }

    private void TryPopulate()
    {
        if (poiRegistry != null && poiRegistry.HasPOI(chunkCoord)) return;
        if (terrain == null || tData == null) return;

        BuildGroups();
        isPopulated = true;
    }

    private void BuildGroups()
    {
        if (objects == null || objects.Count == 0)
        {
            materialToGroups = null;
            return;
        }

        materialToGroups = new Dictionary<Material, List<MeshGroup>>();

        Vector3 tPos = terrain.transform.position;
        float chunkWorldCx = transform.position.x + chunkSize * 0.5f;
        float chunkWorldCz = transform.position.z + chunkSize * 0.5f;

        for (int i = 0; i < objects.Count; i++)
        {
            InstancedObject obj = objects[i];
            if (obj.material == null || obj.count <= 0 || obj.meshes == null || obj.meshes.Length == 0)
                continue;

            var validMeshes = obj.meshes;

            int seed = Hash(chunkCoord.x, chunkCoord.y, i, worldSeed);
            var rng = new System.Random(seed);

            int targetCount = obj.count;

            if (obj.usePerlinDensity)
            {
                float f = Mathf.Max(0.0001f, obj.perlinFrequency);
                float noise = Mathf.PerlinNoise(
                    (chunkWorldCx + worldSeed * 1000) * f,
                    (chunkWorldCz - worldSeed * 777) * f
                );
                float densityMul = Mathf.Lerp(obj.perlinDensityMinMax.x, obj.perlinDensityMinMax.y, noise);
                targetCount = Mathf.Max(0, Mathf.RoundToInt(obj.count * densityMul));
            }

            if (targetCount <= 0) continue;

            Vector2[] centers = null;
            if (obj.useClusters)
            {
                int cCount = Mathf.Clamp(obj.clustersPerChunk, 1, 64);
                centers = new Vector2[cCount];
                for (int c = 0; c < cCount; c++)
                    centers[c] = new Vector2(NextFloat(rng, 0f, chunkSize), NextFloat(rng, 0f, chunkSize));
            }

            Dictionary<Mesh, List<Matrix4x4>> tempMatrices = new Dictionary<Mesh, List<Matrix4x4>>();

            for (int j = 0; j < targetCount; j++)
            {
                Mesh mesh = validMeshes[rng.Next(0, validMeshes.Length)];

                float localX, localZ;
                bool doCluster = obj.useClusters && (NextFloat01(rng) < obj.clusterWeight);

                if (doCluster)
                {
                    int c = rng.Next(0, centers.Length);
                    Vector2 center = centers[c];
                    float dx = NextGaussian(rng) * obj.clusterRadius;
                    float dz = NextGaussian(rng) * obj.clusterRadius;
                    localX = Mathf.Clamp(center.x + dx, 0f, chunkSize);
                    localZ = Mathf.Clamp(center.y + dz, 0f, chunkSize);
                }
                else
                {
                    localX = NextFloat(rng, 0f, chunkSize);
                    localZ = NextFloat(rng, 0f, chunkSize);
                }

                float worldX = transform.position.x + localX;
                float worldZ = transform.position.z + localZ;
                float y = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + tPos.y;

                Vector3 pos = new Vector3(worldX, y, worldZ);

                float yaw = obj.snapYawTo90
                    ? (90f * rng.Next(0, 4))
                    : NextFloat(rng, obj.rotationMinMax.x, obj.rotationMinMax.y);

                Quaternion rot = Quaternion.Euler(obj.baseRotation) * Quaternion.Euler(0f, yaw, 0f);

                float scale = NextFloat(rng, obj.scaleMinMax.x, obj.scaleMinMax.y);

                Matrix4x4 mat = Matrix4x4.TRS(pos, rot, Vector3.one * scale);

                if (!tempMatrices.ContainsKey(mesh))
                    tempMatrices[mesh] = new List<Matrix4x4>();

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

    private void Update()
    {
        if (!isPopulated || materialToGroups == null || mainCam == null) return;

        Vector3 d = mainCam.transform.position - chunkCenter;
        float distSqr = d.sqrMagnitude;

        if (distSqr > lodFarDistance * lodFarDistance) return;

        Plane[] planes = (terrainManager != null) ? terrainManager.FrustumPlanes : null;

        if (planes != null)
        {
            if (!GeometryUtility.TestPlanesAABB(planes, chunkBounds)) return;
        }
        else
        {
            GeometryUtility.CalculateFrustumPlanes(mainCam, frustumPlanesFallback);
            if (!GeometryUtility.TestPlanesAABB(frustumPlanesFallback, chunkBounds)) return;
        }

        bool useMid = distSqr > (lodMidDistance * lodMidDistance);

        foreach (var kvp in materialToGroups)
        {
            Material mat = kvp.Key;

            foreach (var group in kvp.Value)
            {
                int drawCount = useMid ? group.midCount : group.matrices.Length;
                if (drawCount <= 0) continue;

                DrawInstanced(group.mesh, mat, group.matrices, drawCount);
            }
        }
    }

    private void DrawInstanced(Mesh mesh, Material mat, Matrix4x4[] matrices, int count)
    {
        int offset = 0;

        while (offset < count)
        {
            int batch = Mathf.Min(BATCH_SIZE, count - offset);
            Array.Copy(matrices, offset, batchBuffer, 0, batch);
            Graphics.DrawMeshInstanced(mesh, 0, mat, batchBuffer, batch);
            offset += batch;
        }
    }

    public void Clear()
    {
        materialToGroups = null;
        isPopulated = false;
    }

    static int Hash(int x, int y, int i, int seed)
    {
        unchecked
        {
            uint h = (uint)seed;
            h = h * 31u + (uint)x;
            h = h * 31u + (uint)y;
            h = h * 31u + (uint)i;
            h ^= (h >> 16);
            h *= 0x7feb352du;
            h ^= (h >> 15);
            h *= 0x846ca68bu;
            h ^= (h >> 16);
            return (int)h;
        }
    }

    static float NextFloat01(System.Random rng) => (float)rng.NextDouble();
    static float NextFloat(System.Random rng, float min, float max) => (float)(min + (max - min) * rng.NextDouble());

    static float NextGaussian(System.Random rng)
    {
        double u1 = 1.0 - rng.NextDouble();
        double u2 = 1.0 - rng.NextDouble();
        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        return (float)randStdNormal;
    }
}
