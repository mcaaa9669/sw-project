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
        mainMenuButton.onClick.AddListener(ReturnToTitle);
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
        ShowMenu(true);
        EventSystem.current?.SetSelectedGameObject(resumeButton.gameObject);
    }

    void Resume()
    {
        if (!paused) return;

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

    void ReturnToTitle() => Load("Title");

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
