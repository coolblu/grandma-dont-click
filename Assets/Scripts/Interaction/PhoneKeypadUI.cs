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
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private Button closeButton;

    [Header("Events")]
    [SerializeField] private UnityEvent<string> onSubmit;

    [Header("Gameplay References")]
    [SerializeField] private FirstPersonController firstPersonController;
    [SerializeField] private Interactor interactor;
    [SerializeField] private FirstPersonTouchInput touchInput;

    [Header("Cursor")]
    [SerializeField] private bool unlockCursor = true;

    private const int MaxDigits = 16;
    private const string CloseButtonLabel = "X";
    private const float DisplayFontSize = 42f;
    private const float CloseLabelFontSize = 36f;

    private KeypadHotspot[] hotspots = Array.Empty<KeypadHotspot>();
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
        CacheHotspots();
        EnsureDisplay();
        EnsureCloseButton();
        if (hideOnStart) panelRoot.SetActive(false);
    }

    public void Open()
    {
        if (isOpen) return;

        EnsureReferences();
        CacheState();

        isOpen = true;
        SetPanelActive(true);
        ApplyOpenState();
        ClearInput();
    }

    public void Close()
    {
        if (!isOpen) return;

        isOpen = false;
        RestoreState();
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

        if (!TryGetKeyFromHotspots(eventData, out string key)) return;
        OnKeyPressed(key);
    }

    public void OnKeyPressed(string key)
    {
        if (string.IsNullOrEmpty(key)) return;

        if (MaxDigits > 0 && currentInput.Length >= MaxDigits) return;
        currentInput += key;
        UpdateDisplay();
    }

    public void ClearInput()
    {
        currentInput = string.Empty;
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

        RectTransform parentRect = transform as RectTransform;
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
        rect.sizeDelta = new Vector2(64f, 64f);
        rect.anchoredPosition = new Vector2(-20f, -20f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.6f);

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
        label.text = CloseButtonLabel;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.fontSize = CloseLabelFontSize;
        label.raycastTarget = false;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null)
        {
            label.font = font;
        }

        HookCloseButton();
    }

    private void EnsureDisplay()
    {
        if (displayText != null)
        {
            ConfigureDisplayText(displayText);
            UpdateDisplay();
            return;
        }

        RectTransform parentRect = transform as RectTransform;
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
        rect.sizeDelta = new Vector2(360f, 64f);
        rect.anchoredPosition = new Vector2(0f, 190f);

        Image background = displayObject.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.6f);
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
        label.color = Color.white;
        label.fontSize = DisplayFontSize;
        label.raycastTarget = false;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (font != null)
        {
            label.font = font;
        }

        displayText = label;
        ConfigureDisplayText(displayText);
        UpdateDisplay();
    }

    private void ConfigureDisplayText(TMP_Text text)
    {
        if (text == null) return;

        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.enableAutoSizing = true;
        text.fontSizeMin = 18f;
        text.fontSizeMax = DisplayFontSize;
    }

    private void CacheHotspots()
    {
        Transform root = transform.Find("Hotspots");
        if (root == null)
        {
            hotspots = Array.Empty<KeypadHotspot>();
            return;
        }

        int childCount = root.childCount;
        if (childCount == 0)
        {
            hotspots = Array.Empty<KeypadHotspot>();
            return;
        }

        var list = new System.Collections.Generic.List<KeypadHotspot>(childCount);
        for (int i = 0; i < childCount; i++)
        {
            RectTransform child = root.GetChild(i) as RectTransform;
            if (child == null) continue;
            if (!child.name.StartsWith("Key_", StringComparison.Ordinal)) continue;

            string key = child.name.Substring("Key_".Length);
            if (string.IsNullOrEmpty(key)) continue;

            HideHotspotVisual(child);

            list.Add(new KeypadHotspot
            {
                key = key,
                rectTransform = child
            });
        }

        hotspots = list.ToArray();
    }

    private void HideHotspotVisual(RectTransform rect)
    {
        Image image = rect.GetComponent<Image>();
        if (image == null) return;

        image.raycastTarget = false;
        Color color = image.color;
        color.a = 0f;
        image.color = color;
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

    private void OnDisable()
    {
        if (!isOpen) return;

        isOpen = false;
        RestoreState();
    }
}
