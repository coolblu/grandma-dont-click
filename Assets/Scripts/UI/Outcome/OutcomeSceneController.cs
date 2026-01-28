using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OutcomeSceneController : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text crawlText;
    [SerializeField] private TextCrawl crawl;

    [Header("Return scene")]
    [SerializeField] private string returnSceneName = "OfficeScene";

    private void Start()
    {
        var outcome = GameFlowState.LastOutcome;

        if (outcome != null)
        {
            if (background != null) background.sprite = outcome.background;
            if (crawlText != null) crawlText.text = outcome.crawlText;

            if (crawl != null)
            {
                crawl.Angled = outcome.angledCrawl;
                crawl.Duration = outcome.crawlDuration;
                crawl.Restart();
            }
        }
        else
        {
            if (crawlText != null) crawlText.text = "No outcome set.";
        }
    }

    private void Update()
    {
        if (Input.anyKeyDown) SceneManager.LoadScene(returnSceneName);
    }
}
