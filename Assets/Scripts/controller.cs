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

        Transform reference = cameraTransform != null ? cameraTransform : transform;

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

        if (moveInput.y > 0.01f || Mathf.Abs(moveInput.x) > 0.01f)
        {
            Vector3 lookDir = horizontal;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                                                      Quaternion.LookRotation(lookDir.normalized, Vector3.up),
                                                      10f * Time.deltaTime);
        }
    }
}
