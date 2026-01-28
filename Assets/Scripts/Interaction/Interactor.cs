using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class Interactor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private PlayerInput playerInput;

    [Header("Input")]
    [SerializeField] private string interactActionName = "Interact";

    [Header("Raycast")]
    [SerializeField] private bool usePointerPosition = true;
    [SerializeField] private float maxDistance = 3f;
    [SerializeField] private LayerMask interactableLayers = ~0;
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;
    [SerializeField] private bool ignoreUI = true;

    private InputAction interactAction;
    private InteractableBase currentTarget;

    public Vector3 Origin
    {
        get
        {
            if (usePointerPosition && playerCamera != null) return playerCamera.transform.position;
            if (rayOrigin != null) return rayOrigin.position;
            if (playerCamera != null) return playerCamera.transform.position;
            return transform.position;
        }
    }

    public Vector3 Forward
    {
        get
        {
            if (usePointerPosition && playerCamera != null) return playerCamera.transform.forward;
            if (rayOrigin != null) return rayOrigin.forward;
            if (playerCamera != null) return playerCamera.transform.forward;
            return transform.forward;
        }
    }

    public InteractableBase CurrentTarget => currentTarget;

    private void Awake()
    {
        if (playerInput == null) playerInput = GetComponentInParent<PlayerInput>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        CacheActions();
    }

    private void OnDisable()
    {
        if (currentTarget != null)
        {
            currentTarget.FocusExit(this);
        }
        currentTarget = null;
    }

    private void Update()
    {
        if (interactAction == null) CacheActions();
        UpdateTarget();

        if (!WasInteractTriggered()) return;
        if (ignoreUI && IsPointerOverUI()) return;

        if (currentTarget != null)
        {
            currentTarget.TryInteract(this);
        }
    }

    private void UpdateTarget()
    {
        InteractableBase target = FindTarget();
        if (target == currentTarget) return;

        if (currentTarget != null)
        {
            currentTarget.FocusExit(this);
        }
        currentTarget = target;
        if (currentTarget != null)
        {
            currentTarget.FocusEnter(this);
        }
    }

    private InteractableBase FindTarget()
    {
        if (!TryGetRay(out Ray ray)) return null;

        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayers, triggerInteraction))
        {
            return null;
        }

        return hit.collider.GetComponentInParent<InteractableBase>();
    }


    private bool TryGetRay(out Ray ray)
    {
        if (usePointerPosition && playerCamera != null)
        {
            Vector2 screenPos = GetPointerScreenPosition();
            ray = playerCamera.ScreenPointToRay(screenPos);
            return true;
        }

        Vector3 direction = Forward;
        if (direction.sqrMagnitude <= 0.001f)
        {
            ray = default;
            return false;
        }

        ray = new Ray(Origin, direction);
        return true;
    }

    private void CacheActions()
    {
        if (playerInput == null || playerInput.actions == null)
        {
            interactAction = null;
            return;
        }

        interactAction = playerInput.actions.FindAction(interactActionName, false);
    }

    private bool WasInteractTriggered()
    {
        if (interactAction == null) return false;
        return interactAction.triggered;
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject();
    }

    private Vector2 GetPointerScreenPosition()
    {
        if (Pointer.current != null)
        {
            Vector2 position = Pointer.current.position.ReadValue();
            if (IsFinite(position))
            {
                return position;
            }
        }

        return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
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
