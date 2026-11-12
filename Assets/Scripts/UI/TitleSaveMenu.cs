using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleSaveMenu : MonoBehaviour
{
    [SerializeField] Button newGameButton;
    [SerializeField] Button continueButton;
    [SerializeField] Button quitButton;

    const int VisibleRows = 5;
    const float RowHeight = 88f;
    const float RowSpacing = 8f;
    static readonly CultureInfo KoreanCulture = CultureInfo.GetCultureInfo("ko-KR");
    GameSave[] saves = Array.Empty<GameSave>();
    RectTransform panel;
    TMP_Text status;
    bool busy;

    void Awake()
    {
        newGameButton.onClick.AddListener(StartNewGame);
        continueButton.onClick.AddListener(OpenSaveList);
        quitButton.onClick.AddListener(Quit);

        var template = newGameButton.GetComponentInChildren<TMP_Text>();
        status = CreateLabel("SaveStatus", transform, template, 30);
        var rect = status.rectTransform;
        rect.anchorMin = new Vector2(0.1f, 0.04f);
        rect.anchorMax = new Vector2(0.9f, 0.12f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    void StartNewGame()
    {
        if (busy) return;
        if (!Application.CanStreamedLevelBeLoaded(SaveSession.FirstStage))
        {
            status.text = "Stage01 씬을 불러올 수 없습니다.";
            return;
        }

        try
        {
            EnterGame(SaveSession.Create());
        }
        catch (Exception error) when (SaveSession.IsFileError(error))
        {
            ShowError("새 저장을 만들 수 없습니다.", error);
        }
    }

    void OpenSaveList()
    {
        if (busy) return;

        try
        {
            saves = SaveSession.List();
        }
        catch (Exception error) when (SaveSession.IsFileError(error))
        {
            ShowError("저장 목록을 불러올 수 없습니다.", error);
            return;
        }

        SetBusy(false, "");
        newGameButton.gameObject.SetActive(false);
        continueButton.gameObject.SetActive(false);
        quitButton.gameObject.SetActive(false);
        RenderList();
    }

    void RenderList()
    {
        var list = new GameObject("SaveList", typeof(RectTransform), typeof(VerticalLayoutGroup));
        panel = (RectTransform)list.transform;
        panel.SetParent(transform, false);
        panel.anchorMin = new Vector2(0.26f, 0.10f);
        panel.anchorMax = new Vector2(0.74f, 0.67f);
        panel.offsetMin = panel.offsetMax = Vector2.zero;
        var layout = list.GetComponent<VerticalLayoutGroup>();
        layout.spacing = RowSpacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childForceExpandHeight = false;

        var heading = CreateLabel(saves.Length == 0 ? "저장된 게임이 없습니다." : "저장 선택",
            panel, status, 34);
        heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 48;

        if (saves.Length > 0)
        {
            var content = CreateSaveScroll();
            foreach (GameSave save in saves)
            {
                string stageName = save.stage switch
                {
                    SaveSession.FirstStage => "스테이지 1",
                    "Stage02" => "스테이지 2",
                    "Stage03" => "스테이지 3",
                    "Ending" => "엔딩",
                    _ => "저장된 게임"
                };
                string savedAt = DateTimeOffset.Parse(save.updatedAt).ToLocalTime()
                    .ToString("yyyy년 M월 d일 tt h:mm:ss", KoreanCulture);
                var row = AddOption(content, stageName, RowHeight, () => ResumeSave(save.id), 0.43f);
                var subtitle = CreateLabel($"{savedAt} 저장", row, status, 23);
                subtitle.color = new Color(0.79f, 0.84f, 0.91f);
                subtitle.rectTransform.anchorMin = Vector2.zero;
                subtitle.rectTransform.anchorMax = new Vector2(1f, 0.43f);
                subtitle.rectTransform.offsetMin = subtitle.rectTransform.offsetMax = Vector2.zero;
            }
        }

        AddOption(panel, "뒤로", 48, CloseList);
    }

    RectTransform CreateSaveScroll()
    {
        var viewport = new GameObject("SaveViewport", typeof(RectTransform), typeof(Image),
            typeof(RectMask2D), typeof(ScrollRect), typeof(LayoutElement));
        viewport.transform.SetParent(panel, false);
        viewport.GetComponent<Image>().color = Color.clear;
        var viewportRect = (RectTransform)viewport.transform;
        var content = new GameObject("SaveRows", typeof(RectTransform), typeof(VerticalLayoutGroup));
        var contentRect = (RectTransform)content.transform;
        contentRect.SetParent(viewportRect, false);
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = Vector2.one;
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, saves.Length * RowHeight + (saves.Length - 1) * RowSpacing);

        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = RowSpacing;
        layout.childForceExpandHeight = false;
        int visibleRows = Mathf.Min(saves.Length, VisibleRows);
        var viewportLayout = viewport.GetComponent<LayoutElement>();
        viewportLayout.preferredHeight = visibleRows * RowHeight + (visibleRows - 1) * RowSpacing;
        viewportLayout.minHeight = viewportLayout.preferredHeight;

        var scroll = viewport.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = saves.Length > VisibleRows;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = RowHeight;
        return contentRect;
    }

    Transform AddOption(Transform parent, string label, float height, Action action, float labelBottom = 0f)
    {
        var row = new GameObject(label, typeof(RectTransform), typeof(Image),
            typeof(Button), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        var image = row.GetComponent<Image>();
        image.color = new Color(0.12f, 0.16f, 0.23f, 0.96f);
        row.GetComponent<LayoutElement>().preferredHeight = height;
        var button = row.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => { if (!busy) action(); });
        var text = CreateLabel(label, row.transform, status, 30);
        text.rectTransform.anchorMin = new Vector2(0f, labelBottom);
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        return row.transform;
    }

    static TMP_Text CreateLabel(string label, Transform parent, TMP_Text template, float size)
    {
        var text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        text.font = template.font;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.text = label;
        return text;
    }

    void ResumeSave(string id)
    {
        try
        {
            EnterGame(SaveSession.Read(id));
        }
        catch (Exception error) when (SaveSession.IsFileError(error))
        {
            ShowError("선택한 저장을 불러올 수 없습니다.", error);
        }
    }

    void EnterGame(GameSave save)
    {
        if (!Application.CanStreamedLevelBeLoaded(save.stage))
        {
            SetBusy(false, "저장된 스테이지를 불러올 수 없습니다.");
            return;
        }

        SetBusy(true, "");
        SaveSession.Select(save);
        SceneManager.LoadScene(save.stage);
    }

    void CloseList()
    {
        panel.gameObject.SetActive(false);
        Destroy(panel.gameObject);
        panel = null;
        newGameButton.gameObject.SetActive(true);
        continueButton.gameObject.SetActive(true);
        quitButton.gameObject.SetActive(true);
        SetBusy(false, "");
    }

    void SetBusy(bool value, string message)
    {
        busy = value;
        newGameButton.interactable = !value;
        continueButton.interactable = !value;
        quitButton.interactable = !value;
        status.text = message;
    }

    void ShowError(string message, Exception error)
    {
        SetBusy(false, message);
        Debug.LogWarning($"{message} {error.Message}", this);
    }

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
