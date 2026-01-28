using TMPro;
using UnityEngine;

public class BlinkingTMP : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float blinkInterval = 0.6f;
    [SerializeField] private float visibleAlpha = 1f;
    [SerializeField] private float hiddenAlpha = 0f;

    private float timer;
    private bool visible = true;

    private void Awake()
    {
        if (text == null) text = GetComponent<TMP_Text>();
        SetAlpha(visibleAlpha);
    }

    private void OnEnable()
    {
        timer = 0f;
        visible = true;
        SetAlpha(visibleAlpha);
    }

    private void Update()
    {
        if (text == null) return;

        timer += Time.deltaTime;
        if (timer >= blinkInterval)
        {
            timer = 0f;
            visible = !visible;
            SetAlpha(visible ? visibleAlpha : hiddenAlpha);
        }
    }

    private void SetAlpha(float a)
    {
        if (text == null) return;
        var c = text.color;
        c.a = a;
        text.color = c;
    }
}
