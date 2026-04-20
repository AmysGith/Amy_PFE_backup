using System.Collections.Generic;
using UnityEngine;

public class BoundaryStreaming : MonoBehaviour
{
    [Header("Références")]
    public GameObject tilePrefab;
    public Transform player;
    public WorldBounds worldBounds;

    [Header("Paramètres")]
    public float tileSize = 18f;
    public float viewDistance = 100f;
    public int tilesEachSide = 3;
    public int poolInitialSize = 60;
    public float groundHeight = 13f; // La hauteur de ton sol
    public float offsetFix = 0f;    // Pour ajuster le décalage du pivot si besoin

    // Pool et tuiles actives
    private Stack<GameObject> pool = new Stack<GameObject>();
    private Dictionary<Vector2Int, GameObject> activeTiles = new Dictionary<Vector2Int, GameObject>();

    // ─────────────────────────────────────────────
    // Initialisation du pool
    // ─────────────────────────────────────────────
    void Start()
    {
        for (int i = 0; i < poolInitialSize; i++)
        {
            GameObject obj = Instantiate(tilePrefab, transform);
            obj.SetActive(false);
            pool.Push(obj);
        }
    }

    // ─────────────────────────────────────────────
    // Update : gestion des 4 bords
    // ─────────────────────────────────────────────
    void Update()
    {
        CheckAllBorders();
        ReturnDistantTilesToPool();
    }

    void CheckAllBorders()
    {
        float xMin = worldBounds.MinChunkX * worldBounds.chunkSize;
        float xMax = (worldBounds.MaxChunkX + 1) * worldBounds.chunkSize;
        float zMin = worldBounds.MinChunkZ * worldBounds.chunkSize;
        float zMax = (worldBounds.MaxChunkZ + 1) * worldBounds.chunkSize;

        // Bord Nord (Z+) — joueur regarde vers zMax
        if (player.position.z > zMax - viewDistance)
            ManageWallLine(xMin, xMax, zMax, Axis.Z, 180f);

        // Bord Sud (Z-) — joueur regarde vers zMin
        if (player.position.z < zMin + viewDistance)
            ManageWallLine(xMin, xMax, zMin, Axis.Z, 0f);

        // Bord Est (X+) — joueur regarde vers xMax
        if (player.position.x > xMax - viewDistance)
            ManageWallLine(zMin, zMax, xMax, Axis.X, 90f);

        // Bord Ouest (X-) — joueur regarde vers xMin
        if (player.position.x < xMin + viewDistance)
            ManageWallLine(zMin, zMax, xMin, Axis.X, 270f);
    }

    // ─────────────────────────────────────────────
    // Spawn des tuiles le long d'un bord
    // ─────────────────────────────────────────────
    void ManageWallLine(float from, float to, float fixedCoord, Axis axis, float angle)
    {
        float playerAlongAxis = (axis == Axis.Z) ? player.position.x : player.position.z;
        int currentTileIndex = Mathf.RoundToInt(playerAlongAxis / tileSize);

        for (int i = -tilesEachSide; i <= tilesEachSide; i++)
        {
            int index = currentTileIndex + i;

            // On vérifie que la tuile est bien dans les limites du monde
            float worldPos = index * tileSize;
            if (worldPos < from || worldPos > to) continue;

            Vector2Int key = BuildKey(index, fixedCoord, axis);

            if (!activeTiles.ContainsKey(key))
                SpawnFromPool(index * tileSize, fixedCoord, axis, angle, key);
        }
    }

    // ─────────────────────────────────────────────
    // Retour au pool des tuiles trop éloignées
    // ─────────────────────────────────────────────
    void ReturnDistantTilesToPool()
    {
        List<Vector2Int> toRemove = new List<Vector2Int>();

        foreach (var kvp in activeTiles)
        {
            float dist = Vector3.Distance(player.position, kvp.Value.transform.position);
            if (dist > viewDistance * 1.5f) // marge pour éviter le clignotement
                toRemove.Add(kvp.Key);
        }

        foreach (var key in toRemove)
        {
            GameObject tile = activeTiles[key];
            tile.SetActive(false);
            pool.Push(tile);
            activeTiles.Remove(key);
        }
    }

    // ─────────────────────────────────────────────
    // Instanciation depuis le pool
    // ─────────────────────────────────────────────
    void SpawnFromPool(float posCoord, float fixedCoord, Axis axis, float angle, Vector2Int key)
    {
        if (pool.Count == 0)
        {
            GameObject newObj = Instantiate(tilePrefab, transform);
            newObj.SetActive(false);
            pool.Push(newObj);
        }

        GameObject tile = pool.Pop();

        // On applique la hauteur du sol (groundHeight) au lieu de 0
        // On ajoute 'offsetFix' au cas où le pivot bas-gauche décale la tuile
        if (axis == Axis.Z)
        {
            tile.transform.position = new Vector3(posCoord + offsetFix, groundHeight, fixedCoord);
        }
        else
        {
            tile.transform.position = new Vector3(fixedCoord, groundHeight, posCoord + offsetFix);
        }

        tile.transform.rotation = Quaternion.Euler(0, angle, 0);
        tile.SetActive(true);
        activeTiles.Add(key, tile);
    }

    // ─────────────────────────────────────────────
    // Utilitaires
    // ─────────────────────────────────────────────

    // Clé unique par tuile (encode aussi l'axe pour éviter les collisions entre bords)
    Vector2Int BuildKey(int index, float fixedCoord, Axis axis)
    {
        int axisOffset = (axis == Axis.Z) ? 0 : 100000;
        return new Vector2Int(index + axisOffset, Mathf.RoundToInt(fixedCoord));
    }

    // Enum plus lisible que char
    enum Axis { X, Z }
}