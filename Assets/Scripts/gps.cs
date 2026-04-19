using System.Collections.Generic;
using UnityEngine;

public class MiniMap : MonoBehaviour
{
    [Header("Références")]
    public Transform player;
    public POIRegistry poiRegistry;

    [Header("Radar settings")]
    public float chunkSize = 50f;
    public float radarRange = 300f; // distance max affichée = bord du radar
    public float updateRate = 0.1f;

    private float timer;

    void Start()
    {
        Debug.Log("MiniMap OLED START OK");
    }

    void Update()
    {
        if (player == null || ArduinoManager.Instance == null || poiRegistry == null)
            return;


        Debug.Log($"player={player != null} | arduino={ArduinoManager.Instance != null} | poi={poiRegistry != null}");
        timer += Time.deltaTime;
        if (timer < updateRate) return;
        timer = 0;

        // ── HEADER ───────────────────────────
        string msg = $"A:{player.eulerAngles.y:F1}|S:{radarRange:F0}";
        int count = 0;

        // ── POI ──────────────────────────────
        foreach (var kv in poiRegistry.GetAllPOIs())
        {
            if (count >= 5) break;

            Vector2Int chunk = kv.Key;

            Vector3 worldPos = new Vector3(
                chunk.x * chunkSize,
                0f,
                chunk.y * chunkSize
            );

            Vector3 diff = worldPos - player.position;

            // pas de radarScale ici, l'Arduino s'en charge avec S:
            msg += $"|P:{diff.x:F0},{diff.z:F0}";
            count++;
        }

        ArduinoManager.Instance.SendRadar(msg);
        Debug.Log($"RADAR SEND ({count} POIs) → " + msg);
    }
}