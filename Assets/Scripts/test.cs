using UnityEngine;

public class ArduinoTest : MonoBehaviour
{
    void Start()
    {
        Debug.Log("ArduinoManager instance : " + (ArduinoManager.Instance != null));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            Debug.Log("Envoi LCD...");
            ArduinoManager.Instance?.SendLCD("INIT,4");
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log("Envoi BOUNDS 2...");
            ArduinoManager.Instance?.SendBounds('2');
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("Envoi BOUNDS 0...");
            ArduinoManager.Instance?.SendBounds('0');
        }
    }
}