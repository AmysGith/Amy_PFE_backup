using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrainManager : MonoBehaviour
{
    public Transform player;
    public int chunkSize = 50;
    public int renderDistance = 2;
    public GameObject chunkPrefab;
    public POIRegistry poiRegistry;

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
            if (activeChunks.ContainsKey(coord))
            {
                // Désactiver le chunk standard pour ce coord
                var chunk = activeChunks[coord];
                chunk.SetActive(false);
                chunkPool.Enqueue(chunk);
                activeChunks.Remove(coord);
            }

            poiRegistry.Activate(coord);
            activePOIs.Add(coord);
        }
    }
}
