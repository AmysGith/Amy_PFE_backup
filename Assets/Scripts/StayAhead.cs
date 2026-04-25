using UnityEngine;

public class FlockManagerStayAhead : MonoBehaviour
{
    public Transform player;
    public float distanceAhead = 5.0f;
    public float verticalOffset = 0.0f;

    void Update()
    {
        if (player == null)
        {
            Debug.LogError("Aucun joueur assigné au FlockManagerStayAhead !");
            return;
        }

        Vector3 positionAhead = player.position + player.forward * distanceAhead;
        positionAhead.y += verticalOffset;

        transform.position = positionAhead;
        transform.rotation = player.rotation;
    }
}