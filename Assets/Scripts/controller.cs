using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class ArduinoHybridMovement : MonoBehaviour
{
    [Header("Mouvement")]
    public float moveSpeed = 4f;
    public float turnSpeed = 90f;
    public float verticalSpeed = 3f;
    public float smoothTime = 0.1f;

    [Header("Références")]
    public Transform cameraTransform;

    [Header("Options")]
    public bool useArduino = true;
    public bool useInputSystem = true;

    private CharacterController _controller;
    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _ascendAction;
    private InputAction _descendAction;
    private InputAction _sprintAction;

    private Vector3 _currentVelocity;
    private Vector3 _moveVelocityRef;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _playerInput = GetComponent<PlayerInput>();
        _moveAction = _playerInput.actions["Move"];
        _ascendAction = _playerInput.actions["Jump"];
        _descendAction = _playerInput.actions["Crouch"];
        _sprintAction = _playerInput.actions["Sprint"];

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        float moveInput = 0f;
        float turnInput = 0f;
        float ascend = 0f;
        bool sprinting = false;

        if (useInputSystem)
        {
            Vector2 input = _moveAction.ReadValue<Vector2>();
            moveInput += input.y;
            turnInput += input.x;
            ascend += _ascendAction.IsPressed() ? 1f : 0f;
            ascend -= _descendAction.IsPressed() ? 1f : 0f;
            sprinting = _sprintAction.IsPressed();
        }

        if (useArduino && ArduinoManager.Instance != null)
        {
            string dir = ArduinoManager.Instance.Direction;
            bool btn = ArduinoManager.Instance.ButtonPressed;

            switch (dir)
            {
                case "AVANT": moveInput += 1f; break;
                case "ARRIERE": moveInput -= 1f; break;
                case "GAUCHE": turnInput -= 1f; break;
                case "DROITE": turnInput += 1f; break;
            }
            if (btn) ascend += 1f;
        }

        moveInput = Mathf.Clamp(moveInput, -1f, 1f);
        turnInput = Mathf.Clamp(turnInput, -1f, 1f);

        if (turnInput != 0f)
            transform.Rotate(Vector3.up, turnInput * turnSpeed * Time.deltaTime);

        float speed = moveSpeed * (sprinting ? 1.5f : 1f);
        Vector3 targetVelocity = transform.forward * moveInput * speed;
        targetVelocity.y = ascend * verticalSpeed;

        _currentVelocity = Vector3.SmoothDamp(
            _currentVelocity, targetVelocity, ref _moveVelocityRef, smoothTime);

        _controller.Move(_currentVelocity * Time.deltaTime);
    }
}