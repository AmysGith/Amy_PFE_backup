using UnityEngine;

public class POIVisitTrigger : MonoBehaviour
{
    [HideInInspector] public Vector2Int coord;
    [HideInInspector] public string fact;
    [HideInInspector] public string title;
    [HideInInspector] public Sprite factSprite;

    private bool visited = false;
    private bool playerInside = false;
    private bool panelOpen = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = true;
        POIUIManager.Instance?.ShowHint();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = false;
        panelOpen = false;
        POIUIManager.Instance?.HideAll();

        if (!visited)
        {
            visited = true;
            GameCompletionManager.Instance?.NotifyPOIVisited(coord);
        }
    }

    private void Update()
    {
        if (!playerInside) return;

        bool factButton = ArduinoManager.Instance != null
            ? ArduinoManager.Instance.FactPressed
            : Input.GetKeyDown(KeyCode.Space); 
        if (!factButton) return;

        if (!panelOpen)
        {
            panelOpen = true;
            POIUIManager.Instance?.ShowFact(factSprite);
        }
        else
        {
            panelOpen = false;
            POIUIManager.Instance?.ShowHint();
        }
    }
}