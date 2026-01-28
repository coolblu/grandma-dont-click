using UnityEngine;

public class TextCrawl : MonoBehaviour
{
    [SerializeField] private RectTransform textRect;

    public float Duration = 16f;
    public bool Angled = true;

    [Header("Anchored positions")]
    public Vector2 startPos = new Vector2(0, -650);
    public Vector2 endPos = new Vector2(0, 950);

    [Header("Angle + scale (Star Wars-ish)")]
    public Vector3 angledRotation = new Vector3(55f, 0f, 0f);
    public float startScale = 1.15f;
    public float endScale = 0.65f;

    private float t;

    private void Awake()
    {
        if (textRect == null) textRect = transform as RectTransform;
    }

    public void Restart()
    {
        t = 0f;
        if (textRect == null) textRect = transform as RectTransform;

        textRect.anchoredPosition = startPos;
        textRect.localEulerAngles = Angled ? angledRotation : Vector3.zero;
        textRect.localScale = Vector3.one * startScale;

        enabled = true;
    }

    private void Update()
    {
        if (textRect == null) return;

        t += Time.deltaTime;
        float p = Mathf.Clamp01(t / Mathf.Max(0.01f, Duration));

        textRect.anchoredPosition = Vector2.Lerp(startPos, endPos, p);
        textRect.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, p);

        if (p >= 1f) enabled = false;
    }
}
