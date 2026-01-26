using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Camera playerCamera;

    [Header("Input")]
    [SerializeField] private FirstPersonDesktopInput desktopInput;
    [SerializeField] private FirstPersonTouchInput mobileInput;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4.5f;
    [SerializeField] private float sprintSpeed = 7.0f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -25f;

    [Header("Look")]
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

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (desktopInput == null) desktopInput = GetComponent<FirstPersonDesktopInput>();
        if (mobileInput == null) mobileInput = GetComponent<FirstPersonTouchInput>();

        if (!IsUsingMobileInput()) ApplyCursorLock(lockCursor);

        if (playerCamera != null) baseFov = playerCamera.fieldOfView;

        yaw = transform.eulerAngles.y;
        if (cameraPivot != null)
        {
            pitch = Mathf.DeltaAngle(0f, cameraPivot.localEulerAngles.x);
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!lockCursor || IsUsingMobileInput()) return;
        ApplyCursorLock(hasFocus);
    }

    void Update()
    {
        EnforceCursorLock();
        UpdateInput();
        HandleLook();
        HandleMove();
        HandleSprintFeedback();
    }

    private void UpdateInput()
    {
        var source = GetActiveInputSource();
        if (source == null)
        {
            moveInput = Vector2.zero;
            lookInput = Vector2.zero;
            jumpPressed = false;
            sprintHeld = false;
            return;
        }

        FirstPersonInputFrame input = source.ReadInput();
        moveInput = input.Move;
        lookInput = input.Look;
        jumpPressed = input.JumpPressed;
        sprintHeld = input.SprintHeld;

        if (!IsFinite(moveInput)) moveInput = Vector2.zero;
        if (!IsFinite(lookInput)) lookInput = Vector2.zero;
        if (!IsFinite(yaw) || !IsFinite(pitch)) ResetLookState();
    }

    private IFirstPersonInputSource GetActiveInputSource()
    {
        if (mobileInput != null && mobileInput.ShouldUse()) return mobileInput;
        if (desktopInput != null) return desktopInput;
        return null;
    }

    private void HandleLook()
    {
        if (!IsFinite(lookInput))
        {
            ResetLookState();
            return;
        }

        yaw += lookInput.x;
        pitch -= lookInput.y;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

        if (!IsFinite(yaw) || !IsFinite(pitch))
        {
            ResetLookState();
            return;
        }

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

    private void ApplyCursorLock(bool shouldLock)
    {
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !shouldLock;
    }

    private void EnforceCursorLock()
    {
        if (!lockCursor || IsUsingMobileInput()) return;
        if (!Application.isFocused) return;

        if (Cursor.lockState != CursorLockMode.Locked || Cursor.visible)
        {
            ApplyCursorLock(true);
        }
    }

    private bool IsUsingMobileInput()
    {
        if (Application.isMobilePlatform) return true;
        return mobileInput != null && mobileInput.ShouldUse();
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

    private void ResetLookState()
    {
        yaw = transform.eulerAngles.y;
        if (cameraPivot != null)
        {
            pitch = Mathf.DeltaAngle(0f, cameraPivot.localEulerAngles.x);
        }
        else
        {
            pitch = 0f;
        }
    }

    private static bool IsFinite(Vector2 value)
    {
        return IsFinite(value.x) && IsFinite(value.y);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
