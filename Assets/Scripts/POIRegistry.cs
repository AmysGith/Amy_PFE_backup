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
    public int assignedPrefabIndex;
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

    public InfiniteTerrainManager terrainManager;
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

    void InitializeRegions()
    {
        if (regions.Count > 0) return;
        regions = new List<POIRegion>
        {
            new POIRegion { regionName = "Nord", minChunk = new Vector2Int(-1, 2), maxChunk = new Vector2Int(1, 4) },
            new POIRegion { regionName = "Est", minChunk = new Vector2Int(2, -1), maxChunk = new Vector2Int(4, 1) },
            new POIRegion { regionName = "Sud", minChunk = new Vector2Int(-1, -4), maxChunk = new Vector2Int(1, -2) },
            new POIRegion { regionName = "Ouest", minChunk = new Vector2Int(-4, -1), maxChunk = new Vector2Int(-2, 1) },
            new POIRegion { regionName = "Sud-Ouest", minChunk = new Vector2Int(3, 3), maxChunk = new Vector2Int(5, 5) }
        };
    }

    void GenerateRandomPOIPositions()
    {
        GameObject[] prefabs = Resources.LoadAll<GameObject>("POI").OrderBy(p => p.name).ToArray();
        if (prefabs.Length == 0) return;
        List<int> availablePrefabIndices = Enumerable.Range(0, prefabs.Length).ToList();
        for (int i = 0; i < availablePrefabIndices.Count; i++)
        {
            int temp = availablePrefabIndices[i];
            int randomIndex = UnityEngine.Random.Range(i, availablePrefabIndices.Count);
            availablePrefabIndices[i] = availablePrefabIndices[randomIndex];
            availablePrefabIndices[randomIndex] = temp;
        }
        for (int i = 0; i < regions.Count; i++)
        {
            regions[i].assignedChunk = new Vector2Int(
                UnityEngine.Random.Range(regions[i].minChunk.x, regions[i].maxChunk.x + 1),
                UnityEngine.Random.Range(regions[i].minChunk.y, regions[i].maxChunk.y + 1)
            );
            regions[i].assignedPrefabIndex = availablePrefabIndices[i % availablePrefabIndices.Count];
        }
    }

    void InstantiatePOIs()
    {
        pois = new Dictionary<Vector2Int, StaticPOI>();
        GameObject[] prefabs = Resources.LoadAll<GameObject>("POI").OrderBy(p => p.name).ToArray();
        foreach (var region in regions)
        {
            GameObject prefab = prefabs[region.assignedPrefabIndex];
            Vector2Int coord = region.assignedChunk;
            pois[coord] = Create(prefab, coord, region);
        }
    }

    private StaticPOI Create(GameObject prefab, Vector2Int coord, POIRegion region)
    {
        GameObject instance = Instantiate(prefab, poiRoot);
        instance.name = $"POI_{prefab.name}_{coord}";

        float posX = coord.x * CHUNK_SIZE;
        float posZ = coord.y * CHUNK_SIZE;
        float posY = (prefab.name.ToLower() == "bassin") ? -12.5f : 12.5f;

        instance.transform.position = new Vector3(posX, posY, posZ);
        instance.SetActive(false);

        return new StaticPOI { coord = coord, prefab = prefab, instance = instance };
    }

    public bool HasPOI(Vector2Int coord) => pois != null && pois.ContainsKey(coord);

    public GameObject GetInstance(Vector2Int coord) => pois != null && pois.ContainsKey(coord) ? pois[coord].instance : null;

    public void Activate(Vector2Int coord)
    {
        if (pois != null && pois.ContainsKey(coord))
        {
            var instance = pois[coord].instance;

            var pop = instance.GetComponent<POIPopulator>();
            if (pop != null) pop.Init(coord, terrainManager);

            var trigger = instance.GetComponentInChildren<POIVisitTrigger>();
            if (trigger == null)
            {
                var go = new GameObject("VisitTrigger");
                go.transform.SetParent(instance.transform, false);
                go.transform.localPosition = new Vector3(25f, 10f, 25f);

                var col = go.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(50f, 20f, 50f);

                trigger = go.AddComponent<POIVisitTrigger>();
            }

            trigger.coord = coord;
            instance.SetActive(true);
        }
    }

    public void Deactivate(Vector2Int coord)
    {
        if (pois != null && pois.ContainsKey(coord))
            pois[coord].instance.SetActive(false);
    }

    public Dictionary<Vector2Int, string> GetAllPOIs()
    {
        Dictionary<Vector2Int, string> result = new Dictionary<Vector2Int, string>();
        foreach (var region in regions)
            result[region.assignedChunk] = region.regionName;
        return result;
    }

    public void ResetPOIs()
    {
        foreach (var poi in pois.Values)
            if (poi.instance != null) Destroy(poi.instance);
        pois.Clear();
        GenerateRandomPOIPositions();
        InstantiatePOIs();
    }
}