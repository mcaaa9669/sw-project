using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class DialogueBox : MonoBehaviour
{
    [Header("References")]
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] GameObject speakerFrame;
    [SerializeField] TMP_Text speakerText;
    [SerializeField] TMP_Text bodyText;
    [SerializeField] GameObject nextIndicator;
    [SerializeField] Button nextButton;

    [Header("Timing")]
    [SerializeField] float fadeDuration = 0.3f;
    [SerializeField] float charsPerSecond = 30f;

    [Header("Pause")]
    [SerializeField] bool pauseGame = true;

    float prevTimeScale = 1f;
    public bool IsOpen { get; private set; }

    bool nextClicked;

    void Awake()
    {
        canvasGroup.alpha = 0f;
        SetBlocking(false);
        nextIndicator.SetActive(false);

        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextButton);
    }

    void OnNextButton()
    {
        nextClicked = true;
    }

    public IEnumerator Play(DialogueLine[] lines, float lastLineAutoAdvanceDelay = -1f)
    {
        if (lines.Length == 0) yield break;

        SetSpeaker(lines[0].speaker);
        bodyText.maxVisibleCharacters = 0;
        yield return Open();
        for (int i = 0; i < lines.Length; i++)
        {
            float delay = i == lines.Length - 1 ? lastLineAutoAdvanceDelay : -1f;
            yield return ShowLine(lines[i], delay);
        }
        yield return Close();
    }

        void Pause(bool pause)
    {
        if (pause)
        {
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = prevTimeScale;
        }
    }

    IEnumerator Open()
    {
        IsOpen = true;
        SetBlocking(true);
        if (pauseGame) Pause(true);
        yield return UIFade.Fade(canvasGroup, 0f, 1f, fadeDuration);
    }

    IEnumerator Close()
    {
        SetBlocking(false);
        yield return UIFade.Fade(canvasGroup, 1f, 0f, fadeDuration);
        if (pauseGame) Pause(false);
        IsOpen = false;
    }

    void SetSpeaker(string speaker)
    {
        speakerText.text = speaker;
        (speakerFrame != null ? speakerFrame : speakerText.gameObject).SetActive(!string.IsNullOrWhiteSpace(speaker));
    }

    IEnumerator ShowLine(DialogueLine line, float autoAdvanceDelay)
    {
        SetSpeaker(line.speaker);

        bodyText.text = line.body;
        bodyText.ForceMeshUpdate();
        int total = bodyText.textInfo.characterCount;
        bodyText.maxVisibleCharacters = 0;
        nextIndicator.SetActive(false);

        nextClicked = false;
        float shown = 0f;
        while (shown < total)
        {
            if (ConsumeNext()) break;
            shown += charsPerSecond * Time.unscaledDeltaTime;
            bodyText.maxVisibleCharacters = Mathf.Min((int)shown, total);
            yield return null;
        }
        bodyText.maxVisibleCharacters = total;
        yield return null;

        nextClicked = false;
        nextIndicator.SetActive(true);
        float autoAdvanceAt = autoAdvanceDelay >= 0f
            ? Time.unscaledTime + autoAdvanceDelay
            : float.PositiveInfinity;
        while (!ConsumeNext() && Time.unscaledTime < autoAdvanceAt)
        {
            yield return null;
        }
        nextIndicator.SetActive(false);
        yield return null;
    }

    bool ConsumeNext()
    {
        var kb = Keyboard.current;
        bool key = kb != null &&
                   (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame);
        bool pressed = key || nextClicked;
        nextClicked = false;
        return pressed;
    }

    void SetBlocking(bool block)
    {
        canvasGroup.blocksRaycasts = block;
        canvasGroup.interactable = block;
    }
    void OnDisable()
    {
        if (IsOpen && pauseGame) Pause(false);
    }
}