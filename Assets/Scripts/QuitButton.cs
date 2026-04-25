using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class QuitOnButton : MonoBehaviour
{
    [Header("Panel de pause")]
    public GameObject pausePanel;

    [Header("Boutons (dans l'ordre : Reprendre, Rejouer, Quitter)")]
    public Button[] pauseButtons;
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    [Header("Navigation")]
    public float repeatDelay = 0.4f;

    private bool _isPaused = false;
    private int _selected = 0;
    private float _nextNav = 0f;
    private string _lastDir = "NEUTRE";

    void Start()
    {
        if (ArduinoManager.Instance != null)
            ArduinoManager.Instance.OnQuitPressed += TogglePause;

        pausePanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (ArduinoManager.Instance != null)
            ArduinoManager.Instance.OnQuitPressed -= TogglePause;
    }

    void Update()
    {
        if (!_isPaused || ArduinoManager.Instance == null) return;

        string dir = ArduinoManager.Instance.Direction;

        if (Time.unscaledTime >= _nextNav)
        {
            if (dir == "AVANT" && _lastDir != "AVANT") Navigate(-1);
            if (dir == "ARRIERE" && _lastDir != "ARRIERE") Navigate(+1);
            if ((dir == "AVANT" || dir == "ARRIERE") && _lastDir == dir)
            {
                Navigate(dir == "AVANT" ? -1 : +1);
                _nextNav = Time.unscaledTime + repeatDelay;
            }
            else if (dir != _lastDir)
            {
                _nextNav = Time.unscaledTime + repeatDelay * 1.5f;
            }
        }
        _lastDir = dir;

        if (ArduinoManager.Instance.ButtonDown)
            pauseButtons[_selected].onClick.Invoke();
    }

    void TogglePause()
    {
        _isPaused = !_isPaused;
        Time.timeScale = _isPaused ? 0f : 1f;
        pausePanel.SetActive(_isPaused);

        if (_isPaused)
        {
            _selected = 0;
            _lastDir = "NEUTRE";
            _nextNav = Time.unscaledTime + repeatDelay;
            Highlight(_selected);
        }
    }

    private void Navigate(int delta)
    {
        _selected = Mathf.Clamp(_selected + delta, 0, pauseButtons.Length - 1);
        Highlight(_selected);
        _nextNav = Time.unscaledTime + repeatDelay;
    }

    private void Highlight(int index)
    {
        for (int i = 0; i < pauseButtons.Length; i++)
        {
            var colors = pauseButtons[i].colors;
            colors.normalColor = (i == index) ? selectedColor : normalColor;
            pauseButtons[i].colors = colors;
        }
    }

    public void OnResume() => TogglePause();
    public void OnReplay()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("scene1");
    }
    public void OnQuit()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }
}