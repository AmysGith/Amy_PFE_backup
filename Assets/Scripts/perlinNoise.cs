using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class ChunkTerrainUnderwater : MonoBehaviour
{
    [Header("Relief Settings")]
    public float maxHeight = 5f;
    public float noiseScale = 0.05f;
    public int globalSeed = 12345;

    [Header("Chunk Info")]
    public Vector2Int chunkCoord;
    public int chunkSize = 50;

    private Terrain terrain;
    private TerrainData terrainData;

    private void Awake()
    {
        terrain = GetComponent<Terrain>();

        // ✅ Créer un nouveau TerrainData pour éviter de partager entre instances
        terrainData = new TerrainData();
        terrainData.heightmapResolution = 129; // Doit être 2^n + 1
        terrainData.size = new Vector3(chunkSize, maxHeight, chunkSize);

        terrain.terrainData = terrainData;


    }

    public void GenerateTerrain()
    {
        int resolution = terrainData.heightmapResolution;
        float[,] heights = new float[resolution, resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                // ✅ Coordonnées mondiales pour continuité entre chunks
                float worldX = (chunkCoord.x * chunkSize) + ((float)x / (resolution - 1) * chunkSize);
                float worldZ = (chunkCoord.y * chunkSize) + ((float)y / (resolution - 1) * chunkSize);

                // ✅ Perlin noise avec coordonnées mondiales + seed
                float noiseValue = Mathf.PerlinNoise(
                    (worldX + globalSeed) * noiseScale,
                    (worldZ + globalSeed) * noiseScale
                );

                // ✅ Optionnel : ajouter plusieurs octaves pour plus de détail
                float detailNoise = Mathf.PerlinNoise(
                    (worldX + globalSeed) * noiseScale * 3f,
                    (worldZ + globalSeed) * noiseScale * 3f
                ) * 0.3f;

                // ✅ Combiner les octaves
                float finalNoise = noiseValue * 0.7f + detailNoise;

                // ✅ Mapper entre 0 et 1 (le terrain Unity utilise des hauteurs normalisées)
                heights[y, x] = Mathf.Clamp01(finalNoise);
            }
        }

        terrainData.SetHeights(0, 0, heights);

        // ✅ Forcer la mise à jour du collider
        terrain.terrainData = terrainData;
    }

    // ✅ Optionnel : visualiser les limites du chunk en mode debug
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = transform.position + new Vector3(chunkSize / 2f, 0, chunkSize / 2f);
        Gizmos.DrawWireCube(center, new Vector3(chunkSize, maxHeight, chunkSize));
    }
}