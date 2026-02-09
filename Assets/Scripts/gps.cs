using System.Collections.Generic;
using UnityEngine;

public class MiniMap : MonoBehaviour
{
    [Header("Références")]
    public Transform player;                 // Le joueur à suivre
    public RectTransform mapRect;            // RectTransform de la map UI
    public GameObject playerIconPrefab;      // Icône du joueur
    public GameObject poiIconPrefab;         // Icône des POIs
    public POIRegistry poiRegistry;          // Référence au POIRegistry
    public float worldToMapScale = 1f;       // Échelle conversion monde -> map

    private RectTransform playerIcon;
    private Dictionary<Vector2Int, RectTransform> poiIcons = new Dictionary<Vector2Int, RectTransform>();

    void Start()
    {
        if (mapRect == null)
        {
            Debug.LogError("MiniMap : mapRect non assigné !");
            return;
        }

        // Crée l'icône joueur
        if (playerIconPrefab != null)
        {
            playerIcon = Instantiate(playerIconPrefab, mapRect).GetComponent<RectTransform>();
        }

        // Crée les icônes pour TOUS les POIs
        if (poiRegistry != null && poiIconPrefab != null)
        {
            foreach (var kv in poiRegistry.GetAllPOIs())
            {
                Vector2Int chunkCoord = kv.Key;
                string regionName = kv.Value;

                // Instancie l'icône
                RectTransform icon = Instantiate(poiIconPrefab, mapRect).GetComponent<RectTransform>();

                // Calcule la position sur la map en prenant la taille du chunk et l'échelle
                Vector2 anchoredPos = new Vector2(
                    chunkCoord.x * 50 * worldToMapScale,  // 50 = chunkSize
                    chunkCoord.y * 50 * worldToMapScale
                );

                icon.anchoredPosition = anchoredPos;
                icon.name = $"POI_{regionName}_{chunkCoord.x}_{chunkCoord.y}";

                poiIcons[chunkCoord] = icon;

                Debug.Log($"MiniMap : POI '{regionName}' affiché à {anchoredPos}");
            }
        }
    }

    void Update()
    {
        if (player == null || playerIcon == null) return;

        // Position du joueur sur la map
        Vector2 playerMapPos = new Vector2(
            player.position.x * worldToMapScale,
            player.position.z * worldToMapScale
        );
        playerIcon.anchoredPosition = playerMapPos;

        // Rotation de l'icône joueur pour suivre l'orientation du joueur
        playerIcon.localRotation = Quaternion.Euler(0f, 0f, -player.eulerAngles.y);
    }
}
