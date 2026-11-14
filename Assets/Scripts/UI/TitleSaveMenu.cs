using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleSaveMenu : MonoBehaviour
{
    [SerializeField] Button newGameButton;
    [SerializeField] Button continueButton;
    [SerializeField] Button quitButton;
    [SerializeField] GameObject titleLogo;
    [SerializeField] Sprite dialogPanel;

    const int VisibleRows = 3;
    const float RowHeight = 120f;
    const float FooterHeight = 62f;
    const float PanelWidth = 920f;
    const float PanelHeight = 502f;
    const float DeleteActionWidth = 280f;
    const float RowSpacing = 8f;
    static readonly CultureInfo KoreanCulture = CultureInfo.GetCultureInfo("ko-KR");
    GameSave[] saves = Array.Empty<GameSave>();
    RectTransform panel;
    TMP_Text status;
    bool busy;
    bool deleting;
    readonly HashSet<string> selected = new HashSet<string>();
    ScrollRect saveScroll;
    Button reviewDeleteButton;
    GameObject deleteDialog;

    void Awake()
    {
        ShellButtonTheme.Apply(newGameButton);
        ShellButtonTheme.Apply(continueButton);
        ShellButtonTheme.Apply(quitButton);
        newGameButton.onClick.AddListener(StartNewGame);
        continueButton.onClick.AddListener(OpenSaveList);
        quitButton.onClick.AddListener(Quit);

        var template = newGameButton.GetComponentInChildren<TMP_Text>();
        status = CreateLabel("", transform, template, 30);
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
        if (busy || panel != null) return;

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
        var list = new GameObject("SaveList", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(CanvasGroup));
        panel = (RectTransform)list.transform;
        panel.SetParent(transform, false);
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);
        panel.anchoredPosition = Vector2.zero;
        FitSavePanel();
        var layout = list.GetComponent<VerticalLayoutGroup>();
        layout.spacing = RowSpacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;

        var heading = CreateLabel(saves.Length == 0 ? "저장된 게임이 없습니다." : deleting ? "삭제할 저장 선택" : "이어서 플레이할 항목 선택하기",
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
                if (!string.IsNullOrEmpty(save.section))
                    stageName = $"스테이지 {save.section}";

                string savedAt = DateTimeOffset.Parse(save.updatedAt).ToLocalTime()
                    .ToString("yyyy년 M월 d일 tt h:mm:ss", KoreanCulture);
                Toggle checkbox = null;
                var row = AddOption(content, stageName, RowHeight, () =>
                {
                    if (deleting)
                        checkbox.isOn = !checkbox.isOn;
                    else
                        ResumeSave(save.id);
                }, 0.48f);
                if (deleting)
                    checkbox = AddCheckbox(row, save.id);
                var subtitle = CreateLabel($"{savedAt} 저장", row, status, 23);
                subtitle.color = new Color(0.79f, 0.84f, 0.91f);
                subtitle.rectTransform.anchorMin = Vector2.zero;
                subtitle.rectTransform.anchorMax = new Vector2(1f, 0.48f);
                subtitle.rectTransform.offsetMin = new Vector2(64f, 13f);
                subtitle.rectTransform.offsetMax = new Vector2(-64f, 0f);
            }
        }

        var footer = new GameObject("Save Actions", typeof(RectTransform),
            typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        footer.transform.SetParent(panel, false);
        footer.GetComponent<LayoutElement>().preferredHeight = FooterHeight;
        var actions = footer.GetComponent<HorizontalLayoutGroup>();
        actions.spacing = 16f;
        actions.childAlignment = TextAnchor.MiddleCenter;
        actions.childForceExpandWidth = false;
        actions.childForceExpandHeight = false;
        actions.childControlWidth = true;
        actions.childControlHeight = true;
        AddFooterOption(footer.transform, deleting ? "취소" : "뒤로", DeleteActionWidth,
            deleting ? () => SetDeleteMode(false) : CloseList);
        if (deleting)
        {
            reviewDeleteButton = AddFooterOption(footer.transform, "확인 및 삭제", DeleteActionWidth,
                ShowDeleteConfirmation);
            RefreshDeleteButton();
        }
        else if (saves.Length > 0)
        {
            AddFooterOption(footer.transform, "삭제", DeleteActionWidth, () => SetDeleteMode(true));
        }
    }

    void OnRectTransformDimensionsChange() => FitSavePanel();

    void FitSavePanel()
    {
        if (panel == null || !(transform is RectTransform canvasRect))
            return;

        var size = canvasRect.rect.size;
        if (size.x <= 0f || size.y <= 0f)
            return;

        float top = size.y * 0.02f;
        if (titleLogo != null && titleLogo.transform is RectTransform logo)
        {
            var bottom = logo.TransformPoint(new Vector3(logo.rect.center.x, logo.rect.yMin, 0));
            top = canvasRect.InverseTransformPoint(bottom).y - canvasRect.rect.center.y - 12f;
        }
        panel.anchoredPosition = new Vector2(0f, top);
        float availableHeight = Mathf.Max(1f, top + size.y * 0.47f);
        float scale = Mathf.Min(1f, size.x * 0.94f / PanelWidth, availableHeight / PanelHeight);
        panel.localScale = Vector3.one * scale;
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
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        int visibleRows = Mathf.Min(saves.Length, VisibleRows);
        var viewportLayout = viewport.GetComponent<LayoutElement>();
        viewportLayout.preferredHeight = visibleRows * RowHeight + (visibleRows - 1) * RowSpacing;
        viewportLayout.minHeight = viewportLayout.preferredHeight;

        var scroll = saveScroll = viewport.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = saves.Length > VisibleRows;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = RowHeight;
        if (scroll.vertical)
            AddScrollbar(scroll, contentRect);
        return contentRect;
    }

    void AddScrollbar(ScrollRect scroll, RectTransform content)
    {
        var track = AddImage("Scrollbar", scroll.transform, new Color32(188, 152, 99, 255));
        var rect = track.rectTransform;
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(24f, 0f);
        AddScrollbarInset("Recess", rect, new Color32(13, 34, 39, 255), 2f, 2f);
        content.offsetMin = new Vector2(34f, content.offsetMin.y);
        content.offsetMax = new Vector2(-34f, content.offsetMax.y);

        var sliding = new GameObject("Sliding Area", typeof(RectTransform));
        sliding.transform.SetParent(rect, false);
        var slidingRect = (RectTransform)sliding.transform;
        Stretch(slidingRect);
        slidingRect.offsetMin = new Vector2(3f, 3f);
        slidingRect.offsetMax = new Vector2(-3f, -3f);
        var thumb = AddImage("Thumb", slidingRect, new Color32(102, 227, 224, 255));
        Stretch(thumb.rectTransform);
        AddScrollbarInset("Teal Inlay", thumb.rectTransform, new Color32(38, 137, 144, 255), 3f, 4f);
        var bar = track.gameObject.AddComponent<Scrollbar>();
        bar.targetGraphic = thumb;
        bar.handleRect = thumb.rectTransform;
        bar.direction = Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar = bar;
    }

    static void AddScrollbarInset(string label, RectTransform parent, Color color, float x, float y)
    {
        var inset = AddImage(label, parent, color);
        inset.raycastTarget = false;
        Stretch(inset.rectTransform);
        inset.rectTransform.offsetMin = new Vector2(x, y);
        inset.rectTransform.offsetMax = new Vector2(-x, -y);
    }

    Toggle AddCheckbox(Transform row, string id)
    {
        var box = AddImage("Checkbox", row, new Color32(188, 152, 99, 255));
        var rect = box.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(62f, 0f);
        rect.sizeDelta = new Vector2(36f, 36f);
        AddScrollbarInset("Checkbox Recess", rect, new Color32(13, 34, 39, 255), 3f, 3f);
        var mark = new GameObject("Pixel Checkmark", typeof(RectTransform));
        mark.transform.SetParent(rect, false);
        // Stair-step segments stay sharp with the pixel-art frame.
        var pixels = new Vector2Int[] {
            new Vector2Int(-2, 0), new Vector2Int(-1, -1), new Vector2Int(0, -2),
            new Vector2Int(1, -1), new Vector2Int(2, 0), new Vector2Int(3, 1), new Vector2Int(4, 2)
        };
        foreach (var pixel in pixels)
        {
            var point = AddImage("Check Pixel", mark.transform, new Color32(166, 255, 244, 255));
            point.raycastTarget = false;
            Place(point.rectTransform, pixel.x * 3f - 3f, pixel.y * 3f, 5f, 5f);
        }
        var toggle = box.gameObject.AddComponent<Toggle>();
        toggle.targetGraphic = box;
        void ShowSelection(bool value)
        {
            mark.SetActive(value);
            box.color = value ? new Color32(96, 222, 208, 255) : new Color32(188, 152, 99, 255);
        }
        toggle.SetIsOnWithoutNotify(selected.Contains(id));
        ShowSelection(toggle.isOn);
        toggle.onValueChanged.AddListener(value =>
        {
            ShowSelection(value);
            if (value) selected.Add(id);
            else selected.Remove(id);
            RefreshDeleteButton();
        });
        return toggle;
    }

    void RefreshDeleteButton()
    {
        if (reviewDeleteButton == null) return;
        reviewDeleteButton.interactable = selected.Count > 0;
        reviewDeleteButton.GetComponentInChildren<TMP_Text>().text =
            selected.Count > 0 ? $"확인 및 삭제 ({selected.Count})" : "확인 및 삭제";
    }

    void SetDeleteMode(bool enabled)
    {
        if (deleteDialog != null) return;
        deleting = enabled;
        selected.Clear();
        status.text = "";
        RebuildList(saveScroll != null ? saveScroll.verticalNormalizedPosition : 1f);
    }

    void RebuildList(float scrollPosition)
    {
        RemovePanel();
        RenderList();
        Canvas.ForceUpdateCanvases();
        if (saveScroll != null)
            saveScroll.verticalNormalizedPosition = scrollPosition;
    }

    void ShowDeleteConfirmation()
    {
        if (deleteDialog != null || selected.Count == 0) return;
        var ids = selected.ToArray();
        panel.GetComponent<CanvasGroup>().interactable = false;
        panel.GetComponent<CanvasGroup>().blocksRaycasts = false;
        var shade = AddImage("Delete Confirmation", transform, new Color(0f, 0f, 0f, 0.72f));
        Stretch(shade.rectTransform);
        deleteDialog = shade.gameObject;
        var card = AddImage("Dialog", shade.transform, new Color(0.10f, 0.15f, 0.22f));
        if (dialogPanel != null)
        {
            card.sprite = dialogPanel;
            card.color = Color.white;
        }
        card.rectTransform.sizeDelta = new Vector2(880f, 410f);
        ShellPanelSurface.AddTo(card);

        Place(CreateLabel("삭제하시겠습니까?", card.transform, status, 49f).rectTransform,
            0f, 96f, 740f, 70f);
        Place(CreateLabel($"선택한 저장 {ids.Length}개를 삭제합니다.", card.transform, status, 34f).rectTransform,
            0f, 17f, 740f, 50f);
        var yes = AddOption(card.transform, "예", 58f, () => DeleteSelected(ids)).GetComponent<Button>();
        var no = AddOption(card.transform, "아니오", 58f, CloseDeleteConfirmation).GetComponent<Button>();
        Place((RectTransform)yes.transform, -155f, -100f, 280f, 78f);
        Place((RectTransform)no.transform, 155f, -100f, 280f, 78f);
        yes.GetComponentInChildren<TMP_Text>().fontSize = 36f;
        no.GetComponentInChildren<TMP_Text>().fontSize = 36f;
        yes.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnRight = no };
        no.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnLeft = yes };
        EventSystem.current?.SetSelectedGameObject(no.gameObject);
    }

    void CloseDeleteConfirmation()
    {
        if (deleteDialog == null) return;
        deleteDialog.SetActive(false);
        Destroy(deleteDialog);
        deleteDialog = null;
        var group = panel.GetComponent<CanvasGroup>();
        group.interactable = group.blocksRaycasts = true;
        EventSystem.current?.SetSelectedGameObject(reviewDeleteButton.gameObject);
    }

    void DeleteSelected(string[] ids)
    {
        if (deleteDialog == null) return;
        CloseDeleteConfirmation();
        var removed = new HashSet<string>();
        foreach (string id in ids)
        {
            try
            {
                SaveSession.Delete(id);
                removed.Add(id);
                selected.Remove(id);
            }
            catch (Exception error) when (SaveSession.IsFileError(error))
            {
                Debug.LogWarning($"저장을 삭제할 수 없습니다. {error.Message}", this);
            }
        }

        saves = saves.Where(save => !removed.Contains(save.id)).ToArray();
        deleting = selected.Count > 0;
        RebuildList(1f);
        status.text = deleting ? $"삭제하지 못한 저장이 {selected.Count}개 있습니다."
            : $"저장 {removed.Count}개를 삭제했습니다.";
    }

    static Image AddImage(string name, Transform parent, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        return image;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    Button AddFooterOption(Transform parent, string label, float width, Action action)
    {
        var option = AddOption(parent, label, FooterHeight, action);
        option.GetComponent<LayoutElement>().preferredWidth = width;
        option.GetComponentInChildren<TMP_Text>().fontSize = 27f;
        return option.GetComponent<Button>();
    }

    Transform AddOption(Transform parent, string label, float height, Action action, float labelBottom = 0f)
    {
        var row = new GameObject(label, typeof(RectTransform), typeof(Image),
            typeof(Button), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        var image = row.GetComponent<Image>();
        var menuArt = continueButton.targetGraphic as Image;
        if (menuArt != null && menuArt.sprite != null)
        {
            image.sprite = menuArt.sprite;
            image.color = Color.white;
        }
        else
            image.color = new Color(0.12f, 0.16f, 0.23f, 0.96f);
        row.GetComponent<LayoutElement>().preferredHeight = height;
        var button = row.GetComponent<Button>();
        button.targetGraphic = image;
        ShellButtonTheme.Apply(button);
        button.onClick.AddListener(() => { if (!busy) action(); });
        var text = CreateLabel(label, row.transform, status, 30);
        text.rectTransform.anchorMin = new Vector2(0f, labelBottom);
        text.rectTransform.anchorMax = Vector2.one;
        float sidePadding = labelBottom > 0f ? 64f : 12f;
        text.rectTransform.offsetMin = new Vector2(sidePadding, 0f);
        text.rectTransform.offsetMax = new Vector2(-sidePadding, labelBottom > 0f ? -13f : 0f);
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
        SaveSession.PrepareResume(save);
        SceneManager.LoadScene(save.stage);
    }

    void RemovePanel()
    {
        panel.gameObject.SetActive(false);
        Destroy(panel.gameObject);
        panel = null;
        saveScroll = null;
        reviewDeleteButton = null;
    }

    void CloseList()
    {
        RemovePanel();
        deleting = false;
        selected.Clear();
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

// Draw an opaque, flat interior over the panel art while retaining its pixel border.
internal static class ShellPanelSurface
{
    public static void AddTo(Image frame)
    {
        if (frame == null || frame.sprite == null)
            return;

        var fill = new GameObject("Clean Panel Interior", typeof(RectTransform), typeof(Image))
            .GetComponent<Image>();
        fill.transform.SetParent(frame.transform, false);
        fill.transform.SetAsFirstSibling();
        fill.color = new Color32(3, 30, 34, 255);
        fill.raycastTarget = false;
        var rect = fill.rectTransform;
        rect.anchorMin = new Vector2(0.031f, 0.085f);
        rect.anchorMax = new Vector2(0.969f, 0.885f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}

// One palette for the title, save rows, pause menu and confirmation buttons.
internal static class ShellButtonTheme
{
    public static void Apply(Button button)
    {
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.colorMultiplier = 1.55f;
        colors.normalColor = new Color(0.6452f, 0.6452f, 0.6452f, 1f);
        colors.highlightedColor = new Color(0.69f, 0.94f, 1f, 1f);
        colors.selectedColor = new Color(0.69f, 0.94f, 1f, 1f);
        colors.pressedColor = new Color(0.48f, 0.73f, 0.78f, 1f);
        colors.disabledColor = new Color(0.43f, 0.48f, 0.50f, 0.52f);
        colors.fadeDuration = 0.10f;
        button.colors = colors;
    }
}
