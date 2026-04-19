using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameCompletionManager : MonoBehaviour
{
    public static GameCompletionManager Instance { get; private set; }

    [Header("Références")]
    public POIRegistry poiRegistry;

    [Header("Scène de destination")]
    public string menuSceneName = "Menu";

    [Header("UI optionnelle")]
    public TMPro.TextMeshProUGUI progressLabel;

    private HashSet<Vector2Int> visitedPOIs = new();
    private int totalPOIs;
    private bool gameEnded;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // GetAllPOIs() est déjà dans POIRegistry, on lit juste le count
        totalPOIs = poiRegistry.GetAllPOIs().Count;
        Debug.Log($"[Completion] {totalPOIs} POIs à visiter.");
        UpdateProgressUI();
    }

    public void NotifyPOIVisited(Vector2Int coord)
    {
        if (gameEnded) return;

        if (visitedPOIs.Add(coord))
        {
            Debug.Log($"[Completion] {visitedPOIs.Count}/{totalPOIs}");
            UpdateProgressUI();

            if (visitedPOIs.Count >= totalPOIs)
                TriggerGameEnd();
        }
    }

    private void TriggerGameEnd()
    {
        gameEnded = true;
        Invoke(nameof(LoadMenu), 1.5f);
    }

    private void LoadMenu() => SceneManager.LoadScene(menuSceneName);

    private void UpdateProgressUI()
    {
        if (progressLabel != null)
            progressLabel.text = $"{visitedPOIs.Count} / {totalPOIs} lieux visités";
    }
}