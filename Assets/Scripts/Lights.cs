using UnityEngine;

public class FirstPersonHeadlights : MonoBehaviour
{
    [Header("Lumières")]
    public Light leftLight;
    public Light rightLight;

    // Fallback clavier si Arduino non connecté
    [Header("Fallback clavier")]
    public bool allowKeyboardFallback = true;

    private bool _isOn = false;
    private bool _prevSwitchState = false;

    void Update()
    {
        bool switchOn = false;
        bool arduinoAvailable = ArduinoManager.Instance != null;

        if (arduinoAvailable)
        {
            switchOn = ArduinoManager.Instance.SwitchOn;

            // On allume/éteint uniquement sur changement d'état du switch
            if (switchOn != _prevSwitchState)
            {
                _isOn = switchOn;
                _prevSwitchState = switchOn;
            }
        }

        // Fallback clavier L si Arduino absent ou option activée
        if (allowKeyboardFallback && Input.GetKeyDown(KeyCode.L))
        {
            _isOn = !_isOn;
        }

        leftLight.enabled = _isOn;
        rightLight.enabled = _isOn;
    }
}