using System.Collections.Generic;
using UnityEngine;

public class FlockPooler : MonoBehaviour
{
    public static FlockPooler Instance;

    public GameObject[] fishPrefabs;
    public int numFishPerPrefab = 10;
    public List<GameObject> pooledFish = new List<GameObject>();

    public Transform playerTransform;

    [Header("Spawn — réparti sur grand rayon")]
    [Tooltip("Rayon XZ de spawn autour du joueur — mets 20-30 pour éviter les groupes")]
    public float spawnRadius = 25f;
    [Tooltip("Rayon minimum (anneau) — évite que tout le monde spawn au centre")]
    public float spawnRadiusMin = 5f;
    public float minHeight = 13f;
    public float maxHeight = 17f;

    void Awake()
    {
        Instance = this;

        if (fishPrefabs == null || fishPrefabs.Length == 0)
        {
            Debug.LogError("FlockPooler: Aucun prefab assigné !");
            return;
        }

        Vector3 center = playerTransform != null
            ? playerTransform.position
            : new Vector3(25f, 13f, 25f);

        foreach (var prefab in fishPrefabs)
        {
            for (int i = 0; i < numFishPerPrefab; i++)
            {
                // Spawn en anneau : entre spawnRadiusMin et spawnRadius
                // => personne au centre, réparti sur toute la zone
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float radius = Random.Range(spawnRadiusMin, spawnRadius);

                Vector3 spawnPos = new Vector3(
                    center.x + Mathf.Cos(angle) * radius,
                    Random.Range(minHeight, maxHeight),
                    center.z + Mathf.Sin(angle) * radius);

                var fish = Instantiate(prefab, spawnPos, Quaternion.identity);
                fish.SetActive(false);
                pooledFish.Add(fish);
            }
        }

        Debug.Log($"[FlockPooler] {pooledFish.Count} poissons créés en anneau r={spawnRadiusMin}-{spawnRadius} autour de {center}");
    }

    public GameObject GetInactiveFish()
    {
        for (int i = 0; i < pooledFish.Count; i++)
            if (!pooledFish[i].activeInHierarchy) return pooledFish[i];

        Vector3 center = playerTransform != null
            ? playerTransform.position
            : new Vector3(25f, 13f, 25f);

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Random.Range(spawnRadiusMin, spawnRadius);

        Vector3 spawnPos = new Vector3(
            center.x + Mathf.Cos(angle) * radius,
            Random.Range(minHeight, maxHeight),
            center.z + Mathf.Sin(angle) * radius);

        var newFish = Instantiate(
            fishPrefabs[Random.Range(0, fishPrefabs.Length)],
            spawnPos, Quaternion.identity);
        newFish.SetActive(false);
        pooledFish.Add(newFish);
        return newFish;
    }
}