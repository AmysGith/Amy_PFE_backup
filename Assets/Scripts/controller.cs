using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(CharacterController))]
public class SimpleUnderwaterMovement : MonoBehaviour
{
    public float swimSpeed = 4f;
    public float verticalSwimSpeed = 3f;
    public float sprintMultiplier = 1.5f;
    public Transform cameraTransform;

    private PlayerInput playerInput;
    private CharacterController controller;
    private InputAction moveAction;
    private InputAction ascendAction;
    private InputAction descendAction;
    private InputAction sprintAction;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        controller = GetComponent<CharacterController>();

        moveAction = playerInput.actions["Move"];
        ascendAction = playerInput.actions["Jump"];
        descendAction = playerInput.actions["Crouch"];
        sprintAction = playerInput.actions["Sprint"];

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        float ascend = ascendAction.IsPressed() ? 1f : 0f;
        float descend = descendAction.IsPressed() ? 1f : 0f;
        bool sprinting = sprintAction.IsPressed();

        Transform reference;
        if (cameraTransform != null)
            reference = cameraTransform;
        else
            reference = transform;

        Vector3 forward = reference.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 right = reference.right;
        right.y = 0f;
        right.Normalize();

        Vector3 horizontal = forward * moveInput.y + right * moveInput.x;
        Vector3 vertical = Vector3.up * (ascend - descend);
        Vector3 movement = horizontal * swimSpeed + vertical * verticalSwimSpeed;

        if (sprinting)
            movement *= sprintMultiplier;

        controller.Move(movement * Time.deltaTime);

        // Rotation uniquement si le mouvement horizontal est significatif
        if (horizontal.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(horizontal.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 10f * Time.deltaTime);
        }
    }
}
