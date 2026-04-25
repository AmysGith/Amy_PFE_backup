using UnityEngine;


[System.Serializable]
public class FishGroup
{
    public GameObject prefab;
    public int count;
}

public class FlockManager : MonoBehaviour
{
    public static FlockManager FM;

    public FishGroup[] fishGroups;  // un prefab + un count par groupe
    public GameObject[] allFish;
    public Vector3 swimLimits = new Vector3(5.0f, 5.0f, 5.0f);
    public Vector3 goalPos = Vector3.zero;

    [Header("Fish Settings")]
    [Range(0.1f, 8.0f)] public float minSpeed;
    [Range(0.0f, 8.0f)] public float maxSpeed;
    [Range(1.0f, 10.0f)] public float neighbourDistance;
    [Range(1.0f, 5.0f)] public float rotationSpeed;

    void Awake()
    {
        FM = this;
    }

    void Start()
    {
        // Calcule le total de poissons
        int totalFish = 0;
        foreach (var group in fishGroups)
            totalFish += group.count;

        allFish = new GameObject[totalFish];
        int index = 0;

        foreach (var group in fishGroups)
        {
            for (int i = 0; i < group.count; i++)
            {
                Vector3 pos = this.transform.position + new Vector3(
                    Random.Range(-swimLimits.x, swimLimits.x),
                    Random.Range(-swimLimits.y, swimLimits.y),
                    Random.Range(-swimLimits.z, swimLimits.z));

                allFish[index] = Instantiate(group.prefab, pos, Quaternion.identity);
                index++;
            }
        }

        goalPos = this.transform.position;
    }

    void Update()
    {
        if (Random.Range(0, 100) < 10)
        {
            goalPos = this.transform.position + new Vector3(
                Random.Range(-swimLimits.x, swimLimits.x),
                Random.Range(-swimLimits.y, swimLimits.y),
                Random.Range(-swimLimits.z, swimLimits.z));
        }
    }
}