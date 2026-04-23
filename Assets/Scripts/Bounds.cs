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

    [Header("Player & Proximity")]
    [SerializeField] private Transform player;
    [SerializeField] private float warningDistance = 50f;  // → orange fixe
    [SerializeField] private float dangerDistance = 20f;   // → rouge clignotant + buzzer

    public int MinChunkX { get; private set; }
    public int MaxChunkX { get; private set; }
    public int MinChunkZ { get; private set; }
    public int MaxChunkZ { get; private set; }

    private float worldMinX, worldMaxX, worldMinZ, worldMaxZ;
    private char currentMode = '0';

    private void Start()
    {
        ComputeBoundsAndSpawnWalls();
        ArduinoManager.Instance?.SendBounds('0'); // Vert au démarrage
    }

    private void Update()
    {
        if (player == null) return;

        float px = player.position.x;
        float pz = player.position.z;

        float distToEdge = Mathf.Min(
            px - worldMinX,
            worldMaxX - px,
            pz - worldMinZ,
            worldMaxZ - pz
        );

        char newMode;
        if (distToEdge < dangerDistance)
            newMode = '2';      // rouge clignotant + buzzer
        else if (distToEdge < warningDistance)
            newMode = '1';      // orange fixe
        else
            newMode = '0';      // vert fixe

        if (newMode != currentMode)
        {
            currentMode = newMode;
            ArduinoManager.Instance?.SendBounds(currentMode);
        }
    }

    private void OnDestroy()
    {
        ArduinoManager.Instance?.SendBounds('0'); // Repasse au vert à la fin
    }

    private void ComputeBoundsAndSpawnWalls()
    {
        MinChunkX = -worldRadius;
        MaxChunkX = worldRadius;
        MinChunkZ = -worldRadius;
        MaxChunkZ = worldRadius;

        worldMinX = MinChunkX * chunkSize;
        worldMaxX = (MaxChunkX + 1) * chunkSize;
        worldMinZ = MinChunkZ * chunkSize;
        worldMaxZ = (MaxChunkZ + 1) * chunkSize;

        float centerX = (worldMinX + worldMaxX) * 0.5f;
        float centerZ = (worldMinZ + worldMaxZ) * 0.5f;
        float sizeX = worldMaxX - worldMinX;
        float sizeZ = worldMaxZ - worldMinZ;

        Debug.Log($"Bounds: X {MinChunkX} → {MaxChunkX}, Z {MinChunkZ} → {MaxChunkZ}");

        GameObject wallRoot = new GameObject("WorldBounds_Walls");

        SpawnWall(wallRoot.transform, "Wall_North",
            new Vector3(centerX, wallHeight * 0.5f, worldMaxZ + wallThickness * 0.5f),
            new Vector3(sizeX + wallThickness * 2f, wallHeight, wallThickness));

        SpawnWall(wallRoot.transform, "Wall_South",
            new Vector3(centerX, wallHeight * 0.5f, worldMinZ - wallThickness * 0.5f),
            new Vector3(sizeX + wallThickness * 2f, wallHeight, wallThickness));

        SpawnWall(wallRoot.transform, "Wall_East",
            new Vector3(worldMaxX + wallThickness * 0.5f, wallHeight * 0.5f, centerZ),
            new Vector3(wallThickness, wallHeight, sizeZ + wallThickness * 2f));

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