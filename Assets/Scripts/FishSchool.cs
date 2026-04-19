using UnityEngine;
using System.Runtime.InteropServices;

public class FishSchool : MonoBehaviour
{
    [Header("Compute")]
    public ComputeShader computeShader;
    public Mesh fishMesh;
    public Material fishMaterial;

    [Header("Settings")]
    public int fishCount = 300;
    public float zoneRadius = 12f;
    public float speed = 1.5f;

    public float separationWeight = 1.5f;
    public float alignmentWeight = 1.0f;
    public float cohesionWeight = 0.8f;

    [Header("Grid")]
    public float cellSize = 5f;
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

    int kernelClear, kernelBuild, kernelMain;
    int gridDim;
    int totalCells;

    void Start()
    {
        kernelClear = computeShader.FindKernel("ClearGrid");
        kernelBuild = computeShader.FindKernel("BuildGrid");
        kernelMain = computeShader.FindKernel("CSMain");

        // Grille cubique centrée sur la zone sphérique
        gridDim = Mathf.CeilToInt((zoneRadius * 2f) / cellSize);
        totalCells = gridDim * gridDim * gridDim;

        fishBuffer = new ComputeBuffer(fishCount, Marshal.SizeOf<FishData>());
        cellCountBuffer = new ComputeBuffer(totalCells, sizeof(int));
        cellIndicesBuffer = new ComputeBuffer(totalCells * maxPerCell, sizeof(int));

        FishData[] data = new FishData[fishCount];
        for (int i = 0; i < fishCount; i++)
        {
            Vector3 vel = Random.onUnitSphere;
            vel.y *= 0.2f;
            vel = vel.normalized * speed;

            data[i].pos = transform.position + Random.insideUnitSphere * zoneRadius * 0.8f;
            data[i].vel = vel;
            data[i].yaw = Mathf.Atan2(vel.x, vel.z);
            data[i].pitch = Mathf.Atan2(-vel.y, new Vector2(vel.x, vel.z).magnitude);
            data[i].colorIndex = Random.Range(0, 6);
        }
        fishBuffer.SetData(data);

        // Bindings
        foreach (int k in new[] { kernelClear, kernelBuild, kernelMain })
        {
            if (computeShader.HasKernel(kernelClear.ToString())) { }
            computeShader.SetBuffer(kernelClear, "_CellCount", cellCountBuffer);
        }
        computeShader.SetBuffer(kernelBuild, "_Fish", fishBuffer);
        computeShader.SetBuffer(kernelBuild, "_CellCount", cellCountBuffer);
        computeShader.SetBuffer(kernelBuild, "_CellIndices", cellIndicesBuffer);
        computeShader.SetBuffer(kernelMain, "_Fish", fishBuffer);
        computeShader.SetBuffer(kernelMain, "_CellCount", cellCountBuffer);
        computeShader.SetBuffer(kernelMain, "_CellIndices", cellIndicesBuffer);

        uint[] args = { fishMesh.GetIndexCount(0), (uint)fishCount, 0, 0, 0 };
        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        fishMaterial.SetBuffer("_Fish", fishBuffer);
    }

    void Update()
    {
        if (!enabled || !gameObject.activeInHierarchy) return;

        Vector3 center = transform.position;          // centré sur le GameObject

        computeShader.SetInt("_FishCount", fishCount);
        computeShader.SetInt("_MaxPerCell", maxPerCell);
        computeShader.SetFloat("_DeltaTime", Time.deltaTime);
        computeShader.SetFloat("_Speed", speed);
        computeShader.SetFloat("_SeparationWeight", separationWeight);
        computeShader.SetFloat("_AlignmentWeight", alignmentWeight);
        computeShader.SetFloat("_CohesionWeight", cohesionWeight);
        computeShader.SetVector("_ZoneCenter", center);
        computeShader.SetFloat("_ZoneRadius", zoneRadius);
        computeShader.SetFloat("_CellSize", cellSize);
        computeShader.SetInt("_GridDimX", gridDim);
        computeShader.SetInt("_GridDimY", gridDim);
        computeShader.SetInt("_GridDimZ", gridDim);

        int groupsFish = Mathf.CeilToInt(fishCount / 64f);
        int groupsGrid = Mathf.CeilToInt(totalCells / 64f);

        computeShader.Dispatch(kernelClear, groupsGrid, 1, 1);
        computeShader.Dispatch(kernelBuild, groupsFish, 1, 1);
        computeShader.Dispatch(kernelMain, groupsFish, 1, 1);

        Bounds bounds = new Bounds(center, Vector3.one * zoneRadius * 2f);
        Graphics.DrawMeshInstancedIndirect(fishMesh, 0, fishMaterial, bounds, argsBuffer);
    }


    void OnDestroy()
    {
        fishBuffer?.Release();
        cellCountBuffer?.Release();
        cellIndicesBuffer?.Release();
        argsBuffer?.Release();
    }
}