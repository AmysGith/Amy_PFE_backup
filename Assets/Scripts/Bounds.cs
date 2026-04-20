using UnityEngine;

public class WorldBounds : MonoBehaviour
{
    public POIRegistry poiRegistry;
    public int chunkSize = 50;

    [Header("World Settings")]
    [SerializeField] private int worldRadius = 5;

    [Header("Walls")]
    [SerializeField] private float wallHeight = 100f;
    [SerializeField] private float wallThickness = 10f;

    public int MinChunkX { get; private set; }
    public int MaxChunkX { get; private set; }
    public int MinChunkZ { get; private set; }
    public int MaxChunkZ { get; private set; }

    private void Start()
    {
        ComputeBoundsAndSpawnWalls();
    }

    private void ComputeBoundsAndSpawnWalls()
    {
        // ✅ Bounds FIXES (indépendants des régions)
        MinChunkX = -worldRadius;
        MaxChunkX = worldRadius;
        MinChunkZ = -worldRadius;
        MaxChunkZ = worldRadius;

        // Conversion en coordonnées monde
        float worldMinX = MinChunkX * chunkSize;
        float worldMaxX = (MaxChunkX + 1) * chunkSize;
        float worldMinZ = MinChunkZ * chunkSize;
        float worldMaxZ = (MaxChunkZ + 1) * chunkSize;

        float centerX = (worldMinX + worldMaxX) * 0.5f;
        float centerZ = (worldMinZ + worldMaxZ) * 0.5f;
        float sizeX = worldMaxX - worldMinX;
        float sizeZ = worldMaxZ - worldMinZ;

        Debug.Log($"Bounds: X {MinChunkX} → {MaxChunkX}, Z {MinChunkZ} → {MaxChunkZ}");

        // Root des murs
        GameObject wallRoot = new GameObject("WorldBounds_Walls");

        // Nord
        SpawnWall(wallRoot.transform, "Wall_North",
            new Vector3(centerX, wallHeight * 0.5f, worldMaxZ + wallThickness * 0.5f),
            new Vector3(sizeX + wallThickness * 2f, wallHeight, wallThickness));

        // Sud
        SpawnWall(wallRoot.transform, "Wall_South",
            new Vector3(centerX, wallHeight * 0.5f, worldMinZ - wallThickness * 0.5f),
            new Vector3(sizeX + wallThickness * 2f, wallHeight, wallThickness));

        // Est
        SpawnWall(wallRoot.transform, "Wall_East",
            new Vector3(worldMaxX + wallThickness * 0.5f, wallHeight * 0.5f, centerZ),
            new Vector3(wallThickness, wallHeight, sizeZ + wallThickness * 2f)); // ← même logique que Nord/Sud

        // Ouest
        SpawnWall(wallRoot.transform, "Wall_West",
            new Vector3(worldMinX - wallThickness * 0.5f, wallHeight * 0.5f, centerZ),
            new Vector3(wallThickness, wallHeight, sizeZ + wallThickness * 2f));
    }

    private void SpawnWall(Transform parent, string wallName, Vector3 position, Vector3 size)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.SetParent(parent);
        wall.transform.position = position;
        wall.layer = LayerMask.NameToLayer("Default");

        BoxCollider col = wall.AddComponent<BoxCollider>();
        col.size = size;
        col.isTrigger = false;
    }
}