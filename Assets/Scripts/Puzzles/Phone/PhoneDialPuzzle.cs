using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PhoneDialPuzzle : MonoBehaviour
{
    [SerializeField] private PhonePuzzleConfig config;
    [SerializeField] private string outcomeSceneName = "OutcomeScene";

    private HashSet<string> validDigits;

    private void Awake()
    {
        validDigits = new HashSet<string>();

        if (config == null) return;

        foreach (var raw in config.validNumbers)
        {
            var norm = DigitsOnly(raw);
            if (string.IsNullOrEmpty(norm)) continue;

            validDigits.Add(norm);

            if (config.acceptLeading1Variants)
            {
                if (norm.Length == 11 && norm[0] == '1') validDigits.Add(norm.Substring(1));
                if (norm.Length == 10) validDigits.Add("1" + norm);
            }
        }
    }

    public void HandleDial(string rawInput)
    {
        bool hasEvidence = EvidenceRegistry.Instance != null &&
                           EvidenceRegistry.Instance.HasAll(config.requiredEvidence);

        string input = DigitsOnly(rawInput);
        bool isValidNumber = validDigits.Contains(input);

        var outcome = (hasEvidence && isValidNumber) ? config.winOutcome : config.loseOutcome;

        GameFlowState.LastOutcome = outcome;
        SceneManager.LoadScene(outcomeSceneName);
    }

    private static string DigitsOnly(string s)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;

        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
            if (char.IsDigit(c)) sb.Append(c);

        return sb.ToString();
    }
}
