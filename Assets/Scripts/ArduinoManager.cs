using System.IO.Ports;
using System.Threading;
using UnityEngine;

public class ArduinoManager : MonoBehaviour
{
    public static ArduinoManager Instance { get; private set; }

    [Header("Port série")]
    public string portName = "COM6";
    public int baudRate = 9600;

    // ── INPUT JOYSTICK ─────────────────────
    public string Direction { get; private set; } = "NEUTRE";
    public bool ButtonPressed { get; private set; }
    public bool ButtonDown { get; private set; }

    private bool _btnPrev;

    // ── SERIAL ─────────────────────────────
    private SerialPort _serial;
    private Thread _thread;
    private bool _running = false;

    private readonly object _lock = new object();

    private string _pendingDir = "NEUTRE";
    private bool _pendingBtn;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

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
        catch (System.Exception e)
        {
            Debug.LogWarning("Arduino non disponible : " + e.Message);
        }
    }

    // ── THREAD READ ────────────────────────
    void ReadLoop()
    {
        while (_running && _serial != null && _serial.IsOpen)
        {
            try
            {
                string line = _serial.ReadLine();
                if (string.IsNullOrEmpty(line)) continue;

                line = line.Trim();

                // =========================
                // JOYSTICK FROM ARDUINO
                // =========================
                if (line.StartsWith("JOY|"))
                {
                    string[] parts = line.Split('|');

                    if (parts.Length >= 3)
                    {
                        string dir = "NEUTRE";
                        bool btn = false;

                        foreach (string p in parts)
                        {
                            if (p.StartsWith("DIR:"))
                                dir = p.Substring(4);

                            if (p.StartsWith("BTN:"))
                                btn = p.Substring(4) == "APPUYE";
                        }

                        lock (_lock)
                        {
                            _pendingDir = dir;
                            _pendingBtn = btn;
                        }
                    }
                }

                
            }
            catch (System.TimeoutException) { }
            catch (System.Exception e)
            {
                Debug.LogWarning("Serial error: " + e.Message);
                break;
            }
        }
    }

    // ── UNITY UPDATE ───────────────────────
    void Update()
    {
        lock (_lock)
        {
            Direction = _pendingDir;

            bool current = _pendingBtn;

            ButtonDown = current && !_btnPrev;
            ButtonPressed = current;

            _btnPrev = current;
        }
    }

    // ── SEND RADAR TO ARDUINO ──────────────
    public void SendRadar(string msg)
    {
        if (_serial != null && _serial.IsOpen)
        {
            try
            {
                _serial.WriteLine("RADAR|" + msg);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("SendRadar error: " + e.Message);
            }
        }
    }

    // ── CLEANUP ────────────────────────────
    void OnApplicationQuit()
    {
        _running = false;

        if (_thread != null && _thread.IsAlive)
            _thread.Join(200);

        if (_serial != null && _serial.IsOpen)
            _serial.Close();
    }

    void OnDestroy()
    {
        _running = false;
    }
}