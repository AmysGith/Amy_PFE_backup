using System.IO.Ports;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class ArduinoHybridMovement : MonoBehaviour
{
    public float moveSpeed = 4f;
    public float turnSpeed = 90f;
    public float verticalSpeed = 3f;
    public Transform cameraTransform;
    public string portName = "COM3";
    public int baudRate = 9600;
    public bool useArduino = true;
    public bool useInputSystem = true;

    private CharacterController controller;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction ascendAction;
    private InputAction descendAction;
    private InputAction sprintAction;

    private SerialPort serial;
    private Thread serialThread;
    private volatile string arduinoDir = "NEUTRE";
    private volatile bool arduinoBtn = false;
    private readonly object serialLock = new object();

    // Lissage du mouvement
    private Vector3 currentVelocity;
    public float smoothTime = 0.1f;
    private Vector3 moveVelocityRef;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        ascendAction = playerInput.actions["Jump"];
        descendAction = playerInput.actions["Crouch"];
        sprintAction = playerInput.actions["Sprint"];

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (useArduino)
        {
            try
            {
                serial = new SerialPort(portName, baudRate);
                serial.ReadTimeout = 100;
                serial.Open();

                // Thread dédié à la lecture série
                serialThread = new Thread(ReadSerialLoop);
                serialThread.IsBackground = true;
                serialThread.Start();
            }
            catch { useArduino = false; }
        }
    }

    // Tourne en arrière-plan, ne bloque plus Update()
    private void ReadSerialLoop()
    {
        while (serial != null && serial.IsOpen)
        {
            try
            {
                string line = serial.ReadLine();
                if (!string.IsNullOrEmpty(line))
                {
                    string dir = "NEUTRE";
                    bool btn = false;
                    string[] parts = line.Split(' ');
                    foreach (string part in parts)
                    {
                        if (part.StartsWith("DIR:")) dir = part.Substring(4).Trim();
                        else if (part.StartsWith("BTN:")) btn = part.Substring(4).Trim() == "APPUYE";
                    }
                    lock (serialLock)
                    {
                        arduinoDir = dir;
                        arduinoBtn = btn;
                    }
                }
            }
            catch (System.TimeoutException) { /* normal */ }
            catch { break; }
        }
    }

    private void Update()
    {
        float moveInput = 0f;
        float turnInput = 0f;
        float ascend = 0f;
        bool sprinting = false;

        if (useInputSystem)
        {
            Vector2 input = moveAction.ReadValue<Vector2>();
            moveInput += input.y;
            turnInput += input.x;
            ascend += ascendAction.IsPressed() ? 1f : 0f;
            ascend -= descendAction.IsPressed() ? 1f : 0f;
            sprinting = sprintAction.IsPressed();
        }

        if (useArduino)
        {
            string dir;
            bool btn;
            lock (serialLock) { dir = arduinoDir; btn = arduinoBtn; }

            switch (dir)
            {
                case "AVANT": moveInput += 1f; break;
                case "ARRIERE": moveInput -= 1f; break;
                case "GAUCHE": turnInput -= 1f; break;
                case "DROITE": turnInput += 1f; break;
            }
            if (btn) ascend += 1f;
        }

        // Clamp pour éviter les valeurs > 1 si Arduino + clavier simultanés
        moveInput = Mathf.Clamp(moveInput, -1f, 1f);
        turnInput = Mathf.Clamp(turnInput, -1f, 1f);

        // Rotation fluide
        if (turnInput != 0f)
            transform.Rotate(Vector3.up, turnInput * turnSpeed * Time.deltaTime);

        // Déplacement avec lissage (SmoothDamp)
        float speed = moveSpeed * (sprinting ? 1.5f : 1f);
        Vector3 targetVelocity = transform.forward * moveInput * speed;
        targetVelocity.y = ascend * verticalSpeed;

        currentVelocity = Vector3.SmoothDamp(
            currentVelocity, targetVelocity, ref moveVelocityRef, smoothTime);

        controller.Move(currentVelocity * Time.deltaTime);
    }

    private void OnApplicationQuit()
    {
        serialThread?.Abort();
        if (serial != null && serial.IsOpen) serial.Close();
    }
}