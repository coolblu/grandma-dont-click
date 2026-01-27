using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PaperViewerUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private bool hideOnStart = true;

    [Header("UI Refs")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private RawImage paperImage;
    [SerializeField] private AspectRatioFitter aspectFitter;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Zoom")]
    [SerializeField] private float minZoom = 1f;     
    [SerializeField] private float maxZoom = 2.75f;  
    [SerializeField] private float wheelZoomSpeed = 0.15f;
    [SerializeField] private float pinchZoomSpeed = 0.0030f;
    [SerializeField] private bool clickToToggleZoom = true;

    [Header("Gameplay References")]
    [SerializeField] private FirstPersonController firstPersonController;
    [SerializeField] private Interactor interactor;
    [SerializeField] private FirstPersonTouchInput touchInput;

    [Header("Cursor")]
    [SerializeField] private bool unlockCursor = true;

    private bool isOpen;
    private float currentZoom = 1f;

    private bool controllerWasEnabled;
    private bool interactorWasEnabled;
    private bool touchInputWasEnabled;
    private CursorLockMode cachedCursorLock;
    private bool cachedCursorVisible;

    private bool pinching;
    private float lastPinchDist;

    public bool IsOpen => isOpen;

    private RectTransform ContentRect => paperImage != null ? paperImage.rectTransform : null;
    private RectTransform ViewportRect => scrollRect != null ? scrollRect.viewport : null;

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
        if (scrollRect == null) scrollRect = GetComponentInChildren<ScrollRect>(true);
        if (paperImage == null) paperImage = GetComponentInChildren<RawImage>(true);
        if (aspectFitter == null && paperImage != null) aspectFitter = paperImage.GetComponent<AspectRatioFitter>();

        if (hideOnStart) panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (!isOpen) return;

        HandleEscape();
        HandleMouseWheelZoom();
        HandlePinchZoom();
    }

    public void Open(Texture2D texture, string title)
    {
        if (texture == null) return;

        EnsureGameplayRefs();

        CacheState();

        isOpen = true;
        panelRoot.SetActive(true);

        if (titleText != null)
            titleText.text = string.IsNullOrWhiteSpace(title) ? "Document" : title;

        if (paperImage != null)
            paperImage.texture = texture;

        if (aspectFitter != null)
            aspectFitter.aspectRatio = (float)texture.width / texture.height;

        SetZoom(minZoom, keepScreenPoint: false, screenPoint: default);
        SetContentAnchoredPosition(Vector2.zero);

        ApplyOpenState();
    }

    public void Close()
    {
        if (!isOpen) return;

        isOpen = false;
        RestoreState();
        panelRoot.SetActive(false);
    }

    public void OnPaperClicked(Vector2 screenPos, Camera eventCamera)
    {
        if (!isOpen) return;
        if (!clickToToggleZoom) return;

        float prev = currentZoom;
        float midpoint = minZoom + (maxZoom - minZoom) * 0.5f;
        float target = (currentZoom < midpoint) ? maxZoom : minZoom;

        ZoomAroundScreenPoint(target, screenPos, eventCamera, prev);
    }

    private void HandleEscape()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    private void HandleMouseWheelZoom()
    {
        if (Mouse.current == null) return;
        if (scrollRect == null || paperImage == null) return;
        if (ViewportRect == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        if (!RectTransformUtility.RectangleContainsScreenPoint(ViewportRect, mousePos, null))
            return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) < 0.01f) return;

        float prev = currentZoom;
        float target = Mathf.Clamp(currentZoom + Mathf.Sign(scroll) * wheelZoomSpeed, minZoom, maxZoom);
        ZoomAroundScreenPoint(target, mousePos, null, prev);
    }

    private void HandlePinchZoom()
    {
        if (scrollRect == null || paperImage == null) return;

        if (Input.touchCount != 2)
        {
            pinching = false;
            return;
        }

        Touch t0 = Input.GetTouch(0);
        Touch t1 = Input.GetTouch(1);

        Vector2 p0 = t0.position;
        Vector2 p1 = t1.position;

        float dist = (p0 - p1).magnitude;
        Vector2 center = (p0 + p1) * 0.5f;

        if (!pinching)
        {
            pinching = true;
            lastPinchDist = dist;
            scrollRect.velocity = Vector2.zero;
            return;
        }

        float delta = dist - lastPinchDist;
        lastPinchDist = dist;

        if (Mathf.Abs(delta) < 0.5f) return;

        float prev = currentZoom;
        float target = Mathf.Clamp(currentZoom + delta * pinchZoomSpeed, minZoom, maxZoom);
        ZoomAroundScreenPoint(target, center, null, prev);
    }

    private void ZoomAroundScreenPoint(float targetZoom, Vector2 screenPoint, Camera eventCamera, float prevZoom)
    {
        RectTransform content = ContentRect;
        if (content == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(content, screenPoint, eventCamera, out Vector2 local);

        SetZoom(targetZoom, keepScreenPoint: true, screenPoint: screenPoint);

        Vector2 pos = content.anchoredPosition;
        pos -= local * (currentZoom - prevZoom);
        SetContentAnchoredPosition(pos);

        ClampContentToViewport();
        scrollRect.velocity = Vector2.zero;
    }

    private void SetZoom(float zoom, bool keepScreenPoint, Vector2 screenPoint)
    {
        currentZoom = Mathf.Clamp(zoom, minZoom, maxZoom);

        RectTransform content = ContentRect;
        if (content != null)
            content.localScale = new Vector3(currentZoom, currentZoom, 1f);
    }

    private void SetContentAnchoredPosition(Vector2 pos)
    {
        RectTransform content = ContentRect;
        if (content != null)
            content.anchoredPosition = pos;
    }

    private void ClampContentToViewport()
    {
        RectTransform content = ContentRect;
        RectTransform viewport = ViewportRect;
        if (content == null || viewport == null) return;

        Vector2 contentSize = content.rect.size * currentZoom;
        Vector2 viewportSize = viewport.rect.size;

        Vector2 pos = content.anchoredPosition;

        float maxX = Mathf.Max(0f, (contentSize.x - viewportSize.x) * 0.5f);
        float maxY = Mathf.Max(0f, (contentSize.y - viewportSize.y) * 0.5f);

        pos.x = Mathf.Clamp(pos.x, -maxX, maxX);
        pos.y = Mathf.Clamp(pos.y, -maxY, maxY);

        content.anchoredPosition = pos;
    }

    private void EnsureGameplayRefs()
    {
        if (firstPersonController == null) firstPersonController = FindAnyObjectByType<FirstPersonController>();
        if (interactor == null) interactor = FindAnyObjectByType<Interactor>();
        if (touchInput == null) touchInput = FindAnyObjectByType<FirstPersonTouchInput>();
    }

    private void CacheState()
    {
        cachedCursorLock = Cursor.lockState;
        cachedCursorVisible = Cursor.visible;

        controllerWasEnabled = firstPersonController != null && firstPersonController.enabled;
        interactorWasEnabled = interactor != null && interactor.enabled;
        touchInputWasEnabled = touchInput != null && touchInput.enabled;
    }

    private void ApplyOpenState()
    {
        if (firstPersonController != null) firstPersonController.enabled = false;
        if (interactor != null) interactor.enabled = false;
        if (touchInput != null) touchInput.enabled = false;

        if (unlockCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void RestoreState()
    {
        if (firstPersonController != null) firstPersonController.enabled = controllerWasEnabled;
        if (interactor != null) interactor.enabled = interactorWasEnabled;
        if (touchInput != null) touchInput.enabled = touchInputWasEnabled;

        if (unlockCursor)
        {
            Cursor.lockState = cachedCursorLock;
            Cursor.visible = cachedCursorVisible;
        }
    }

    private void OnDisable()
    {
        if (!isOpen) return;
        isOpen = false;
        RestoreState();
    }
}
