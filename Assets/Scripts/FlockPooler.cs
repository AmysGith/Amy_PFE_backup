using System.Collections.Generic;
using UnityEngine;

public class FlockPooler : MonoBehaviour
{
    public static FlockPooler Instance;
    public GameObject[] fishPrefabs;
    public int numFishPerPrefab = 10;
    public List<GameObject> pooledFish = new List<GameObject>();


    void Awake()
    {
        Instance = this;

        if (fishPrefabs == null || fishPrefabs.Length == 0)
        {
            Debug.LogError("FlockPooler: Aucun prefab assigné !");
            return;
        }

        foreach (var prefab in fishPrefabs)
            for (int i = 0; i < numFishPerPrefab; i++)
            {
                var fish = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                fish.SetActive(false);
                pooledFish.Add(fish);
            }
    }


    // Retourne le prochain poisson inactif, en étend le pool si besoin
    public GameObject GetInactiveFish()
    {
        for (int i = 0; i < pooledFish.Count; i++)
            if (!pooledFish[i].activeInHierarchy) return pooledFish[i];

        var newFish = Instantiate(
            fishPrefabs[Random.Range(0, fishPrefabs.Length)],
            Vector3.zero, Quaternion.identity);
        newFish.SetActive(false);
        pooledFish.Add(newFish);
        return newFish;
    }
}