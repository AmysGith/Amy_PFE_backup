using UnityEngine;

public class FirstPersonHeadlights : MonoBehaviour
{
    public Light leftLight;
    public Light rightLight;

    public Transform cameraTransform; // ta caméra

    public Vector3 offset = new Vector3(0.2f, -0.1f, 0.5f); // position devant caméra

    private bool isOn = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            isOn = !isOn;
            leftLight.enabled = isOn;
            rightLight.enabled = isOn;
        }

        if (!isOn) return;

        UpdateHeadlights();
    }

    void UpdateHeadlights()
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        Vector3 up = cameraTransform.up;

        // position devant la caméra
        Vector3 basePos = cameraTransform.position + cameraTransform.forward * 0.5f;

        leftLight.transform.position = basePos - right * offset.x + up * offset.y;
        rightLight.transform.position = basePos + right * offset.x + up * offset.y;

        // direction des phares = direction caméra
        leftLight.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        rightLight.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }
}