using UnityEngine;
using UnityEngine.Rendering.Universal;

public class FollowPlayerDecal : MonoBehaviour
{
    public Transform player;
    public float heightOffset = 8f;

    private DecalProjector projector;

    void Awake()
    {
        projector = GetComponent<DecalProjector>();
        projector.size = new Vector3(20f, 20f, 20f);
    }

    void Update()
    {
        transform.position = new Vector3(
            player.position.x,
            heightOffset,
            player.position.z
        );
    }
}