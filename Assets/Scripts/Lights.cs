using UnityEngine;

public class FirstPersonHeadlights : MonoBehaviour
{
    public Light leftLight;
    public Light rightLight;
    public Transform cameraTransform;


    bool isOn = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            isOn = !isOn;
            leftLight.enabled = isOn;
            rightLight.enabled = isOn;
        }

        if (!isOn) return;


    }

}