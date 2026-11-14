using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class PauseMenuController : MonoBehaviour
{
    [SerializeField] Button resumeButton;
    [SerializeField] Button restartButton;
    [SerializeField] Button mainMenuButton;
    [SerializeField] Sprite dialogPanel;
    [SerializeField] Image pausePanel;

    CanvasGroup menu;
    TMP_Text saveStatus;
    GameObject confirmation;
    TMP_Text confirmationTitle;
    TMP_Text confirmationDetails;
    Button cancelConfirmationButton;
    Action confirmAction;
    Action cancelAction;
    bool paused;
    bool loading;
    float previousTimeScale;

    void Awake()
    {
        menu = GetComponent<CanvasGroup>();
        if (GetComponentInParent<Canvas>() == null)
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
        }

        ShellButtonTheme.Apply(resumeButton);
        ShellButtonTheme.Apply(restartButton);
        ShellButtonTheme.Apply(mainMenuButton);
        resumeButton.onClick.AddListener(Resume);
        restartButton.onClick.AddListener(OpenRestartConfirmation);
        mainMenuButton.onClick.AddListener(OpenSaveConfirmation);
        ShellPanelSurface.AddTo(pausePanel);
        CreateSaveStatus();
        CreateConfirmation();
        ShowMenu(false);
    }

    void Start()
    {
        if (gameObject.scene.name == "PauseMenu_Test")
            Pause();
    }

    void Update()
    {
        if (loading || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        if (confirmation.activeSelf)
            CancelConfirmation();
        else if (paused)
            Resume();
        else if (Time.timeScale > 0f)
            Pause();
    }

    void Pause()
    {
        if (EventSystem.current == null)
        {
            var input = new GameObject("Pause EventSystem", typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            SceneManager.MoveGameObjectToScene(input, gameObject.scene);
        }

        previousTimeScale = Time.timeScale;
        paused = true;
        Time.timeScale = 0f;
        saveStatus.text = "";
        ShowMenu(true);
        EventSystem.current?.SetSelectedGameObject(resumeButton.gameObject);
    }

    void Resume()
    {
        if (!paused) return;

        CloseConfirmation();
        paused = false;
        Time.timeScale = previousTimeScale;
        ShowMenu(false);
        var input = EventSystem.current;
        if (input != null && input.currentSelectedGameObject != null &&
            input.currentSelectedGameObject.transform.IsChildOf(transform))
        {
            input.SetSelectedGameObject(null);
        }
    }

    void OpenRestartConfirmation()
    {
        OpenConfirmation("해당 스테이지를 다시 시작하시겠습니까?", "",
            Restart, () => EventSystem.current?.SetSelectedGameObject(restartButton.gameObject));
    }

    void Restart()
    {
        Scene scene = gameObject.scene;
        string section = GetCurrentSection(scene);
#if UNITY_EDITOR
        if (scene.buildIndex < 0 && !string.IsNullOrEmpty(scene.path))
        {
            if (loading) return;
            loading = true;
            SaveSession.PrepareStageEntry(scene.name, section);
            Resume();
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                scene.path, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        Load(scene.path, () => SaveSession.PrepareStageEntry(scene.name, section));
    }

    void OpenSaveConfirmation()
    {
        OpenConfirmation("저장 후 나가시겠습니까?", "해당 과정은 저장되지 않습니다.",
            () =>
            {
                EventSystem.current?.SetSelectedGameObject(mainMenuButton.gameObject);
                SaveAndExit();
            }, Resume);
    }

    void OpenConfirmation(string title, string details, Action onConfirm, Action onCancel)
    {
        if (loading || !paused || confirmation.activeSelf) return;

        saveStatus.text = "";
        confirmationTitle.text = title;
        confirmationDetails.text = details;
        confirmationTitle.rectTransform.anchoredPosition =
            new Vector2(0f, string.IsNullOrEmpty(details) ? 82f : 120f);
        confirmAction = onConfirm;
        cancelAction = onCancel;
        resumeButton.interactable = false;
        restartButton.interactable = false;
        mainMenuButton.interactable = false;
        if (pausePanel != null) pausePanel.gameObject.SetActive(false);
        confirmation.SetActive(true);
        EventSystem.current?.SetSelectedGameObject(cancelConfirmationButton.gameObject);
    }

    void CloseConfirmation()
    {
        if (!confirmation.activeSelf) return;

        confirmation.SetActive(false);
        if (pausePanel != null) pausePanel.gameObject.SetActive(true);
        confirmAction = cancelAction = null;
        resumeButton.interactable = true;
        restartButton.interactable = true;
        mainMenuButton.interactable = true;
    }

    void ConfirmSelection() => ResolveConfirmation(confirmAction);

    void CancelConfirmation() => ResolveConfirmation(cancelAction);

    void ResolveConfirmation(Action action)
    {
        if (loading || !paused || !confirmation.activeSelf) return;

        CloseConfirmation();
        action?.Invoke();
    }

    static string GetCurrentSection(Scene scene)
    {
        foreach (var view in FindObjectsByType<StageViewController>(FindObjectsSortMode.None))
        {
            if (view.gameObject.scene == scene && !string.IsNullOrEmpty(view.CurrentSaveSection))
                return view.CurrentSaveSection;
        }
        return null;
    }

    void SaveAndExit()
    {
        if (loading || !paused) return;

        Scene scene = gameObject.scene;
        if (!SaveSession.IsSaveStage(scene.name) ||
            scene.buildIndex < 0 ||
            !Application.CanStreamedLevelBeLoaded("Title"))
        {
            saveStatus.text = "이 화면에서는 저장할 수 없습니다.";
            return;
        }

        try
        {
            SaveSession.SaveAndExit(scene.name, GetCurrentSection(scene));
        }
        catch (Exception error) when (SaveSession.IsFileError(error))
        {
            saveStatus.text = "저장하지 못했습니다. 다시 시도해 주세요.";
            Debug.LogWarning($"{saveStatus.text} {error.Message}", this);
            return;
        }

        Load("Title");
    }

    void CreateConfirmation()
    {
        var font = mainMenuButton.GetComponentInChildren<TMP_Text>().font;
        var shade = CreateDialogImage("Pause Confirmation", transform, Color.clear);
        var shadeRect = shade.rectTransform;
        shadeRect.anchorMin = Vector2.zero;
        shadeRect.anchorMax = Vector2.one;
        shadeRect.offsetMin = shadeRect.offsetMax = Vector2.zero;
        confirmation = shade.gameObject;

        var panel = CreateDialogImage("Dialog", shadeRect, new Color(0.09f, 0.12f, 0.18f));
        if (dialogPanel != null)
        {
            panel.sprite = dialogPanel;
            panel.color = Color.white;
        }
        if (pausePanel != null)
        {
            var source = pausePanel.rectTransform;
            var rect = panel.rectTransform;
            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMax;
            rect.pivot = source.pivot;
            rect.anchoredPosition = source.anchoredPosition;
            rect.sizeDelta = source.sizeDelta;
        }
        else
            panel.rectTransform.sizeDelta = new Vector2(1100f, 590f);
        ShellPanelSurface.AddTo(panel);

        confirmationTitle = AddDialogLabel(panel.transform, font, "", 54f,
            new Vector2(0f, 120f), new Vector2(948f, 92f), Color.white);
        confirmationTitle.enableAutoSizing = true;
        confirmationTitle.fontSizeMin = 36f;
        confirmationTitle.fontSizeMax = 54f;
        confirmationDetails = AddDialogLabel(panel.transform, font, "", 38f,
            new Vector2(0f, 25f), new Vector2(900f, 66f), new Color(0.88f, 0.9f, 0.94f));

        var confirmSaveButton = CreateDialogButton(panel.transform, font, "예", -190f, ConfirmSelection);
        var cancelSaveButton = CreateDialogButton(panel.transform, font, "아니오", 190f, CancelConfirmation);
        cancelConfirmationButton = cancelSaveButton;
        confirmSaveButton.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit, selectOnRight = cancelSaveButton
        };
        cancelSaveButton.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit, selectOnLeft = confirmSaveButton
        };
        confirmation.SetActive(false);
    }

    static Image CreateDialogImage(string name, Transform parent, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        return image;
    }

    Button CreateDialogButton(Transform parent, TMP_FontAsset font, string label, float x,
        Action action)
    {
        var image = CreateDialogImage(label, parent, new Color(0.24f, 0.32f, 0.46f));
        var menuArt = mainMenuButton.targetGraphic as Image;
        if (menuArt != null && menuArt.sprite != null)
        {
            image.sprite = menuArt.sprite;
            image.color = Color.white;
        }
        image.rectTransform.anchoredPosition = new Vector2(x, -116f);
        image.rectTransform.sizeDelta = new Vector2(344f, 90f);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ShellButtonTheme.Apply(button);
        button.onClick.AddListener(() => action());
        AddDialogLabel(image.transform, font, label, 42f, Vector2.zero,
            image.rectTransform.sizeDelta, Color.white);
        return button;
    }

    static TMP_Text AddDialogLabel(Transform parent, TMP_FontAsset font, string label, float size,
        Vector2 position, Vector2 bounds, Color color)
    {
        var text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        text.transform.SetParent(parent, false);
        text.font = font;
        text.text = label;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.rectTransform.anchoredPosition = position;
        text.rectTransform.sizeDelta = bounds;
        return text;
    }

    void CreateSaveStatus()
    {
        var label = mainMenuButton.GetComponentInChildren<TMP_Text>();
        saveStatus = new GameObject("Save Status", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TMP_Text>();
        saveStatus.transform.SetParent(mainMenuButton.transform, false);
        saveStatus.font = label.font;
        saveStatus.fontSize = 24;
        saveStatus.alignment = TextAlignmentOptions.Center;
        saveStatus.raycastTarget = false;
        var rect = saveStatus.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -8f);
        rect.sizeDelta = new Vector2(680f, 42f);
    }

    void Load(string sceneName, Action beforeLoad = null)
    {
        if (loading) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"씬을 불러올 수 없습니다. {sceneName}", this);
            return;
        }

        loading = true;
        Resume();
        beforeLoad?.Invoke();
        SceneManager.LoadScene(sceneName);
    }

    void OnDisable() => Resume();

    void ShowMenu(bool visible)
    {
        menu.alpha = visible ? 1f : 0f;
        menu.interactable = visible;
        menu.blocksRaycasts = visible;
    }
}
