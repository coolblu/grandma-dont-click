using UnityEngine;

public class PhoneInteractable : InteractableBase
{
    [SerializeField] private PhoneKeypadUI keypadUI;

    protected override void Interact(Interactor interactor)
    {
        PhoneKeypadUI ui = ResolveKeypadUI();
        if (ui == null)
        {
            Debug.LogWarning("PhoneInteractable is missing a PhoneKeypadUI reference.", this);
            return;
        }

        ui.Open();
    }

    private PhoneKeypadUI ResolveKeypadUI()
    {
        if (keypadUI != null) return keypadUI;

        keypadUI = FindObjectOfType<PhoneKeypadUI>();
        if (keypadUI != null) return keypadUI;

        PhoneKeypadUI[] all = Resources.FindObjectsOfTypeAll<PhoneKeypadUI>();
        for (int i = 0; i < all.Length; i++)
        {
            if (!all[i].gameObject.scene.IsValid()) continue;
            if (!all[i].gameObject.scene.isLoaded) continue;

            keypadUI = all[i];
            break;
        }

        return keypadUI;
    }
}
