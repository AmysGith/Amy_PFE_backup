using System.Collections.Generic;
using UnityEngine;

public class GlobalInstancingRenderer : MonoBehaviour
{
    public static GlobalInstancingRenderer Instance;

    private const int BATCH_SIZE = 1023;
    private static readonly Matrix4x4[] batchBuffer = new Matrix4x4[BATCH_SIZE];

    // Material → Mesh → Matrices
    private Dictionary<Material, Dictionary<Mesh, List<Matrix4x4>>> batches
        = new Dictionary<Material, Dictionary<Mesh, List<Matrix4x4>>>();

    private Dictionary<Material, MaterialPropertyBlock> mpbs
        = new Dictionary<Material, MaterialPropertyBlock>();

    void Awake()
    {
        Instance = this;
    }

    // ─────────────────────────────────────────────

    public void AddBatch(Mesh mesh, Material mat, Matrix4x4[] matrices, int count)
    {
        if (!batches.TryGetValue(mat, out var meshDict))
        {
            meshDict = new Dictionary<Mesh, List<Matrix4x4>>();
            batches[mat] = meshDict;
        }

        if (!meshDict.TryGetValue(mesh, out var list))
        {
            list = new List<Matrix4x4>();
            meshDict[mesh] = list;
        }

        for (int i = 0; i < count; i++)
            list.Add(matrices[i]);

        if (!mpbs.ContainsKey(mat))
            mpbs[mat] = new MaterialPropertyBlock();
    }

    // ─────────────────────────────────────────────

    void LateUpdate()
    {
        foreach (var matKvp in batches)
        {
            Material mat = matKvp.Key;
            var meshDict = matKvp.Value;

            mpbs.TryGetValue(mat, out MaterialPropertyBlock mpb);

            foreach (var meshKvp in meshDict)
            {
                Mesh mesh = meshKvp.Key;
                List<Matrix4x4> matrices = meshKvp.Value;

                int total = matrices.Count;
                int offset = 0;

                while (offset < total)
                {
                    int batch = Mathf.Min(BATCH_SIZE, total - offset);

                    for (int i = 0; i < batch; i++)
                        batchBuffer[i] = matrices[offset + i];

                    Graphics.DrawMeshInstanced(mesh, 0, mat, batchBuffer, batch, mpb);

                    offset += batch;
                }
            }
        }

        // 🔥 IMPORTANT : reset chaque frame
        batches.Clear();
    }
}