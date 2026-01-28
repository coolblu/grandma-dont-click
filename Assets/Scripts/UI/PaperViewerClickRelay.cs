using UnityEngine;
using UnityEngine.EventSystems;

public class PaperViewerClickRelay : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private PaperViewerUI viewer;

    private void Awake()
    {
        if (viewer == null) viewer = GetComponentInParent<PaperViewerUI>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (viewer == null) return;
        if (eventData == null) return;

        viewer.OnPaperClicked(eventData.position, eventData.pressEventCamera);
    }
}
