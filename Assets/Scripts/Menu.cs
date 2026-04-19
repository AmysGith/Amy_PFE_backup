using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Boutons du menu (dans l'ordre)")]
    public Button[] menuButtons;
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    [Header("Répétition de la navigation")]
    public float repeatDelay = 0.4f;

    private int _selected = 0;
    private float _nextNav = 0f;
    private string _lastDir = "NEUTRE";

    private void Start() => Highlight(_selected);

    private void Update()
    {
        if (ArduinoManager.Instance == null) return;

        string dir = ArduinoManager.Instance.Direction;

        if (Time.time >= _nextNav)
        {
            if (dir == "AVANT" && _lastDir != "AVANT") Navigate(-1);
            if (dir == "ARRIERE" && _lastDir != "ARRIERE") Navigate(+1);

            if ((dir == "AVANT" || dir == "ARRIERE") && _lastDir == dir)
            {
                Navigate(dir == "AVANT" ? -1 : +1);
                _nextNav = Time.time + repeatDelay;
            }
            else if (dir != _lastDir)
            {
                _nextNav = Time.time + repeatDelay * 1.5f;
            }
        }
        _lastDir = dir;

        if (ArduinoManager.Instance.ButtonDown)
            menuButtons[_selected].onClick.Invoke();
    }

    private void Navigate(int delta)
    {
        _selected = Mathf.Clamp(_selected + delta, 0, menuButtons.Length - 1);
        Highlight(_selected);
        _nextNav = Time.time + repeatDelay;
    }

    private void Highlight(int index)
    {
        for (int i = 0; i < menuButtons.Length; i++)
        {
            var colors = menuButtons[i].colors;
            colors.normalColor = (i == index) ? selectedColor : normalColor;
            menuButtons[i].colors = colors;
        }
    }

    public void OnPlay() => SceneManager.LoadScene("scene1");
    public void OnQuit() => Application.Quit();
}