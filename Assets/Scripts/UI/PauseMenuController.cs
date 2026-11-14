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

    CanvasGroup menu;
    TMP_Text saveStatus;
    GameObject saveConfirmation;
    Button confirmSaveButton;
    Button cancelSaveButton;
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

        resumeButton.onClick.AddListener(Resume);
        restartButton.onClick.AddListener(Restart);
        mainMenuButton.onClick.AddListener(OpenSaveConfirmation);
        CreateSaveStatus();
        CreateSaveConfirmation();
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

        if (paused)
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

        CloseSaveConfirmation();
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

    void Restart()
    {
        Scene scene = gameObject.scene;
#if UNITY_EDITOR
        if (scene.buildIndex < 0 && !string.IsNullOrEmpty(scene.path))
        {
            if (loading) return;
            loading = true;
            Resume();
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                scene.path, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        Load(scene.name);
    }

    void OpenSaveConfirmation()
    {
        if (loading || !paused || saveConfirmation.activeSelf) return;

        saveStatus.text = "";
        resumeButton.interactable = false;
        restartButton.interactable = false;
        mainMenuButton.interactable = false;
        saveConfirmation.SetActive(true);
        EventSystem.current?.SetSelectedGameObject(cancelSaveButton.gameObject);
    }

    void CloseSaveConfirmation()
    {
        if (!saveConfirmation.activeSelf) return;

        saveConfirmation.SetActive(false);
        resumeButton.interactable = true;
        restartButton.interactable = true;
        mainMenuButton.interactable = true;
    }

    void ConfirmSaveAndExit()
    {
        if (loading || !paused || !saveConfirmation.activeSelf) return;

        CloseSaveConfirmation();
        EventSystem.current?.SetSelectedGameObject(mainMenuButton.gameObject);
        SaveAndExit();
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

        // The scene can contain several sections (for example Stage01 contains 1-1 to 1-7).
        string section = null;
        foreach (var view in FindObjectsByType<StageViewController>(FindObjectsSortMode.None))
        {
            if (view.gameObject.scene == scene)
            {
                section = view.CurrentSaveSection;
                break;
            }
        }

        try
        {
            SaveSession.SaveAndExit(scene.name, section);
        }
        catch (Exception error) when (SaveSession.IsFileError(error))
        {
            saveStatus.text = "저장하지 못했습니다. 다시 시도해 주세요.";
            Debug.LogWarning($"{saveStatus.text} {error.Message}", this);
            return;
        }

        Load("Title");
    }

    void CreateSaveConfirmation()
    {
        var font = mainMenuButton.GetComponentInChildren<TMP_Text>().font;
        var shade = CreateDialogImage("Save Confirmation", transform, new Color(0f, 0f, 0f, 0.72f));
        var shadeRect = shade.rectTransform;
        shadeRect.anchorMin = Vector2.zero;
        shadeRect.anchorMax = Vector2.one;
        shadeRect.offsetMin = shadeRect.offsetMax = Vector2.zero;
        saveConfirmation = shade.gameObject;

        var panel = CreateDialogImage("Dialog", shadeRect, new Color(0.09f, 0.12f, 0.18f));
        panel.rectTransform.sizeDelta = new Vector2(720f, 292f);

        AddDialogLabel(panel.transform, font, "저장 후 나가시겠습니까?", 42f,
            new Vector2(0f, 78f), new Vector2(648f, 60f), Color.white);
        AddDialogLabel(panel.transform, font, "해당 과정은 저장되지 않습니다.", 28f,
            new Vector2(0f, 12f), new Vector2(648f, 48f), new Color(0.88f, 0.9f, 0.94f));

        confirmSaveButton = CreateDialogButton(panel.transform, font, "예", -116f, ConfirmSaveAndExit);
        cancelSaveButton = CreateDialogButton(panel.transform, font, "아니오", 116f, Resume);
        confirmSaveButton.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit, selectOnRight = cancelSaveButton
        };
        cancelSaveButton.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit, selectOnLeft = confirmSaveButton
        };
        saveConfirmation.SetActive(false);
    }

    static Image CreateDialogImage(string name, Transform parent, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.color = color;
        return image;
    }

    static Button CreateDialogButton(Transform parent, TMP_FontAsset font, string label, float x,
        Action action)
    {
        var image = CreateDialogImage(label, parent, new Color(0.24f, 0.32f, 0.46f));
        image.rectTransform.anchoredPosition = new Vector2(x, -76f);
        image.rectTransform.sizeDelta = new Vector2(208f, 62f);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => action());
        AddDialogLabel(image.transform, font, label, 32f, Vector2.zero,
            image.rectTransform.sizeDelta, Color.white);
        return button;
    }

    static void AddDialogLabel(Transform parent, TMP_FontAsset font, string label, float size,
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

    void Load(string sceneName)
    {
        if (loading) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"씬을 불러올 수 없습니다. {sceneName}", this);
            return;
        }

        loading = true;
        Resume();
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
