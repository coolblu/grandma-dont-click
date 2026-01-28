using UnityEngine;

public class EvidenceInteractable : MonoBehaviour
{
    [SerializeField] private string evidenceId;
    
    public void Verify()
    {
        EvidenceRegistry.Instance?.MarkVerified(evidenceId);
    }
}
