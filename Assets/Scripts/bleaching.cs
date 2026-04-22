using UnityEngine;
public class SimplePOIBleach : MonoBehaviour
{
    public Transform player;
    public float radius = 10f;

    private POIPopulator targetPOI;
    private Vector3 chunkCenter;
    private Vector3 chunkMin;
    private Vector3 chunkMax;

    void Start()
    {
        POIRegistry registry = FindObjectOfType<POIRegistry>();
        foreach (var kvp in registry.GetAllPOIs())
        {
            GameObject instance = registry.GetInstance(kvp.Key);
            if (instance != null && instance.name.Contains("coraux"))
            {
                targetPOI = instance.GetComponent<POIPopulator>();
                break;
            }
        }

        if (targetPOI == null)
        {
            UnityEngine.Debug.LogError("POI_coraux introuvable !");
            return;
        }

        Vector3 origin = targetPOI.transform.position;
        float halfSize = targetPOI.chunkSize * 0.5f;
        chunkCenter = origin + new Vector3(halfSize, 0f, halfSize);
        chunkMin = origin;
        chunkMax = origin + new Vector3(targetPOI.chunkSize, 9999f, targetPOI.chunkSize);

        UnityEngine.Debug.Log($"chunkCenter: {chunkCenter} | chunkMin: {chunkMin} | chunkMax: {chunkMax}");
    }

    void Update()
    {
        if (targetPOI == null || player == null) return;

        bool playerInChunk = player.position.x >= chunkMin.x && player.position.x <= chunkMax.x &&
                             player.position.z >= chunkMin.z && player.position.z <= chunkMax.z;

        if (!playerInChunk)
        {
            targetPOI.bleachProgress = 0f;
            return;
        }

        float dist = Vector3.Distance(new Vector3(player.position.x, 0f, player.position.z),
                                      new Vector3(chunkCenter.x, 0f, chunkCenter.z));
        targetPOI.bleachProgress = dist <= radius ? 1f : 0f;
    }
}