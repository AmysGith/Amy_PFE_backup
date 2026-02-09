using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[Serializable]
public class POIRegion
{
    public string regionName;
    public Vector2Int minChunk;
    public Vector2Int maxChunk;

    [HideInInspector]
    public Vector2Int assignedChunk;

    [HideInInspector]
    public int assignedPrefabIndex; // Index du prefab assigné
}

public class StaticPOI
{
    public Vector2Int coord;
    public GameObject prefab;
    public GameObject instance;
}

public class POIRegistry : MonoBehaviour
{
    private const int CHUNK_SIZE = 50;

    [SerializeField] private Transform poiRoot;

    public List<POIRegion> regions = new List<POIRegion>();

    private Dictionary<Vector2Int, StaticPOI> pois;

    private void Awake()
    {
        if (poiRoot == null)
        {
            GameObject root = new GameObject("POI");
            poiRoot = root.transform;
        }

        InitializeRegions();
        GenerateRandomPOIPositions();
        InstantiatePOIs();
    }

    // Initialise les régions si elles ne sont pas définies dans l'inspecteur
    void InitializeRegions()
    {
        if (regions.Count > 0) return;

        regions = new List<POIRegion>
        {
            new POIRegion { regionName = "Nord", minChunk = new Vector2Int(-1, 2), maxChunk = new Vector2Int(1, 4) },
            new POIRegion { regionName = "Est", minChunk = new Vector2Int(2, -1), maxChunk = new Vector2Int(4, 1) },
            new POIRegion { regionName = "Sud", minChunk = new Vector2Int(-1, -4), maxChunk = new Vector2Int(1, -2) },
            new POIRegion { regionName = "Ouest", minChunk = new Vector2Int(-4, -1), maxChunk = new Vector2Int(-2, 1) },
            new POIRegion { regionName = "Centre-Nord-Est", minChunk = new Vector2Int(3, 3), maxChunk = new Vector2Int(5, 5) }
        };
    }

    // Génère des positions et prefabs aléatoires pour la session
    void GenerateRandomPOIPositions()
    {
        GameObject[] prefabs = Resources.LoadAll<GameObject>("POI").OrderBy(p => p.name).ToArray();

        if (prefabs.Length == 0)
        {
            Debug.LogError("Aucun prefab trouvé dans Resources/POI !");
            return;
        }

        // Crée et mélange une liste d'indices de prefabs
        List<int> availablePrefabIndices = Enumerable.Range(0, prefabs.Length).ToList();
        for (int i = 0; i < availablePrefabIndices.Count; i++)
        {
            int temp = availablePrefabIndices[i];
            int randomIndex = UnityEngine.Random.Range(i, availablePrefabIndices.Count);
            availablePrefabIndices[i] = availablePrefabIndices[randomIndex];
            availablePrefabIndices[randomIndex] = temp;
        }

        // Assigne une position et un prefab aléatoire à chaque région
        for (int i = 0; i < regions.Count; i++)
        {
            regions[i].assignedChunk = new Vector2Int(
                UnityEngine.Random.Range(regions[i].minChunk.x, regions[i].maxChunk.x + 1),
                UnityEngine.Random.Range(regions[i].minChunk.y, regions[i].maxChunk.y + 1)
            );

            regions[i].assignedPrefabIndex = availablePrefabIndices[i % availablePrefabIndices.Count];

            Debug.Log($"Région '{regions[i].regionName}' → Chunk {regions[i].assignedChunk}, Prefab {prefabs[regions[i].assignedPrefabIndex].name}");
        }
    }

    // Instancie les POIs dans le monde
    void InstantiatePOIs()
    {
        pois = new Dictionary<Vector2Int, StaticPOI>();

        GameObject[] prefabs = Resources.LoadAll<GameObject>("POI").OrderBy(p => p.name).ToArray();
        if (prefabs.Length == 0)
        {
            Debug.LogError("Aucun prefab trouvé dans Resources/POI !");
            return;
        }

        foreach (var region in regions)
        {
            GameObject prefab = prefabs[region.assignedPrefabIndex];
            Vector2Int coord = region.assignedChunk;
            pois[coord] = Create(prefab, coord);
        }
    }

    private StaticPOI Create(GameObject prefab, Vector2Int coord)
    {
        GameObject instance = Instantiate(prefab, poiRoot);
        instance.name = $"POI_{prefab.name}_{coord}";
        instance.transform.position = new Vector3(coord.x * CHUNK_SIZE, 0, coord.y * CHUNK_SIZE);
        instance.SetActive(false);

        return new StaticPOI { coord = coord, prefab = prefab, instance = instance };
    }

    // ===== MÉTHODES PUBLIQUES =====

    public bool HasPOI(Vector2Int coord) => pois.ContainsKey(coord);

    public GameObject GetInstance(Vector2Int coord) => pois.ContainsKey(coord) ? pois[coord].instance : null;

    public void Activate(Vector2Int coord)
    {
        if (pois.ContainsKey(coord))
            pois[coord].instance.SetActive(true);
    }

    public void Deactivate(Vector2Int coord)
    {
        if (pois.ContainsKey(coord))
            pois[coord].instance.SetActive(false);
    }

    public Dictionary<Vector2Int, string> GetAllPOIs()
    {
        Dictionary<Vector2Int, string> result = new Dictionary<Vector2Int, string>();
        foreach (var region in regions)
            result[region.assignedChunk] = region.regionName;
        return result;
    }

    // Reset pour debug
    public void ResetPOIs()
    {
        foreach (var poi in pois.Values)
        {
            if (poi.instance != null)
                Destroy(poi.instance);
        }
        pois.Clear();

        GenerateRandomPOIPositions();
        InstantiatePOIs();
    }
}
