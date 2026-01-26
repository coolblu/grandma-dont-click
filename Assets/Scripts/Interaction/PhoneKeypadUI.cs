using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PhoneKeypadUI : MonoBehaviour, IPointerClickHandler
{
    [Serializable]
    private struct KeypadHotspot
    {
        public string key;
        public RectTransform rectTransform;
    }

    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private bool hideOnStart = true;

    [Header("Display")]
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private bool autoCreateDisplay = true;
    [SerializeField] private Vector2 displaySize = new Vector2(360f, 64f);
    [SerializeField] private Vector2 displayOffset = new Vector2(0f, 190f);
    [SerializeField] private TMP_FontAsset displayFont;
    [SerializeField] private float displayFontSize = 42f;
    [SerializeField] private Color displayTextColor = Color.white;
    [SerializeField] private Color displayBackgroundColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private bool clearOnOpen = true;
    [SerializeField] private bool clearOnClose = false;
    [SerializeField, Min(0)] private int maxDigits = 8;
    [SerializeField] private bool starBackspaces = true;
    [SerializeField] private bool hashSubmits = true;
    [SerializeField] private UnityEvent<string> onSubmit;

    [Header("Hotspots")]
    [SerializeField] private bool useHotspots = true;
    [SerializeField] private RectTransform hotspotsRoot;
    [SerializeField] private bool autoCreateHotspots = true;
    [SerializeField] private Vector2 hotspotSize = new Vector2(110f, 110f);
    [SerializeField] private Vector2 hotspotSpacing = new Vector2(125f, 125f);
    [SerializeField] private Vector2 hotspotGridOffset = new Vector2(0f, -10f);
    [SerializeField] private bool showHotspots = false;
    [SerializeField] private Color hotspotColor = new Color(0f, 1f, 1f, 0.2f);
    [SerializeField] private KeypadHotspot[] hotspots = Array.Empty<KeypadHotspot>();

    [Header("Canvas Scaling")]
    [SerializeField] private bool enforceScaleWithScreenSize = true;
    [SerializeField] private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField, Range(0f, 1f)] private float matchWidthOrHeight = 0.5f;

    [Header("Close Button")]
    [SerializeField] private Button closeButton;
    [SerializeField] private bool autoCreateCloseButton = true;
    [SerializeField] private Vector2 closeButtonSize = new Vector2(64f, 64f);
    [SerializeField] private Vector2 closeButtonOffset = new Vector2(-20f, -20f);
    [SerializeField] private string closeButtonLabel = "X";
    [SerializeField] private TMP_FontAsset closeButtonFont;
    [SerializeField] private Color closeButtonColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private Color closeButtonTextColor = Color.white;
    [SerializeField] private RectTransform closeButtonRoot;

    [Header("Gameplay References")]
    [SerializeField] private FirstPersonController firstPersonController;
    [SerializeField] private Interactor interactor;
    [SerializeField] private FirstPersonTouchInput touchInput;

    [Header("Cursor")]
    [SerializeField] private bool unlockCursor = true;

    private static readonly string[,] KeypadLayout =
    {
        { "1", "2", "3" },
        { "4", "5", "6" },
        { "7", "8", "9" },
        { "*", "0", "#" }
    };

    private bool isOpen;
    private bool controllerWasEnabled;
    private bool interactorWasEnabled;
    private bool touchInputWasEnabled;
    private CursorLockMode cachedCursorLock;
    private bool cachedCursorVisible;
    private string currentInput = string.Empty;

    public bool IsOpen => isOpen;
    public string CurrentInput => currentInput;

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
        if (closeButtonRoot == null) closeButtonRoot = transform as RectTransform;
        EnsureCanvasScaler();
        EnsureCloseButton();
        EnsureDisplay();
        EnsureHotspots();
        if (hideOnStart) panelRoot.SetActive(false);
    }

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        if (panelRoot == null) panelRoot = gameObject;
        if (closeButtonRoot == null) closeButtonRoot = transform as RectTransform;

        EnsureCanvasScaler();
        EnsureHotspots();
    }

    public void Open()
    {
        if (isOpen) return;

        EnsureReferences();
        CacheState();

        isOpen = true;
        SetPanelActive(true);
        ApplyOpenState();
        if (clearOnOpen) ClearInput();
        else UpdateDisplay();
    }

    public void Close()
    {
        if (!isOpen) return;

        isOpen = false;
        RestoreState();
        if (clearOnClose) ClearInput();
        SetPanelActive(false);
    }

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    public void OnClosePressed()
    {
        Close();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isOpen) return;
        if (eventData == null) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (!TryGetKeyFromPointer(eventData, out string key)) return;
        OnKeyPressed(key);
    }

    public void OnKeyPressed(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        if (key == "*" && starBackspaces)
        {
            Backspace();
            return;
        }

        if (key == "#" && hashSubmits)
        {
            Submit();
            return;
        }

        if (maxDigits > 0 && currentInput.Length >= maxDigits) return;
        currentInput += key;
        UpdateDisplay();
    }

    public void ClearInput()
    {
        currentInput = string.Empty;
        UpdateDisplay();
    }

    private void Backspace()
    {
        if (currentInput.Length == 0) return;
        currentInput = currentInput.Substring(0, currentInput.Length - 1);
        UpdateDisplay();
    }

    private void Submit()
    {
        if (onSubmit != null) onSubmit.Invoke(currentInput);
    }

    private void EnsureCloseButton()
    {
        if (closeButton != null)
        {
            HookCloseButton();
            return;
        }

        if (!autoCreateCloseButton) return;

        RectTransform parentRect = closeButtonRoot != null
            ? closeButtonRoot
            : panelRoot != null
                ? panelRoot.GetComponent<RectTransform>()
                : null;
        if (parentRect == null)
        {
            Debug.LogWarning("PhoneKeypadUI could not create a close button because no RectTransform root is available.", this);
            return;
        }

        GameObject buttonObject = new GameObject(
            "CloseButton",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );
        buttonObject.transform.SetParent(parentRect, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = closeButtonSize;
        rect.anchoredPosition = closeButtonOffset;

        Image image = buttonObject.GetComponent<Image>();
        image.color = closeButtonColor;

        closeButton = buttonObject.GetComponent<Button>();
        closeButton.targetGraphic = image;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = closeButtonLabel;
        label.alignment = TextAlignmentOptions.Center;
        label.color = closeButtonTextColor;
        label.fontSize = 36f;
        label.raycastTarget = false;

        if (closeButtonFont == null)
        {
            closeButtonFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        if (closeButtonFont != null)
        {
            label.font = closeButtonFont;
        }

        HookCloseButton();
    }

    private void EnsureDisplay()
    {
        if (displayText != null)
        {
            UpdateDisplay();
            return;
        }

        if (!autoCreateDisplay) return;

        RectTransform parentRect = GetComponent<RectTransform>();
        if (parentRect == null)
        {
            Debug.LogWarning("PhoneKeypadUI could not create a display because it is missing a RectTransform.", this);
            return;
        }

        GameObject displayObject = new GameObject(
            "Display",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        displayObject.transform.SetParent(parentRect, false);

        RectTransform rect = displayObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = displaySize;
        rect.anchoredPosition = displayOffset;

        Image background = displayObject.GetComponent<Image>();
        background.color = displayBackgroundColor;
        background.raycastTarget = false;

        GameObject labelObject = new GameObject("DisplayText", typeof(RectTransform));
        labelObject.transform.SetParent(displayObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 6f);
        labelRect.offsetMax = new Vector2(-8f, -6f);

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.color = displayTextColor;
        label.fontSize = displayFontSize;
        label.raycastTarget = false;

        if (displayFont == null)
        {
            displayFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        if (displayFont != null)
        {
            label.font = displayFont;
        }

        displayText = label;
        UpdateDisplay();
    }

    private bool TryGetKeyFromPointer(PointerEventData eventData, out string key)
    {
        key = null;

        if (useHotspots && TryGetKeyFromHotspots(eventData, out key)) return true;

        return TryGetKeyFromGrid(eventData, out key);
    }

    private bool TryGetKeyFromHotspots(PointerEventData eventData, out string key)
    {
        key = null;
        if (hotspots == null || hotspots.Length == 0) return false;

        Camera eventCamera = eventData.pressEventCamera != null
            ? eventData.pressEventCamera
            : eventData.enterEventCamera;

        for (int i = 0; i < hotspots.Length; i++)
        {
            RectTransform rect = hotspots[i].rectTransform;
            if (rect == null) continue;

            if (!RectTransformUtility.RectangleContainsScreenPoint(rect, eventData.position, eventCamera)) continue;

            string value = hotspots[i].key;
            if (string.IsNullOrEmpty(value)) continue;

            key = value;
            return true;
        }

        return false;
    }

    private bool TryGetKeyFromGrid(PointerEventData eventData, out string key)
    {
        key = null;

        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null) return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            return false;
        }

        Rect rect = rectTransform.rect;
        if (rect.width <= 0f || rect.height <= 0f) return false;

        float normalizedX = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        float normalizedY = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);

        int columns = KeypadLayout.GetLength(1);
        int rows = KeypadLayout.GetLength(0);

        int col = Mathf.Clamp(Mathf.FloorToInt(normalizedX * columns), 0, columns - 1);
        int row = Mathf.Clamp(Mathf.FloorToInt((1f - normalizedY) * rows), 0, rows - 1);

        key = KeypadLayout[row, col];
        return true;
    }

    private void UpdateDisplay()
    {
        if (displayText == null) return;
        displayText.text = currentInput;
    }

    private void HookCloseButton()
    {
        if (closeButton == null) return;
        closeButton.onClick.RemoveListener(OnClosePressed);
        closeButton.onClick.AddListener(OnClosePressed);
    }

    private void EnsureReferences()
    {
        if (firstPersonController == null) firstPersonController = FindObjectOfType<FirstPersonController>();
        if (interactor == null) interactor = FindObjectOfType<Interactor>();
        if (touchInput == null) touchInput = FindObjectOfType<FirstPersonTouchInput>();
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

    private void SetPanelActive(bool active)
    {
        if (panelRoot != null) panelRoot.SetActive(active);
        else gameObject.SetActive(active);
    }

    private void EnsureCanvasScaler()
    {
        if (!enforceScaleWithScreenSize) return;

        CanvasScaler scaler = GetComponentInParent<CanvasScaler>();
        if (scaler == null) return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = matchWidthOrHeight;
    }

    private void EnsureHotspots()
    {
        if (!useHotspots) return;

        if (hotspotsRoot == null)
        {
            hotspotsRoot = FindHotspotsRoot();
            if (hotspotsRoot == null && autoCreateHotspots)
            {
                hotspotsRoot = CreateHotspotsRoot();
            }
        }

        if (hotspots == null || hotspots.Length == 0)
        {
            KeypadHotspot[] discovered = FindHotspots(hotspotsRoot);
            if (discovered.Length > 0)
            {
                hotspots = discovered;
            }
            else if (autoCreateHotspots)
            {
                RectTransform parent = hotspotsRoot != null ? hotspotsRoot : transform as RectTransform;
                hotspots = CreateHotspots(parent);
            }
        }

        UpdateHotspotVisuals();
    }

    private RectTransform FindHotspotsRoot()
    {
        Transform found = transform.Find("Hotspots");
        return found as RectTransform;
    }

    private RectTransform CreateHotspotsRoot()
    {
        RectTransform parentRect = transform as RectTransform;
        if (parentRect == null) return null;

        GameObject rootObject = new GameObject("Hotspots", typeof(RectTransform));
        rootObject.transform.SetParent(parentRect, false);

        RectTransform rect = rootObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private KeypadHotspot[] FindHotspots(RectTransform root)
    {
        if (root == null) return Array.Empty<KeypadHotspot>();

        int childCount = root.childCount;
        if (childCount == 0) return Array.Empty<KeypadHotspot>();

        var found = new System.Collections.Generic.List<KeypadHotspot>(childCount);
        for (int i = 0; i < childCount; i++)
        {
            RectTransform child = root.GetChild(i) as RectTransform;
            if (child == null) continue;
            if (!child.name.StartsWith("Key_", StringComparison.Ordinal)) continue;

            string key = child.name.Substring("Key_".Length);
            if (string.IsNullOrEmpty(key)) continue;

            found.Add(new KeypadHotspot
            {
                key = key,
                rectTransform = child
            });
        }

        return found.ToArray();
    }

    private KeypadHotspot[] CreateHotspots(RectTransform parent)
    {
        if (parent == null) return Array.Empty<KeypadHotspot>();

        int rows = KeypadLayout.GetLength(0);
        int cols = KeypadLayout.GetLength(1);

        float totalWidth = (cols - 1) * hotspotSpacing.x;
        float totalHeight = (rows - 1) * hotspotSpacing.y;
        Vector2 origin = new Vector2(-totalWidth * 0.5f, totalHeight * 0.5f) + hotspotGridOffset;

        var created = new System.Collections.Generic.List<KeypadHotspot>(rows * cols);
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                string key = KeypadLayout[row, col];
                GameObject keyObject = new GameObject(
                    $"Key_{key}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image)
                );
                keyObject.transform.SetParent(parent, false);

                RectTransform rect = keyObject.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = hotspotSize;
                rect.anchoredPosition = origin + new Vector2(col * hotspotSpacing.x, -row * hotspotSpacing.y);

                Image image = keyObject.GetComponent<Image>();
                image.raycastTarget = false;

                created.Add(new KeypadHotspot
                {
                    key = key,
                    rectTransform = rect
                });
            }
        }

        return created.ToArray();
    }

    private void UpdateHotspotVisuals()
    {
        if (hotspots == null || hotspots.Length == 0) return;

        Color visible = hotspotColor;
        Color hidden = new Color(hotspotColor.r, hotspotColor.g, hotspotColor.b, 0f);
        Color target = showHotspots ? visible : hidden;

        for (int i = 0; i < hotspots.Length; i++)
        {
            RectTransform rect = hotspots[i].rectTransform;
            if (rect == null) continue;

            Image image = rect.GetComponent<Image>();
            if (image == null) continue;

            image.raycastTarget = false;
            image.color = target;
        }
    }

    private void OnDisable()
    {
        if (!isOpen) return;

        isOpen = false;
        RestoreState();
    }
}
