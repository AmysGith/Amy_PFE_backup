using UnityEngine;

public class POIVisitTrigger : MonoBehaviour
{
    [HideInInspector] public Vector2Int coord;
    private bool visited;

    private void OnTriggerExit(Collider other)
    {
        if (visited) return;
        if (!other.CompareTag("Player")) return;

        visited = true;
        GameCompletionManager.Instance?.NotifyPOIVisited(coord);
    }
}