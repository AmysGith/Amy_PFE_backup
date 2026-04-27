using System.IO.Ports;
using System.Threading;
using UnityEngine;

public class ArduinoManager : MonoBehaviour
{
    public static ArduinoManager Instance { get; private set; }

    [Header("Port série")]
    public string portName = "COM6";
    public int baudRate = 9600;

    public string Direction { get; private set; } = "NEUTRE";
    public bool ButtonPressed { get; private set; }
    public bool ButtonDown { get; private set; }
    public bool SwitchOn { get; private set; } = false;

    public bool FactPressed { get; private set; }
    private bool _pendingFact;

    private bool _btnPrev;
    private SerialPort _serial;
    private Thread _thread;
    private bool _running = false;
    private readonly object _lock = new object();

    private string _pendingDir = "NEUTRE";
    private bool _pendingBtn;
    private bool _pendingSwitch;
    private bool _pendingQuit;

    public bool QuitPressed { get; private set; }
    public event System.Action OnQuitPressed;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        try
        {
            _serial = new SerialPort(portName, baudRate);
            _serial.ReadTimeout = 100;
            _serial.Open();
            _running = true;
            _thread = new Thread(ReadLoop);
            _thread.IsBackground = true;
            _thread.Start();
        }
        catch (System.Exception e) { Debug.LogWarning("Arduino non disponible : " + e.Message); }
    }

    void ReadLoop()
    {
        while (_running && _serial != null && _serial.IsOpen)
        {
            try
            {
                string line = _serial.ReadLine();
                if (string.IsNullOrEmpty(line)) continue;
                line = line.Trim();

                if (line.StartsWith("JOY|"))
                {
                    string[] parts = line.Split('|');
                    if (parts.Length >= 3)
                    {
                        string dir = "NEUTRE";
                        bool btn = false;
                        foreach (string p in parts)
                        {
                            if (p.StartsWith("DIR:")) dir = p.Substring(4);
                            if (p.StartsWith("BTN:")) btn = p.Substring(4) == "APPUYE";
                        }
                        lock (_lock) { _pendingDir = dir; _pendingBtn = btn; }
                    }
                }
                else if (line.StartsWith("SWITCH|"))
                {
                    string val = line.Substring(7).Trim();
                    lock (_lock) { _pendingSwitch = (val == "1"); }
                }
                else if (line == "QUIT")
                {
                    lock (_lock) { _pendingQuit = true; }
                }
                else if (line == "FACT")
                {
                    lock (_lock) { _pendingFact = true; }
                }
            }
            catch (System.TimeoutException) { }
            catch (System.Exception e) { Debug.LogWarning("Serial error: " + e.Message); break; }
        }
    }

    void Update()
    {
        FactPressed = false; 
        lock (_lock)
        {
            Direction = _pendingDir;
            bool current = _pendingBtn;
            ButtonDown = current && !_btnPrev;
            ButtonPressed = current;
            _btnPrev = current;
            SwitchOn = _pendingSwitch;
            if (_pendingFact) { FactPressed = true; _pendingFact = false; }
        }
    }

    void OnGUI()
    {
        lock (_lock)
        {
            if (_pendingQuit)
            {
                _pendingQuit = false;
                OnQuitPressed?.Invoke();
            }
        }
    }

    public void SendRadar(string msg)
    {
        if (_serial != null && _serial.IsOpen)
            try { _serial.WriteLine("RADAR|" + msg); }
            catch (System.Exception e) { Debug.LogWarning("SendRadar error: " + e.Message); }
    }

    public void SendLCD(string msg)
    {
        if (_serial != null && _serial.IsOpen)
            try { _serial.WriteLine("LCD|" + msg); }
            catch (System.Exception e) { Debug.LogWarning("SendLCD error: " + e.Message); }
    }

    public void SendBounds(char mode)
    {
        if (_serial != null && _serial.IsOpen)
            try { _serial.WriteLine("BOUNDS|" + mode); }
            catch (System.Exception e) { Debug.LogWarning("SendBounds error: " + e.Message); }
    }

    void OnDisable()
    {
        SendBounds('0'); // Vert + stop buzzer dès l'arrêt du Play
        _running = false;
        if (_thread != null && _thread.IsAlive) _thread.Join(200);
        if (_serial != null && _serial.IsOpen) _serial.Close();
    }

    void OnDestroy() { _running = false; }
}