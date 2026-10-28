using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class DialogueBox : MonoBehaviour
{
    [Header("References")]
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] TMP_Text speakerText;
    [SerializeField] TMP_Text bodyText;
    [SerializeField] GameObject nextIndicator;
    [SerializeField] Button nextButton;

    [Header("Timing")]
    [SerializeField] float fadeDuration = 0.3f;
    [SerializeField] float charsPerSecond = 30f;

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

    public IEnumerator Play(DialogueLine[] lines)
    {
        yield return Open();
        foreach (DialogueLine line in lines)
        {
            yield return ShowLine(line);
        }
        yield return Close();
    }

    IEnumerator Open()
    {
        IsOpen = true;
        SetBlocking(true);
        yield return UIFade.Fade(canvasGroup, 0f, 1f, fadeDuration);
    }

    IEnumerator Close()
    {
        SetBlocking(false);
        yield return UIFade.Fade(canvasGroup, 1f, 0f, fadeDuration);
        IsOpen = false;
    }

    IEnumerator ShowLine(DialogueLine line)
    {
        speakerText.text = line.speaker;
        speakerText.gameObject.SetActive(!string.IsNullOrEmpty(line.speaker));

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
        while (!ConsumeNext())
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
}