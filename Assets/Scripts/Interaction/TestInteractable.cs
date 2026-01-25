using UnityEngine;

public class TestInteractable : InteractableBase
{
    protected override void Interact(Interactor interactor)
    {
        Debug.Log($"Interacted with {name}");
    }

    public override void OnFocusEnter(Interactor interactor)
    {
        Debug.Log($"Focus enter {name}");
    }

    public override void OnFocusExit(Interactor interactor)
    {
        Debug.Log($"Focus exit {name}");
    }
}
