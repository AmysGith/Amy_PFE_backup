using UnityEngine;

public class QuitOnButton : MonoBehaviour
{
    void Start()
    {
        if (ArduinoManager.Instance != null)
            ArduinoManager.Instance.OnQuitPressed += TogglePause;
    }

    void OnDestroy()
    {
        if (ArduinoManager.Instance != null)
            ArduinoManager.Instance.OnQuitPressed -= TogglePause;
    }

    void TogglePause()
    {
        Time.timeScale = Time.timeScale == 0f ? 1f : 0f;
    }
}