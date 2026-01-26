using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhoneKeypadUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private bool hideOnStart = true;

    [Header("Close Button")]
    [SerializeField] private Button closeButton;
    [SerializeField] private bool autoCreateCloseButton = true;
    [SerializeField] private Vector2 closeButtonSize = new Vector2(64f, 64f);
    [SerializeField] private Vector2 closeButtonOffset = new Vector2(-20f, -20f);
    [SerializeField] private string closeButtonLabel = "X";
    [SerializeField] private TMP_FontAsset closeButtonFont;
    [SerializeField] private Color closeButtonColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private Color closeButtonTextColor = Color.white;

    [Header("Gameplay References")]
    [SerializeField] private FirstPersonController firstPersonController;
    [SerializeField] private Interactor interactor;
    [SerializeField] private FirstPersonTouchInput touchInput;

    [Header("Cursor")]
    [SerializeField] private bool unlockCursor = true;

    private bool isOpen;
    private bool controllerWasEnabled;
    private bool interactorWasEnabled;
    private bool touchInputWasEnabled;
    private CursorLockMode cachedCursorLock;
    private bool cachedCursorVisible;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
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

    private void EnsureCloseButton()
    {
        if (closeButton != null)
        {
            HookCloseButton();
            return;
        }

        if (!autoCreateCloseButton) return;

        RectTransform parentRect = panelRoot != null ? panelRoot.GetComponent<RectTransform>() : null;
        if (parentRect == null)
        {
            Debug.LogWarning("PhoneKeypadUI could not create a close button because panelRoot is not a RectTransform.", this);
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
