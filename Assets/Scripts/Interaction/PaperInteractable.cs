using UnityEngine;

public class PaperInteractable : InteractableBase
{
    [Header("Paper Content")]
    [SerializeField] private Texture2D paperTexture;
    [SerializeField] private string paperTitle = "Document";

    [Header("UI (Optional Override)")]
    [SerializeField] private PaperViewerUI paperViewerUI;

    protected override void Interact(Interactor interactor)
    {
        if (paperTexture == null)
        {
            Debug.LogWarning($"{name}: PaperInteractable has no paperTexture assigned.", this);
            return;
        }

        PaperViewerUI ui = paperViewerUI != null
            ? paperViewerUI
            : FindAnyObjectByType<PaperViewerUI>();

        if (ui == null)
        {
            Debug.LogWarning($"{name}: No PaperViewerUI found in the scene.", this);
            return;
        }

        ui.Open(paperTexture, paperTitle);
    }
}
