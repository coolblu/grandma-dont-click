using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonDesktopInput : MonoBehaviour, IFirstPersonInputSource
{
    [Header("References")]
    [SerializeField] private PlayerInput playerInput;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 0.12f;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    private void Awake()
    {
        CacheActions();
    }

    public FirstPersonInputFrame ReadInput()
    {
        if (moveAction == null || lookAction == null || jumpAction == null || sprintAction == null)
        {
            CacheActions();
        }

        return new FirstPersonInputFrame
        {
            Move = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero,
            Look = lookAction != null ? lookAction.ReadValue<Vector2>() * lookSensitivity : Vector2.zero,
            JumpPressed = jumpAction != null && jumpAction.WasPressedThisFrame(),
            SprintHeld = sprintAction != null && sprintAction.IsPressed()
        };
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
}
