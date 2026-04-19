using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CausticsAnimation : MonoBehaviour
{
    public float speedX = 0.02f;
    public float speedY = 0.01f;

    private UniversalAdditionalLightData lightData;

    void Awake()
    {
        lightData = GetComponent<UniversalAdditionalLightData>();
    }

    void Update()
    {
        if (lightData != null)
        {
            Vector2 offset = new Vector2(
                (Time.time * speedX) % 1f,
                (Time.time * speedY) % 1f
            );

            lightData.lightCookieOffset = offset;
        }
    }
}