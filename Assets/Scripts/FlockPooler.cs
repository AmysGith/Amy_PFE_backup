using System.Collections.Generic;
using UnityEngine;

public class FlockPooler : MonoBehaviour
{
    public static FlockPooler Instance;

    public GameObject[] fishPrefabs;
    public int numFishPerPrefab = 10;
    public List<GameObject> pooledFish = new List<GameObject>();

    public Transform playerTransform;  // Assigne le joueur ici dans l'inspecteur
    public float spawnRadius = 5f;     // Rayon de spawn autour du joueur
    public float minHeight = 11f;      // Hauteur min (joueur à Y=13)
    public float maxHeight = 15f;      // Hauteur max

    void Awake()
    {
        Instance = this;

        if (fishPrefabs == null || fishPrefabs.Length == 0)
        {
            Debug.LogError("FlockPooler: Aucun prefab assigné !");
            return;
        }

        // Position de base : le joueur, ou (25,13,25) si pas assigné
        Vector3 center = playerTransform != null
            ? playerTransform.position
            : new Vector3(25f, 13f, 25f);

        foreach (var prefab in fishPrefabs)
        {
            for (int i = 0; i < numFishPerPrefab; i++)
            {
                // Position aléatoire autour du joueur
                Vector3 offset = new Vector3(
                    Random.Range(-spawnRadius, spawnRadius),
                    0f,
                    Random.Range(-spawnRadius, spawnRadius));

                Vector3 spawnPos = center + offset;
                spawnPos.y = Random.Range(minHeight, maxHeight);

                var fish = Instantiate(prefab, spawnPos, Quaternion.identity);
                fish.SetActive(false);
                pooledFish.Add(fish);
            }
        }
        Debug.Log("FlockPooler: " + pooledFish.Count + " poissons créés à " +
    (playerTransform != null ? playerTransform.position.ToString() : "playerTransform NULL"));
    }

    public GameObject GetInactiveFish()
    {
        for (int i = 0; i < pooledFish.Count; i++)
            if (!pooledFish[i].activeInHierarchy) return pooledFish[i];

        Vector3 center = playerTransform != null
            ? playerTransform.position
            : new Vector3(25f, 13f, 25f);

        Vector3 spawnPos = center + new Vector3(
            Random.Range(-spawnRadius, spawnRadius),
            Random.Range(minHeight, maxHeight),
            Random.Range(-spawnRadius, spawnRadius));

        var newFish = Instantiate(
            fishPrefabs[Random.Range(0, fishPrefabs.Length)],
            spawnPos, Quaternion.identity);
        newFish.SetActive(false);
        pooledFish.Add(newFish);
        return newFish;
    }
}