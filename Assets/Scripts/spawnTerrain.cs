// ================= InfiniteTerrainManager =================
using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrainManager : MonoBehaviour
{
    public Transform player;
    public int chunkSize = 50;
    public int renderDistance = 2;
    public GameObject chunkPrefab;
    public POIRegistry poiRegistry;

    // Chunks actifs
    private Dictionary<Vector2Int, GameObject> activeChunks = new();
    private Queue<GameObject> chunkPool = new();
    private Vector2Int currentChunkCoord;
    private HashSet<Vector2Int> activePOIs = new();

    private void Start()
    {
        currentChunkCoord = GetPlayerChunkCoord();
        UpdateChunks();
    }

    private void Update()
    {
        Vector2Int newChunkCoord = GetPlayerChunkCoord();
        if (newChunkCoord != currentChunkCoord)
        {
            currentChunkCoord = newChunkCoord;
            UpdateChunks();
        }
    }

    private Vector2Int GetPlayerChunkCoord()
    {
        float px = player.position.x + chunkSize * 0.5f;
        float pz = player.position.z + chunkSize * 0.5f;

        int cx = Mathf.FloorToInt(px / chunkSize);
        int cz = Mathf.FloorToInt(pz / chunkSize);

        return new Vector2Int(cx, cz);
    }

    private void UpdateChunks()
    {
        HashSet<Vector2Int> needed = new();

        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int z = -renderDistance; z <= renderDistance; z++)
            {
                Vector2Int coord = new(currentChunkCoord.x + x, currentChunkCoord.y + z);
                needed.Add(coord);

                if (poiRegistry != null && poiRegistry.HasPOI(coord))
                {
                    ActivatePOI(coord);
                }
                else if (!activeChunks.ContainsKey(coord))
                {
                    SpawnChunk(coord);
                }
            }
        }

        // Désactivation des chunks inutiles
        List<Vector2Int> toRemove = new();
        foreach (var kvp in activeChunks)
        {
            if (!needed.Contains(kvp.Key))
            {
                kvp.Value.SetActive(false);
                chunkPool.Enqueue(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var coord in toRemove)
            activeChunks.Remove(coord);

        // Désactivation des POIs trop loin
        if (poiRegistry != null)
        {
            List<Vector2Int> poisToDeactivate = new();
            foreach (var coord in activePOIs)
            {
                if (!needed.Contains(coord))
                {
                    poiRegistry.Deactivate(coord);
                    poisToDeactivate.Add(coord);
                }
            }
            foreach (var coord in poisToDeactivate)
                activePOIs.Remove(coord);
        }
    }

    private void SpawnChunk(Vector2Int coord)
    {
        GameObject chunk;
        if (chunkPool.Count > 0)
            chunk = chunkPool.Dequeue();
        else
            chunk = Instantiate(chunkPrefab);

        chunk.transform.position = new Vector3(coord.x * chunkSize, 0f, coord.y * chunkSize);
        chunk.SetActive(true);
        activeChunks.Add(coord, chunk);
    }

    private void ActivatePOI(Vector2Int coord)
    {
        if (!activePOIs.Contains(coord))
        {
            GameObject poiInstance = poiRegistry.GetInstance(coord);
            if (poiInstance != null)
            {
                poiInstance.SetActive(true);
            }

            // On ne désactive le chunk que si ce n'est pas un POI spécial comme le bassin
            if (activeChunks.ContainsKey(coord) && poiInstance != null)
            {
                var chunk = activeChunks[coord];
                chunk.SetActive(false);
                chunkPool.Enqueue(chunk);
                activeChunks.Remove(coord);
            }

            activePOIs.Add(coord);
        }
    }

    // ===== Helpers pour POIRegistry =====
    public bool ActiveChunksContains(Vector2Int coord) => activeChunks.ContainsKey(coord);
    public GameObject GetActiveChunk(Vector2Int coord)
    {
        if (activeChunks.TryGetValue(coord, out var chunk))
            return chunk;
        return null;
    }

    // Nouvelle méthode pour obtenir la hauteur de surface d'un chunk adjacent
    public float GetAdjacentTerrainSurfaceHeight(Vector2Int poiCoord)
    {
        // Chercher un chunk adjacent (par exemple au nord)
        Vector2Int[] adjacentCoords = new Vector2Int[]
        {
            new Vector2Int(poiCoord.x, poiCoord.y + 1), // Nord
            new Vector2Int(poiCoord.x, poiCoord.y - 1), // Sud
            new Vector2Int(poiCoord.x + 1, poiCoord.y), // Est
            new Vector2Int(poiCoord.x - 1, poiCoord.y)  // Ouest
        };

        foreach (var coord in adjacentCoords)
        {
            if (activeChunks.TryGetValue(coord, out GameObject chunk))
            {
                Terrain terrain = chunk.GetComponent<Terrain>();
                if (terrain != null)
                {
                    // Échantillonner au centre du terrain
                    int centerX = terrain.terrainData.heightmapResolution / 2;
                    int centerZ = terrain.terrainData.heightmapResolution / 2;
                    float sampledHeight = terrain.terrainData.GetHeight(centerX, centerZ);

                    return terrain.transform.position.y + sampledHeight;
                }
            }
        }

        // Si aucun chunk adjacent n'est trouvé, retourner 0
        return 0f;
    }
}