using UnityEngine;

[CreateAssetMenu(menuName = "Game/Outcome Definition")]
public class OutcomeDefinition : ScriptableObject
{
    [Header("Visuals")]
    public Sprite background;

    [Header("Header")]
    public string titleText = "WIN";
    public Color titleColor = Color.white;
    [Range(100, 300)] public int titleSizePercent = 180;

    [Header("Body")]
    [TextArea(6, 30)]
    public string crawlText;

    [Header("Crawl")]
    public float crawlDuration = 16f;
    public bool angledCrawl = true;
}