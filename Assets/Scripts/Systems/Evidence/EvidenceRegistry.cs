using System.Collections.Generic;
using UnityEngine;

public class EvidenceRegistry : MonoBehaviour
{
    public static EvidenceRegistry Instance { get; private set; }

    private readonly HashSet<string> verified = new HashSet<string>();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void MarkVerified(string evidenceId)
    {
        if (!string.IsNullOrEmpty(evidenceId)) verified.Add(evidenceId);
    }

    public bool HasAll(IEnumerable<string> ids)
    {
        foreach (var id in ids) if (!verified.Contains(id)) return false;
        return true;
    }
}
