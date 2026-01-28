using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class OutcomeSceneController : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text crawlText;
    [SerializeField] private TextCrawl crawl;

    [Header("Prompt")]
    [SerializeField] private GameObject continuePromptObject;
    [SerializeField] private TMP_Text continuePromptText;
    [SerializeField] private string continuePrompt = "Tap or press any button to continue";

    [Header("Optional: hide crawl once finished")]
    [SerializeField] private GameObject crawlContainer;
    [SerializeField] private bool hideCrawlAfterFinish = false;

    [Header("Return scene")]
    [SerializeField] private string returnSceneName = "OfficeScene";

    private bool canContinue;
    private bool isLeaving;

    private void Start()
    {
        if (continuePromptText != null) continuePromptText.text = continuePrompt;
        if (continuePromptObject != null) continuePromptObject.SetActive(false);

        var outcome = GameFlowState.LastOutcome;

        if (outcome != null)
        {
            if (background != null) background.sprite = outcome.background;
            if (crawlText != null) crawlText.text = outcome.crawlText;
            if (crawl != null)
            {
                crawl.Angled = outcome.angledCrawl;
                crawl.Duration = outcome.crawlDuration;

                crawl.onFinished.RemoveListener(OnCrawlFinished);
                crawl.onFinished.AddListener(OnCrawlFinished);

                crawl.Restart();
            }
        }
        else
        {
            canContinue = true;
            ShowContinuePrompt();
        }
    }

    private void OnCrawlFinished()
    {
        canContinue = true;

        if (hideCrawlAfterFinish && crawlContainer != null)
            crawlContainer.SetActive(false);

        ShowContinuePrompt();
    }

    private void ShowContinuePrompt()
    {
        if (continuePromptObject != null)
            continuePromptObject.SetActive(true);
    }

    private void Update()
    {
        if (!canContinue) return;

        if (AnyInputPressedThisFrame())
            Continue();
    }

    private bool AnyInputPressedThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            return true;

        if (Mouse.current != null &&
            (Mouse.current.leftButton.wasPressedThisFrame ||
             Mouse.current.rightButton.wasPressedThisFrame ||
             Mouse.current.middleButton.wasPressedThisFrame))
            return true;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return true;

        if (Gamepad.current != null)
        {
            foreach (var c in Gamepad.current.allControls)
            {
                if (c is ButtonControl b && b.wasPressedThisFrame)
                    return true;
            }
        }

        return false;
    }

    private void Continue()
    {
        if (isLeaving) return;
        isLeaving = true;

        SceneManager.LoadScene(returnSceneName);
    }
}
