using UnityEngine;
using System.Runtime.InteropServices;

public class FishSchool : MonoBehaviour
{
    [Header("Compute")]
    public ComputeShader computeShader;
    public Mesh fishMesh;
    public Material fishMaterial;

    [Header("Banc")]
    public int fishCount = 300;
    public float speed = 1.5f;

    [Header("Boids")]
    public float separationWeight = 1.5f;
    public float alignmentWeight = 1.0f;
    public float cohesionWeight = 0.8f;

    [Header("Terrain")]
    public Terrain terrain;
    public float minSwimHeight = 2f;

    [Header("Couleurs")]
    public Color[] fishColors = new Color[]
    {
        new Color(0.2f, 0.6f, 0.9f),
        new Color(0.9f, 0.5f, 0.1f),
        new Color(0.2f, 0.8f, 0.4f),
        new Color(0.9f, 0.2f, 0.3f),
        new Color(0.8f, 0.8f, 0.2f),
        new Color(0.6f, 0.2f, 0.9f),
    };

    [Header("Grille (performances)")]
    public float cellSize = 3f;
    public int maxPerCell = 32;

    struct FishData
    {
        public Vector3 pos;
        public Vector3 vel;
        public float yaw, pitch, colorIndex;
    }

    ComputeBuffer fishBuffer;
    ComputeBuffer cellCountBuffer;
    ComputeBuffer cellIndicesBuffer;
    ComputeBuffer argsBuffer;
    Material instancedMaterial;
    RenderTexture heightmapRT;

    int kernelClear, kernelBuild, kernelMain;
    int gridDim, totalCells;

    Vector3 zoneCenter;
    float zoneRadius;

    bool ready;

    void OnEnable() => Initialize();
    void OnDisable() => Release();

    void Initialize()
    {
        if (computeShader == null || fishMesh == null || fishMaterial == null)
        {
            Debug.LogWarning("[FishSchool] Assigne ComputeShader, Mesh et Material.");
            return;
        }

        kernelClear = computeShader.FindKernel("ClearGrid");
        kernelBuild = computeShader.FindKernel("BuildGrid");
        kernelMain = computeShader.FindKernel("CSMain");

        // ── Centre et rayon depuis le SphereCollider ──────────
        var sphere = GetComponent<SphereCollider>();
        if (sphere != null)
        {
            Vector3 worldCenter = transform.TransformPoint(sphere.center);
            zoneRadius = sphere.radius * Mathf.Max(transform.lossyScale.x,
                                                   transform.lossyScale.z);
            float groundY = terrain != null
                ? terrain.SampleHeight(worldCenter) + terrain.transform.position.y
                : 0f;
            // Centre relevé pour que le bas de la sphère touche le sol
            zoneCenter = new Vector3(worldCenter.x, groundY + zoneRadius, worldCenter.z);
        }
        else
        {
            zoneRadius = 12f;
            zoneCenter = transform.position + Vector3.up * zoneRadius;
            Debug.LogWarning("[FishSchool] Pas de SphereCollider, fallback sur le transform.");
        }

        // ── Grille spatiale ───────────────────────────────────
        gridDim = Mathf.CeilToInt((zoneRadius * 2f) / cellSize);
        totalCells = gridDim * gridDim * gridDim;

        fishBuffer = new ComputeBuffer(fishCount, Marshal.SizeOf<FishData>());
        cellCountBuffer = new ComputeBuffer(totalCells, sizeof(int));
        cellIndicesBuffer = new ComputeBuffer(totalCells * maxPerCell, sizeof(int));

        // ── Heightmap ─────────────────────────────────────────
        if (terrain != null)
        {
            var td = terrain.terrainData;
            heightmapRT = new RenderTexture(
                td.heightmapResolution, td.heightmapResolution, 0, RenderTextureFormat.RFloat);
            heightmapRT.enableRandomWrite = true;
            heightmapRT.Create();
            Graphics.Blit(td.heightmapTexture, heightmapRT);
            computeShader.SetTexture(kernelMain, "_HeightMap", heightmapRT);
            computeShader.SetVector("_TerrainSize", td.size);
            computeShader.SetVector("_TerrainPos", terrain.transform.position);
        }
        computeShader.SetFloat("_MinSwimHeight", minSwimHeight);

        // ── Init poissons ─────────────────────────────────────
        FishData[] data = new FishData[fishCount];
        for (int i = 0; i < fishCount; i++)
        {
            Vector3 vel = Random.onUnitSphere * speed;
            vel.y *= 0.2f;
            vel = vel.normalized * speed;

            Vector3 pos = zoneCenter + Random.insideUnitSphere * zoneRadius * 0.8f;
            if (terrain != null)
            {
                float minY = terrain.SampleHeight(pos)
                           + terrain.transform.position.y + minSwimHeight;
                if (pos.y < minY) pos.y = minY;
            }

            data[i].pos = pos;
            data[i].vel = vel;
            data[i].yaw = Mathf.Atan2(vel.x, vel.z);
            data[i].pitch = Mathf.Atan2(-vel.y, new Vector2(vel.x, vel.z).magnitude);
            data[i].colorIndex = Random.Range(0, Mathf.Min(fishColors.Length, 8));
        }
        fishBuffer.SetData(data);

        // ── Bindings compute ──────────────────────────────────
        computeShader.SetBuffer(kernelClear, "_CellCount", cellCountBuffer);
        computeShader.SetBuffer(kernelBuild, "_Fish", fishBuffer);
        computeShader.SetBuffer(kernelBuild, "_CellCount", cellCountBuffer);
        computeShader.SetBuffer(kernelBuild, "_CellIndices", cellIndicesBuffer);
        computeShader.SetBuffer(kernelMain, "_Fish", fishBuffer);
        computeShader.SetBuffer(kernelMain, "_CellCount", cellCountBuffer);
        computeShader.SetBuffer(kernelMain, "_CellIndices", cellIndicesBuffer);

        // ── Args buffer ───────────────────────────────────────
        uint[] args = { fishMesh.GetIndexCount(0), (uint)fishCount, 0, 0, 0 };
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint),
                                       ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        // ── Material ──────────────────────────────────────────
        instancedMaterial = new Material(fishMaterial);
        instancedMaterial.SetBuffer("_Fish", fishBuffer);

