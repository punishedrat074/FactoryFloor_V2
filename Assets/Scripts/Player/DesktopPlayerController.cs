using UnityEngine;
using UnityEngine.InputSystem;

public class DesktopPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float gravity = -20f;

    [Header("Look")]
    public float mouseSensitivity = 0.1f;
    public Transform cameraTransform;
    public float maxLookAngle = 85f;

    private CharacterController controller;
    private Vector3 velocity;
    private float cameraPitch = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMovement();
        HandleLook();

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void HandleMovement()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed)
                input.y += 1;

            if (Keyboard.current.sKey.isPressed)
                input.y -= 1;

            if (Keyboard.current.dKey.isPressed)
                input.x += 1;

            if (Keyboard.current.aKey.isPressed)
                input.x -= 1;
        }

        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 move =
            transform.right * input.x +
            transform.forward * input.y;

        controller.Move(
            move * moveSpeed * Time.deltaTime
        );

        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;

        controller.Move(
            velocity * Time.deltaTime
        );
    }

    void HandleLook()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouseDelta =
            Mouse.current.delta.ReadValue();

        float mouseX =
            mouseDelta.x * mouseSensitivity;

        float mouseY =
            mouseDelta.y * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;

        cameraPitch =
            Mathf.Clamp(
                cameraPitch,
                -maxLookAngle,
                maxLookAngle
            );

        cameraTransform.localRotation =
            Quaternion.Euler(
                cameraPitch,
                0f,
                0f
            );
    }
}