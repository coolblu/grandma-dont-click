using UnityEngine;

public abstract class InteractableBase : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private string displayName = "Interact";
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private Transform interactionPoint;

    public string DisplayName => displayName;
    public float InteractionRange => interactionRange;
    public Transform InteractionPoint => interactionPoint != null ? interactionPoint : transform;

    public bool TryInteract(Interactor interactor)
    {
        if (!CanInteract(interactor)) return false;
        Interact(interactor);
        return true;
    }

    public virtual bool CanInteract(Interactor interactor)
    {
        if (!isActiveAndEnabled) return false;
        if (interactor == null) return false;

        if (interactionRange > 0f)
        {
            float distance = Vector3.Distance(interactor.Origin, InteractionPoint.position);
            if (distance > interactionRange) return false;
        }

        return true;
    }

    protected abstract void Interact(Interactor interactor);

    public virtual void OnFocusEnter(Interactor interactor) { }

    public virtual void OnFocusExit(Interactor interactor) { }
}
