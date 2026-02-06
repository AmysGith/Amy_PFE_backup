using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
    private List<Vector2Int> poiCoords = new()
    {
        new Vector2Int(0,1),
        new Vector2Int(2,2),
        new Vector2Int(1,3),
        new Vector2Int(3,2),
        new Vector2Int(2,3)
    };
    private Dictionary<Vector2Int, StaticPOI> pois;

    private void Awake()
    {
        if (poiRoot == null)
        {
            GameObject root = new GameObject("POI");
            poiRoot = root.transform;
        }
        pois = new Dictionary<Vector2Int, StaticPOI>();
        GameObject[] prefabs = Resources.LoadAll<GameObject>("POI").OrderBy(p => p.name).ToArray();
        for (int i = 0; i < poiCoords.Count; i++)
        {
            GameObject prefab = prefabs[i % prefabs.Length];
            Vector2Int coord = poiCoords[i];
            pois[coord] = Create(prefab, coord);
        }
    }

    private StaticPOI Create(GameObject prefab, Vector2Int coord)
    {
        GameObject instance = Instantiate(prefab, poiRoot);
        instance.name = $"POI_{coord}";
        instance.transform.position = new Vector3(coord.x * CHUNK_SIZE, 0, coord.y * CHUNK_SIZE);
        instance.SetActive(false);
        return new StaticPOI { coord = coord, prefab = prefab, instance = instance };
    }

    public bool HasPOI(Vector2Int coord) => pois.ContainsKey(coord);
    public GameObject GetInstance(Vector2Int coord) => pois[coord].instance;
    public void Activate(Vector2Int coord) => pois[coord].instance.SetActive(true);
    public void Deactivate(Vector2Int coord) => pois[coord].instance.SetActive(false);
}
