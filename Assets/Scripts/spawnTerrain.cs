using System.Collections.Generic;
using UnityEngine;

public class InfiniteTerrainManager : MonoBehaviour
{
    public Transform player;
    public int chunkSize = 50;
    public int renderDistance = 2;
    public GameObject chunkPrefab;
    public POIRegistry poiRegistry;
    public WorldBounds worldBounds;

    private Dictionary<Vector2Int, GameObject> activeChunks = new();
    private Queue<GameObject> chunkPool = new();
    private Vector2Int currentChunkCoord;
    private HashSet<Vector2Int> activePOIs = new();

    private readonly Plane[] cachedFrustumPlanes = new Plane[6];
    public Plane[] FrustumPlanes => cachedFrustumPlanes;

    private void Start()
    {
        currentChunkCoord = GetPlayerChunkCoord();

        if (Camera.main != null)
            GeometryUtility.CalculateFrustumPlanes(Camera.main, cachedFrustumPlanes);

        UpdateChunksWithRadius(renderDistance + 1);
    }

    private void Update()
    {
        if (Camera.main != null)
            GeometryUtility.CalculateFrustumPlanes(Camera.main, cachedFrustumPlanes);

        Vector2Int newChunkCoord = GetPlayerChunkCoord();

        if (newChunkCoord != currentChunkCoord)
        {
            currentChunkCoord = newChunkCoord;

            if (!IsPlayerAtBounds())
                UpdateChunks();
        }
    }

    private Vector2Int GetPlayerChunkCoord()
    {
        return new Vector2Int(
            Mathf.FloorToInt(player.position.x / chunkSize),
            Mathf.FloorToInt(player.position.z / chunkSize)
        );
    }

    private bool IsPlayerAtBounds()
    {
        if (worldBounds == null) return false;

        Vector2Int coord = currentChunkCoord;

        return coord.x <= worldBounds.MinChunkX ||
               coord.x >= worldBounds.MaxChunkX ||
               coord.y <= worldBounds.MinChunkZ ||
               coord.y >= worldBounds.MaxChunkZ;
    }

    private bool IsWithinBounds(Vector2Int coord)
    {
        if (worldBounds == null) return true;

        return coord.x >= worldBounds.MinChunkX &&
               coord.x <= worldBounds.MaxChunkX &&
               coord.y >= worldBounds.MinChunkZ &&
               coord.y <= worldBounds.MaxChunkZ;
    }

    private void UpdateChunks() => UpdateChunksWithRadius(renderDistance);

    private void UpdateChunksWithRadius(int radius)
    {
        HashSet<Vector2Int> needed = new();

        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                Vector2Int coord = new(currentChunkCoord.x + x, currentChunkCoord.y + z);

                if (!IsWithinBounds(coord))
                    continue;

                needed.Add(coord);

                if (poiRegistry != null && poiRegistry.HasPOI(coord))
                    ActivatePOI(coord);
                else if (!activeChunks.ContainsKey(coord))
                    SpawnChunk(coord);
            }
        }

        List<Vector2Int> toRemove = new();

        foreach (var kvp in activeChunks)
        {
            if (!needed.Contains(kvp.Key))
            {
                kvp.Value.GetComponent<ChunkPopulator>()?.Clear();
                kvp.Value.SetActive(false);
                chunkPool.Enqueue(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var coord in toRemove)
            activeChunks.Remove(coord);

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

        chunk.transform.position = new Vector3(
            coord.x * chunkSize,
            0f,
            coord.y * chunkSize
        );

        chunk.SetActive(true);
        activeChunks.Add(coord, chunk);

        var populator = chunk.GetComponent<ChunkPopulator>();
        if (populator != null)
        {
            populator.terrainManager = this;
            populator.Init(coord, poiRegistry);
        }
    }

    private void ActivatePOI(Vector2Int coord)
    {
        if (!activePOIs.Contains(coord))
        {
            poiRegistry.Activate(coord);

            if (activeChunks.ContainsKey(coord))
            {
                var chunk = activeChunks[coord];
                chunk.GetComponent<ChunkPopulator>()?.Clear();
                chunk.SetActive(false);
                chunkPool.Enqueue(chunk);
                activeChunks.Remove(coord);
            }

            activePOIs.Add(coord);
        }
    }

    public bool ActiveChunksContains(Vector2Int coord)
        => activeChunks.ContainsKey(coord);

    public GameObject GetActiveChunk(Vector2Int coord)
    {
        activeChunks.TryGetValue(coord, out var chunk);
        return chunk;
    }

    public float GetAdjacentTerrainSurfaceHeight(Vector2Int poiCoord)
    {
        Vector2Int[] adjacentCoords =
        {
            new(poiCoord.x, poiCoord.y + 1),
            new(poiCoord.x, poiCoord.y - 1),
            new(poiCoord.x + 1, poiCoord.y),
            new(poiCoord.x - 1, poiCoord.y)
        };

        foreach (var coord in adjacentCoords)
        {
            if (activeChunks.TryGetValue(coord, out GameObject chunk))
            {
                Terrain terrain = chunk.GetComponent<Terrain>();
                if (terrain != null)
                {
                    int cx = terrain.terrainData.heightmapResolution / 2;
                    int cz = terrain.terrainData.heightmapResolution / 2;

                    return terrain.transform.position.y +
                           terrain.terrainData.GetHeight(cx, cz);
                }
            }
        }

        return 0f;
    }
}