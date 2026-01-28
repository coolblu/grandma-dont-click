using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Phone Puzzle Config")]
public class PhonePuzzleConfig : ScriptableObject
{
    [Header("Valid Numbers (any formatting OK)")]
    public List<string> validNumbers = new List<string>();

    [Header("Required Evidence IDs")]
    public List<string> requiredEvidence = new List<string>();

    [Header("Outcomes")]
    public OutcomeDefinition winOutcome;
    public OutcomeDefinition loseOutcome;

    [Header("Matching Options")]
    public bool acceptLeading1Variants = true;
}
