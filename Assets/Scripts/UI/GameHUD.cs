
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private GameObject stageBadge;
    [SerializeField] private TMP_Text stageText;
    [SerializeField] private TMP_Text deathText;
    [SerializeField] private PauseMenuController pauseMenuPrefab;
    [SerializeField] private Sprite controlFrame;
    [SerializeField] private Sprite pauseIcon;
    [SerializeField] private Sprite restartIcon;

    [Header("Soul Timer")]
    [SerializeField] private GameObject soulTimerGroup;
    [SerializeField] private Image timerFill;

    public void SetStage(string section, bool showDeaths)
    {
        if (stageBadge != null && stageText != null)
        {
            stageBadge.SetActive(!string.IsNullOrEmpty(section));
            stageText.text = string.IsNullOrEmpty(section) ? string.Empty : $"STAGE {section}";
        }

        if (deathText != null)
            deathText.gameObject.SetActive(showDeaths);
    }

    // 사망 횟수 갱신
    private int displayedMaxDeaths = 5;

    public void SetDeathCount(int count)
    {
        SetDeathCount(count, displayedMaxDeaths);
    }

    public void SetDeathCount(int count, int maxDeaths)
    {
        displayedMaxDeaths = Mathf.Max(1, maxDeaths);
        deathText.text = $"DEATHS : {count} / {displayedMaxDeaths}";
    }

    // 남은 시간에 따라 게이지 갱신
    public void SetSoulTime(float remaining, float maxTime)
    {
        timerFill.fillAmount = maxTime > 0f ? Mathf.Clamp01(remaining / maxTime) : 0f;
    }

    // 영혼세계 타이머 표시 및 숨김
    public void ShowSoulTimer(bool show)
    {
        soulTimerGroup.SetActive(show);
    }

    private void Start()
    {
        PauseMenuController pauseMenu = null;
        foreach (var menu in FindObjectsByType<PauseMenuController>(FindObjectsSortMode.None))
        {
            if (menu.gameObject.scene != gameObject.scene) continue;
            pauseMenu = menu;
            break;
        }

        if (pauseMenu == null && pauseMenuPrefab != null && gameObject.scene.name.StartsWith("Stage"))
        {
            pauseMenu = Instantiate(pauseMenuPrefab);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(pauseMenu.gameObject, gameObject.scene);
        }

        if (pauseMenu == null) return;

        pauseMenu.EnsureEventSystem();
        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null && !canvas.TryGetComponent<GraphicRaycaster>(out _))
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        AddControl("Pause", "II", -102f, pauseMenu.OpenPauseMenu, pauseIcon);
        AddControl("Restart", "↻", -24f, pauseMenu.RequestRestart, restartIcon);
    }

    private void AddControl(string name, string icon, float right, UnityEngine.Events.UnityAction action, Sprite iconSprite = null)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(transform, false);
        var rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(right, -22f);
        rect.sizeDelta = new Vector2(70f, 70f);
        image.sprite = controlFrame;
        ShellButtonTheme.ApplyFrame(image);
        image.color = controlFrame != null ? Color.white : new Color32(24, 46, 49, 255);

        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 0.89f);
        colors.pressedColor = new Color(0.72f, 0.80f, 0.79f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(SHELLSoundAuto.PlayMenuClick);
        button.onClick.AddListener(action);

        if (iconSprite != null)
        {
            var glyph = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            var glyphRect = glyph.rectTransform;
            glyphRect.SetParent(rect, false);
            glyphRect.anchorMin = glyphRect.anchorMax = glyphRect.pivot = new Vector2(0.5f, 0.5f);
            glyphRect.anchoredPosition = Vector2.zero;
            glyphRect.sizeDelta = new Vector2(38f, 38f);
            glyph.sprite = iconSprite;
            glyph.preserveAspect = true;
            glyph.color = ShellButtonTheme.Ivory;
            glyph.raycastTarget = false;
            return;
        }

        var label = new GameObject("Icon", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        label.transform.SetParent(rect, false);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        label.font = deathText.font;
        label.text = icon == "↻" && !label.font.HasCharacter('↻') ? "R" : icon;
        label.fontSize = 46f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = ShellButtonTheme.Ivory;
        label.raycastTarget = false;
    }

    private void Awake()
    {
        string sceneName = gameObject.scene.name;
        if (sceneName.StartsWith("Stage") && int.TryParse(sceneName.Substring(5), out int stageNumber))
            SetStage(stageNumber.ToString(), true);
        ShowSoulTimer(false);
    }
}
