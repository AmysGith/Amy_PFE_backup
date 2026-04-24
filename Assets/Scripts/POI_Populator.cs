using System;
using System.Collections.Generic;
using UnityEngine;


public class POIPopulator : MonoBehaviour
{
    public InfiniteTerrainManager terrainManager;

    public List<InstancedObject> objects = new List<InstancedObject>();
    public int worldSeed = 1337;

    public float lodFarDistance = 80f;
    public float lodMidDistance = 40f;
    [Range(0f, 1f)] public float lodMidDensity = 0.4f;

    public float chunkSize = 50f;
    public float boundsHeight = 30f;

    [Header("Bleach (ONLY IF ENABLED)")]
    public bool isBleachablePOI = false;
    [Range(0f, 1f)] public float bleachProgress = 0f;

    private const int BATCH_SIZE = 1023;
    private static readonly Matrix4x4[] batchBuffer = new Matrix4x4[BATCH_SIZE];
    private MaterialPropertyBlock globalMPB;

    private Vector2Int chunkCoord;
    private bool isPopulated;

    private Camera mainCam;
    private Terrain terrain;
    private TerrainData tData;

    private Bounds chunkBounds;
    private Vector3 chunkCenter;

    private readonly Plane[] frustumFallback = new Plane[6];

    private Dictionary<Material, List<MeshGroup>> materialToGroups;

    private class MeshGroup
    {
        public Mesh mesh;
        public Matrix4x4[] matrices;
        public int midCount;
        public Color color;
        public bool hasColor;
    }

    private void Awake()
    {
        mainCam = Camera.main;
        terrain = GetComponent<Terrain>();
        if (terrain != null) tData = terrain.terrainData;
        globalMPB = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        RebuildBounds();
        isPopulated = false;
        TryPopulate();
    }

    public void Init(Vector2Int coord, InfiniteTerrainManager manager)
    {
        chunkCoord = coord;
        terrainManager = manager;
        isPopulated = false;
        RebuildBounds();
        TryPopulate();
    }

    public void Clear()
    {
        materialToGroups = null;
        isPopulated = false;
    }

    private void RebuildBounds()
    {
        chunkCenter = transform.position + new Vector3(chunkSize * 0.5f, 0f, chunkSize * 0.5f);
        chunkBounds = new Bounds(chunkCenter, new Vector3(chunkSize, boundsHeight, chunkSize));
    }

    private void TryPopulate()
    {
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

            bool hasColors = obj.instanceColors != null && obj.instanceColors.Length > 0;
            int colorCount = hasColors ? obj.instanceColors.Length : 1;

            Vector2[] centers = null;
            if (obj.useClusters)
            {
                int cCount = Mathf.Clamp(obj.clustersPerChunk, 1, 64);
                centers = new Vector2[cCount];
                for (int c = 0; c < cCount; c++)
                    centers[c] = new Vector2(NextFloat(rng, 0f, chunkSize), NextFloat(rng, 0f, chunkSize));
            }

            // mesh -> (colorIndex -> matrices)
            var tempMatrices = new Dictionary<Mesh, Dictionary<int, List<Matrix4x4>>>();

            for (int m = 0; m < obj.meshes.Length; m++)
            {
                Mesh mesh = obj.meshes[m];
                if (mesh == null) continue;

                if (!tempMatrices.ContainsKey(mesh))
                {
                    tempMatrices[mesh] = new Dictionary<int, List<Matrix4x4>>();
                    for (int ci = 0; ci < colorCount; ci++)
                        tempMatrices[mesh][ci] = new List<Matrix4x4>();
                }

                for (int j = 0; j < targetCount; j++)
                {
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

                    float yaw = obj.snapYawTo90
                        ? (90f * rng.Next(0, 4))
                        : NextFloat(rng, obj.rotationMinMax.x, obj.rotationMinMax.y);

                    Quaternion rot = Quaternion.Euler(obj.baseRotation) * Quaternion.Euler(0f, yaw, 0f);
                    float scale = NextFloat(rng, obj.scaleMinMax.x, obj.scaleMinMax.y);

                    Matrix4x4 mat = Matrix4x4.TRS(
                        new Vector3(worldX, y, worldZ),
                        rot,
                        Vector3.one * scale
                    );

                    int colorIdx = hasColors ? rng.Next(0, colorCount) : 0;
                    tempMatrices[mesh][colorIdx].Add(mat);
                }
            }

            foreach (var meshKv in tempMatrices)
            {
                foreach (var colorKv in meshKv.Value)
                {
                    if (colorKv.Value.Count == 0) continue;

                    Color assignedColor = hasColors ? obj.instanceColors[colorKv.Key] : Color.white;

                    var mg = new MeshGroup
                    {
                        mesh = meshKv.Key,
                        matrices = colorKv.Value.ToArray(),
                        midCount = Mathf.Clamp(Mathf.RoundToInt(colorKv.Value.Count * lodMidDensity), 1, colorKv.Value.Count),
                        color = assignedColor,
                        hasColor = hasColors
                    };

                    if (!materialToGroups.ContainsKey(obj.material))
                        materialToGroups[obj.material] = new List<MeshGroup>();
                    materialToGroups[obj.material].Add(mg);
                }
            }
        }
    }

    private Dictionary<Material, Color> originalColors = new Dictionary<Material, Color>();

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
            GeometryUtility.CalculateFrustumPlanes(mainCam, frustumFallback);
            if (!GeometryUtility.TestPlanesAABB(frustumFallback, chunkBounds)) return;
        }

        bool useMid = distSqr > (lodMidDistance * lodMidDistance);

        foreach (var kvp in materialToGroups)
        {
            Material mat = kvp.Key;
            foreach (var group in kvp.Value)
            {
                int drawCount = useMid ? group.midCount : group.matrices.Length;
                if (drawCount <= 0) continue;
                Draw(group.mesh, mat, group.matrices, drawCount, group.color, group.hasColor);
            }
        }
    }

    private void Draw(Mesh mesh, Material mat, Matrix4x4[] matrices, int count, Color groupColor, bool hasColor)
    {
        globalMPB.Clear();

        Color baseColor;

        if (hasColor)
        {
            baseColor = groupColor;
        }
        else
        {
            if (!originalColors.ContainsKey(mat))
                originalColors[mat] = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;
            baseColor = originalColors[mat];
        }

        if (isBleachablePOI && bleachProgress > 0f)
        {
            baseColor = Color.Lerp(baseColor, Color.white, bleachProgress);
            if (bleachProgress > 0.99f)
                globalMPB.SetTexture("_BaseMap", Texture2D.whiteTexture);
        }

        globalMPB.SetColor("_BaseColor", baseColor);

        int offset = 0;
        while (offset < count)
        {
            int batch = Mathf.Min(BATCH_SIZE, count - offset);
            Array.Copy(matrices, offset, batchBuffer, 0, batch);
            Graphics.DrawMeshInstanced(mesh, 0, mat, batchBuffer, batch, globalMPB);
            offset += batch;
        }
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