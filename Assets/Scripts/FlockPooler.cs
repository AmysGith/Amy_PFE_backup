using System.Collections.Generic;
using UnityEngine;

public class FlockPooler : MonoBehaviour
{
    public static FlockPooler Instance;

    public GameObject[] fishPrefabs;
    public int numFishPerPrefab = 10;
    public List<GameObject> pooledFish = new List<GameObject>();
    public Transform playerTransform;

    void Awake()
    {
        Instance = this;

        if (fishPrefabs == null || fishPrefabs.Length == 0)
        {
            Debug.LogError("[FlockPooler] Aucun prefab assigné !");
            return;
        }

        // Tous les poissons spawned SUR le joueur.
        // FlockManager les disperse dès la première frame via les wanderDirs.
        Vector3 spawnPos = playerTransform != null
            ? playerTransform.position
            : Vector3.zero;

        foreach (var prefab in fishPrefabs)
        {
            for (int i = 0; i < numFishPerPrefab; i++)
            {
                var fish = Instantiate(prefab, spawnPos, Quaternion.identity);
                fish.SetActive(false);
                pooledFish.Add(fish);
            }
        }

        Debug.Log($"[FlockPooler] {pooledFish.Count} poissons créés sur le joueur ({spawnPos}).");
    }

    public GameObject GetInactiveFish()
    {
        foreach (var f in pooledFish)
            if (!f.activeInHierarchy) return f;

        Vector3 spawnPos = playerTransform != null
            ? playerTransform.position
            : Vector3.zero;

        var newFish = Instantiate(
            fishPrefabs[Random.Range(0, fishPrefabs.Length)],
            spawnPos, Quaternion.identity);
        newFish.SetActive(false);
        pooledFish.Add(newFish);
        return newFish;
    }
}