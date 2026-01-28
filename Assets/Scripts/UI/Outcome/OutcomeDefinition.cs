using UnityEngine;

[CreateAssetMenu(menuName = "Game/Outcome Definition")]
public class OutcomeDefinition : ScriptableObject
{
    public Sprite background;

    [TextArea(6, 30)]
    public string crawlText;

    public float crawlDuration = 16f;
    public bool angledCrawl = true;
}
