using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainIntro : MonoBehaviour
{
    [SerializeField] DialogueBox dialogueBox;
    [SerializeField] TextAsset story;

    IEnumerator Start()
    {
        yield return dialogueBox.Play(DialogueParser.Parse(story.text));

        if (!Application.CanStreamedLevelBeLoaded(SaveSession.FirstStage))
        {
            Debug.LogError("Stage01 씬을 불러올 수 없습니다.", this);
            yield break;
        }

        var overlay = new GameObject("Scene Fade", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        overlay.transform.SetParent(transform, false);

        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        var black = new GameObject("Black", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        black.transform.SetParent(overlay.transform, false);
        black.rectTransform.anchorMin = Vector2.zero;
        black.rectTransform.anchorMax = Vector2.one;
        black.rectTransform.offsetMin = black.rectTransform.offsetMax = Vector2.zero;
        black.color = Color.black;
        black.raycastTarget = false;

        var fade = overlay.GetComponent<CanvasGroup>();
        yield return UIFade.Fade(fade, 0f, 1f, 0.45f);
        DontDestroyOnLoad(gameObject);
        yield return SceneManager.LoadSceneAsync(SaveSession.FirstStage);
        yield return UIFade.Fade(fade, 1f, 0f, 0.55f);
        Destroy(gameObject);
    }
}
