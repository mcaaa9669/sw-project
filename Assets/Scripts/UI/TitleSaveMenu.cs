using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleSaveMenu : MonoBehaviour
{
    [SerializeField] Button newGameButton;
    [SerializeField] Button continueButton;
    [SerializeField] Button quitButton;

    const int PageSize = 4;
    GameSave[] saves = Array.Empty<GameSave>();
    RectTransform panel;
    TMP_Text status;
    int page;
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

        page = 0;
        SetBusy(false, "");
        newGameButton.gameObject.SetActive(false);
        continueButton.gameObject.SetActive(false);
        quitButton.gameObject.SetActive(false);
        RenderList();
    }

    void RenderList()
    {
        if (panel != null)
        {
            panel.gameObject.SetActive(false);
            Destroy(panel.gameObject);
        }

        var list = new GameObject("SaveList", typeof(RectTransform), typeof(VerticalLayoutGroup));
        panel = (RectTransform)list.transform;
        panel.SetParent(transform, false);
        panel.anchorMin = new Vector2(0.26f, 0.12f);
        panel.anchorMax = new Vector2(0.74f, 0.67f);
        panel.offsetMin = panel.offsetMax = Vector2.zero;
        var layout = list.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandHeight = false;

        CreateLabel(saves.Length == 0 ? "저장된 게임이 없습니다." : "저장 선택",
            panel, status, 34);

        int start = page * PageSize;
        for (int i = start; i < Mathf.Min(start + PageSize, saves.Length); i++)
        {
            GameSave save = saves[i];
            string date = DateTimeOffset.TryParse(save.updatedAt, out var time)
                ? time.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") : save.updatedAt;
            AddOption($"{save.stage}  ·  {date}  ·  {save.id.Substring(0, 6)}",
                () => ResumeSave(save.id));
        }

        if (page > 0) AddOption("이전 페이지", () => { page--; RenderList(); });
        if (start + PageSize < saves.Length) AddOption("다음 페이지", () => { page++; RenderList(); });
        AddOption("뒤로", CloseList);
    }

    void AddOption(string label, Action action)
    {
        var row = new GameObject(label, typeof(RectTransform), typeof(Image),
            typeof(Button), typeof(LayoutElement));
        row.transform.SetParent(panel, false);
        row.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.23f, 0.96f);
        row.GetComponent<LayoutElement>().preferredHeight = 48;
        row.GetComponent<Button>().onClick.AddListener(() => { if (!busy) action(); });
        var text = CreateLabel(label, row.transform, status, 30);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
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
