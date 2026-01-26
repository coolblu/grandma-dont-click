using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class FirstPersonTouchInput : MonoBehaviour, IFirstPersonInputSource
{
    [Header("References")]
    [SerializeField] private PlayerInput playerInput;

    [Header("Options")]
    [SerializeField] private bool enableMobileInput = true;
    [SerializeField] private bool forceMobileInEditor = false;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 0.12f;
    [SerializeField] private float touchLookSensitivity = 0.08f;

    [Header("Move")]
    [SerializeField] private float touchMoveRadius = 80f;
    [SerializeField] private float touchMoveDeadzone = 8f;
    [SerializeField, Range(0.1f, 0.9f)] private float touchMoveArea = 0.5f;

    [Header("Tap")]
    [SerializeField] private float touchTapMaxTime = 0.25f;
    [SerializeField] private float touchTapMaxMove = 12f;

    [Header("Buttons")]
    [SerializeField] private bool touchJumpEnabled = true;
    [SerializeField] private Rect touchJumpZoneNormalized = new Rect(0.78f, 0.62f, 0.2f, 0.34f);
    [SerializeField] private bool touchSprintEnabled = false;
    [SerializeField] private Rect touchSprintZoneNormalized = new Rect(0.78f, 0.28f, 0.2f, 0.28f);

    [Header("Joystick UI")]
    [SerializeField] private RectTransform joystickRoot;
    [SerializeField] private RectTransform joystickHandle;

    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;

    private int moveFingerId = -1;
    private int lookFingerId = -1;
    private int jumpFingerId = -1;
    private int sprintFingerId = -1;
    private Vector2 moveFingerStartPos;
    private Vector2 lookFingerLastPos;
    private Vector2 touchMoveInput;
    private Vector2 touchLookInput;
    private bool touchMoveActive;
    private bool touchLookActive;
    private bool touchJumpPressed;
    private bool touchSprintHeld;
    private float jumpFingerStartTime;
    private Vector2 jumpFingerStartPos;
    private Canvas joystickCanvas;
    private RectTransform joystickRootParent;
    private RectTransform joystickCanvasRoot;
    private Vector2 joystickRestAnchoredPosition;
    private Vector3 joystickRestWorldPosition;
    private bool joystickRestCached;

    private void Awake()
    {
        CacheActions();
        CacheJoystickReferences();
        HideJoystick();
    }

    private void OnEnable()
    {
        CacheJoystickReferences();

        if (!ShouldUse())
        {
            HideJoystick();
            return;
        }

        EnsureTouchSupport();
        if (forceMobileInEditor && Application.isEditor) TouchSimulation.Enable();
        ShowJoystickAtRest();
    }

    private void OnDisable()
    {
        if (forceMobileInEditor && Application.isEditor) TouchSimulation.Disable();
        HideJoystick();
    }

    private void Update()
    {
        if (!ShouldUse())
        {
            HideJoystick();
            return;
        }

        if (joystickRoot != null && !joystickRoot.gameObject.activeSelf)
        {
            ShowJoystickAtRest();
        }
    }

    public bool ShouldUse()
    {
        if (!enableMobileInput) return false;
        if (Application.isMobilePlatform) return true;
        return forceMobileInEditor && Application.isEditor;
    }

    public FirstPersonInputFrame ReadInput()
    {
        if (!ShouldUse())
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

        touchMoveInput = Vector2.zero;
        touchLookInput = Vector2.zero;
        touchMoveActive = false;
        touchLookActive = false;
        touchJumpPressed = false;
        touchSprintHeld = false;

        EnsureTouchSupport();
        ReadTouchInput();

        return new FirstPersonInputFrame
        {
            Move = touchMoveActive ? touchMoveInput : Vector2.zero,
            Look = touchLookActive ? touchLookInput * touchLookSensitivity : Vector2.zero,
            JumpPressed = touchJumpPressed,
            SprintHeld = touchSprintHeld
        };
    }

    private void EnsureTouchSupport()
    {
        if (!EnhancedTouchSupport.enabled) EnhancedTouchSupport.Enable();
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

    private void ReadTouchInput()
    {
        if (Touchscreen.current == null) return;

        var activeTouches = Touch.activeTouches;
        for (int i = 0; i < activeTouches.Count; i++)
        {
            var touch = activeTouches[i];
            if (touch.began)
            {
                RegisterTouchBegin(touch);
            }
            else if (touch.ended)
            {
                RegisterTouchEnd(touch);
            }
        }

        for (int i = 0; i < activeTouches.Count; i++)
        {
            var touch = activeTouches[i];
            if (!touch.inProgress) continue;

            int fingerId = touch.finger.index;
            Vector2 pos = touch.screenPosition;

            if (fingerId == moveFingerId)
            {
                touchMoveActive = true;
                touchMoveInput = CalculateMoveInput(pos);
                continue;
            }

            if (fingerId == lookFingerId)
            {
                touchLookActive = true;
                Vector2 delta = pos - lookFingerLastPos;
                lookFingerLastPos = pos;
                touchLookInput = delta;
                continue;
            }

            if (fingerId == sprintFingerId)
            {
                touchSprintHeld = true;
            }
        }

        if (moveFingerId != -1 && !touchMoveActive) moveFingerId = -1;
        if (lookFingerId != -1 && !touchLookActive) lookFingerId = -1;
        if (sprintFingerId != -1 && !touchSprintHeld) sprintFingerId = -1;
    }

    private void RegisterTouchBegin(Touch touch)
    {
        int fingerId = touch.finger.index;
        Vector2 pos = touch.screenPosition;

        if (touchJumpEnabled && jumpFingerId == -1 && IsInZone(pos, touchJumpZoneNormalized))
        {
            jumpFingerId = fingerId;
            jumpFingerStartTime = Time.unscaledTime;
            jumpFingerStartPos = pos;
            return;
        }

        if (touchSprintEnabled && sprintFingerId == -1 && IsInZone(pos, touchSprintZoneNormalized))
        {
            sprintFingerId = fingerId;
            return;
        }

        float split = GetMoveSplitPosition();
        if (pos.x <= split)
        {
            if (moveFingerId == -1)
            {
                moveFingerId = fingerId;
                moveFingerStartPos = pos;
                ShowJoystick(pos);
                return;
            }
        }

        if (lookFingerId == -1)
        {
            lookFingerId = fingerId;
            lookFingerLastPos = pos;
        }
    }

    private void RegisterTouchEnd(Touch touch)
    {
        int fingerId = touch.finger.index;
        Vector2 pos = touch.screenPosition;

        if (fingerId == moveFingerId)
        {
            moveFingerId = -1;
            touchMoveInput = Vector2.zero;
            touchMoveActive = false;
            ShowJoystickAtRest();
        }

        if (fingerId == lookFingerId)
        {
            lookFingerId = -1;
            touchLookInput = Vector2.zero;
            touchLookActive = false;
        }

        if (fingerId == sprintFingerId)
        {
            sprintFingerId = -1;
            touchSprintHeld = false;
        }

        if (fingerId == jumpFingerId)
        {
            float tapTime = Time.unscaledTime - jumpFingerStartTime;
            float maxMove = GetScaledTapMove();
            if (tapTime <= touchTapMaxTime && (pos - jumpFingerStartPos).sqrMagnitude <= maxMove * maxMove)
            {
                touchJumpPressed = true;
            }

            jumpFingerId = -1;
        }
    }

    private Vector2 CalculateMoveInput(Vector2 pos)
    {
        if (joystickRoot != null)
        {
            if (TryGetJoystickLocalPoint(pos, out var localPoint))
            {
                float range = GetJoystickHandleRange();
                Vector2 joystickClamped = Vector2.ClampMagnitude(localPoint, range);
                if (joystickHandle != null) joystickHandle.anchoredPosition = joystickClamped;

                float joystickDeadzone = GetJoystickDeadzone(range);
                return NormalizeMoveInput(joystickClamped, joystickDeadzone, range);
            }

            return Vector2.zero;
        }

        float radius = GetScaledMoveRadius();
        if (radius <= 0f) return Vector2.zero;

        float deadzone = Mathf.Clamp(GetScaledDeadzone(), 0f, radius - 1f);
        Vector2 delta = pos - moveFingerStartPos;
        Vector2 clamped = Vector2.ClampMagnitude(delta, radius);

        return NormalizeMoveInput(clamped, deadzone, radius);
    }

    private bool IsInZone(Vector2 screenPos, Rect normalizedRect)
    {
        if (Screen.width <= 0 || Screen.height <= 0) return false;
        Vector2 normalized = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
        return normalizedRect.Contains(normalized);
    }

    private float GetMoveSplitPosition()
    {
        float split = Mathf.Clamp01(touchMoveArea);
        return Screen.width * split;
    }

    private float GetScaledMoveRadius()
    {
        return Mathf.Max(1f, touchMoveRadius * GetDpiScale());
    }

    private float GetScaledDeadzone()
    {
        return Mathf.Max(0f, touchMoveDeadzone * GetDpiScale());
    }

    private float GetScaledTapMove()
    {
        return Mathf.Max(0f, touchTapMaxMove * GetDpiScale());
    }

    private float GetDpiScale()
    {
        float dpi = Screen.dpi;
        if (dpi <= 0f) return 1f;
        return dpi / 160f;
    }

    private void CacheJoystickReferences()
    {
        if (joystickRoot == null) return;
        joystickRootParent = joystickRoot.parent as RectTransform;
        joystickCanvas = joystickRoot.GetComponentInParent<Canvas>();
        joystickCanvasRoot = joystickCanvas != null ? joystickCanvas.transform as RectTransform : null;
        CacheJoystickRestPosition();
    }

    private void ShowJoystick(Vector2 screenPos)
    {
        if (!ShouldUse()) return;
        if (joystickRoot == null) return;

        if (joystickRootParent == null) joystickRootParent = joystickRoot.parent as RectTransform;
        if (joystickCanvas == null) joystickCanvas = joystickRoot.GetComponentInParent<Canvas>();
        if (joystickCanvasRoot == null && joystickCanvas != null) joystickCanvasRoot = joystickCanvas.transform as RectTransform;

        RectTransform screenRoot = GetJoystickScreenRoot();
        if (screenRoot != null)
        {
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                screenRoot,
                screenPos,
                GetJoystickCamera(),
                out var worldPoint
            ))
            {
                joystickRoot.position = worldPoint;
            }
        }

        if (!joystickRoot.gameObject.activeSelf) joystickRoot.gameObject.SetActive(true);
        joystickRoot.SetAsLastSibling();
        if (joystickHandle != null) joystickHandle.anchoredPosition = Vector2.zero;
    }

    private void ShowJoystickAtRest()
    {
        if (!ShouldUse()) return;
        if (joystickRoot == null) return;

        if (!joystickRoot.gameObject.activeSelf) joystickRoot.gameObject.SetActive(true);
        RestoreJoystickRestPosition();
        joystickRoot.SetAsLastSibling();
        ResetJoystickHandle();
    }

    private void HideJoystick()
    {
        if (joystickRoot == null) return;
        ResetJoystickHandle();
        if (joystickRoot.gameObject.activeSelf) joystickRoot.gameObject.SetActive(false);
    }

    private void ResetJoystickHandle()
    {
        if (joystickHandle != null) joystickHandle.anchoredPosition = Vector2.zero;
    }

    private void CacheJoystickRestPosition()
    {
        if (joystickRestCached || joystickRoot == null) return;
        joystickRestAnchoredPosition = joystickRoot.anchoredPosition;
        joystickRestWorldPosition = joystickRoot.position;
        joystickRestCached = true;
    }

    private void RestoreJoystickRestPosition()
    {
        if (!joystickRestCached) CacheJoystickRestPosition();
        if (joystickRoot == null) return;

        if (joystickRootParent != null)
        {
            joystickRoot.anchoredPosition = joystickRestAnchoredPosition;
        }
        else
        {
            joystickRoot.position = joystickRestWorldPosition;
        }
    }

    private float GetJoystickHandleRange()
    {
        if (joystickRoot == null) return GetScaledMoveRadius();
        float radius = Mathf.Min(joystickRoot.rect.width, joystickRoot.rect.height) * 0.5f;
        return radius > 0f ? radius : GetScaledMoveRadius();
    }

    private float GetJoystickDeadzone(float range)
    {
        if (touchMoveRadius <= 0f) return 0f;
        float ratio = Mathf.Clamp01(touchMoveDeadzone / touchMoveRadius);
        return range * ratio;
    }

    private Vector2 NormalizeMoveInput(Vector2 delta, float deadzone, float radius)
    {
        float magnitude = delta.magnitude;
        if (magnitude <= deadzone || radius <= deadzone + 0.001f) return Vector2.zero;

        float normalized = (magnitude - deadzone) / (radius - deadzone);
        return delta.normalized * Mathf.Clamp01(normalized);
    }

    private bool TryGetJoystickLocalPoint(Vector2 screenPos, out Vector2 localPoint)
    {
        if (joystickRoot == null)
        {
            localPoint = Vector2.zero;
            return false;
        }

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickRoot,
            screenPos,
            GetJoystickCamera(),
            out localPoint
        );
    }

    private RectTransform GetJoystickScreenRoot()
    {
        if (joystickCanvasRoot != null) return joystickCanvasRoot;
        return joystickRootParent;
    }

    private Camera GetJoystickCamera()
    {
        if (joystickCanvas == null) return null;
        if (joystickCanvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
        if (joystickCanvas.worldCamera != null) return joystickCanvas.worldCamera;
        return Camera.main;
    }
}
