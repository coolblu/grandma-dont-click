using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot;

    [SerializeField] private Camera playerCamera;
    [SerializeField] private PlayerInput playerInput;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4.5f;
    [SerializeField] private float sprintSpeed = 7.0f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -25f;

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float maxPitch = 85f;

    [Header("Options")]
    [SerializeField] private bool lockCursor = true;

    [Header("Sprint Feedback")]
    [SerializeField] private bool useSprintFov = true;
    [SerializeField] private float sprintFov = 72f;
    [SerializeField] private float fovLerpSpeed = 12f;

    private CharacterController controller;

    // input vals
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed;
    private bool sprintHeld;

    // state
    private float yaw;
    private float pitch;
    private float verticalVelocity;
    private float baseFov;
    private bool isSprinting;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        CacheActions();

        ApplyCursorLock(lockCursor);

        if (playerCamera != null) baseFov = playerCamera.fieldOfView;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!lockCursor) return;
        ApplyCursorLock(hasFocus);
    }

    void Update()
    {
        UpdateInput();
        HandleLook();
        HandleMove();
        HandleSprintFeedback();
    }

    private void HandleLook()
    {
        yaw += lookInput.x * mouseSensitivity;
        pitch -= lookInput.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (cameraPivot != null) cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMove()
    {
        bool grounded = controller.isGrounded;
        if (grounded && verticalVelocity < 0f) verticalVelocity = -2f;

        isSprinting = sprintHeld && moveInput.sqrMagnitude > 0.01f;
        float speed = isSprinting ? sprintSpeed : walkSpeed;

        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);
        move = Vector3.ClampMagnitude(move, 1f);
        move = transform.TransformDirection(move);

        if (jumpPressed && grounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * speed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);

        jumpPressed = false;
    }

    public bool IsSprinting => isSprinting;

    private static void ApplyCursorLock(bool shouldLock)
    {
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !shouldLock;
    }

    private void UpdateInput()
    {
        if (moveAction == null || lookAction == null || jumpAction == null || sprintAction == null) CacheActions();

        moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        lookInput = lookAction != null ? lookAction.ReadValue<Vector2>() : Vector2.zero;
        jumpPressed = jumpAction != null && jumpAction.WasPressedThisFrame();
        sprintHeld = sprintAction != null && sprintAction.IsPressed();
    }

    private void CacheActions()
    {
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();

        if (playerInput == null || playerInput.actions == null) return;

        var actions = playerInput.actions;
        moveAction = actions.FindAction("Move", false);
        lookAction = actions.FindAction("Look", false);
        jumpAction = actions.FindAction("Jump", false);
        sprintAction = actions.FindAction("Sprint", false);
    }

    private void HandleSprintFeedback()
    {
        if (!useSprintFov || playerCamera == null) return;

        if (baseFov <= 0f) baseFov = playerCamera.fieldOfView;

        float targetFov = isSprinting ? sprintFov : baseFov;
        playerCamera.fieldOfView = Mathf.Lerp(
            playerCamera.fieldOfView,
            targetFov,
            fovLerpSpeed * Time.deltaTime
        );
    }
}