        Vector4[] cols = new Vector4[8];
        for (int i = 0; i < 8; i++)
            cols[i] = i < fishColors.Length ? (Vector4)fishColors[i] : Vector4.zero;
        instancedMaterial.SetVectorArray("_FishColors", cols);
        instancedMaterial.SetInt("_ColorCount", fishColors.Length);

        ready = true;
    }

    void Update()
    {
        if (!ready) return;

        computeShader.SetInt("_FishCount", fishCount);
        computeShader.SetInt("_MaxPerCell", maxPerCell);
        computeShader.SetFloat("_DeltaTime", Time.deltaTime);
        computeShader.SetFloat("_Speed", speed);
        computeShader.SetFloat("_SeparationWeight", separationWeight);
        computeShader.SetFloat("_AlignmentWeight", alignmentWeight);
        computeShader.SetFloat("_CohesionWeight", cohesionWeight);
        computeShader.SetVector("_ZoneCenter", zoneCenter);
        computeShader.SetFloat("_ZoneRadius", zoneRadius);
        computeShader.SetFloat("_CellSize", cellSize);
        computeShader.SetInt("_GridDimX", gridDim);
        computeShader.SetInt("_GridDimY", gridDim);
        computeShader.SetInt("_GridDimZ", gridDim);
        computeShader.SetFloat("_MinSwimHeight", minSwimHeight);

        int groupsFish = Mathf.CeilToInt(fishCount / 64f);
        int groupsGrid = Mathf.CeilToInt(totalCells / 64f);

        computeShader.Dispatch(kernelClear, groupsGrid, 1, 1);
        computeShader.Dispatch(kernelBuild, groupsFish, 1, 1);
        computeShader.Dispatch(kernelMain, groupsFish, 1, 1);

        Bounds bounds = new Bounds(zoneCenter, Vector3.one * zoneRadius * 2f);
        if (Camera.main != null)
            bounds.Encapsulate(Camera.main.transform.position);

        Graphics.DrawMeshInstancedIndirect(fishMesh, 0, instancedMaterial, bounds, argsBuffer);
    }

    void Release()
    {
        ready = false;
        fishBuffer?.Release();
        cellCountBuffer?.Release();
        cellIndicesBuffer?.Release();
        argsBuffer?.Release();
        heightmapRT?.Release();
        if (instancedMaterial != null) Destroy(instancedMaterial);
        fishBuffer = null;
        cellCountBuffer = null;
        cellIndicesBuffer = null;
        argsBuffer = null;
        instancedMaterial = null;
    }
}