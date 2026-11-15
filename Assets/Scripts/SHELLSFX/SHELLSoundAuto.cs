// SHELL sound effects: additive runtime integration.
// Does not modify shared scenes, prefabs, or existing gameplay scripts.
// Unity 6.3 LTS (6000.3). Reads existing buttons, player and wall state.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(9000)]
public sealed class SHELLSoundAuto : MonoBehaviour
{
    private static SHELLSoundAuto instance;
    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private AudioSource audioSource;

    private PlayerController player;
    private Rigidbody2D playerBody;
    private Animator playerAnimator;
    private PoisonSwim playerPoison;
    private SoulWorldController soulController;
    private WorldStateManager worldManager;

    private readonly HashSet<int> boundWeightButtons = new HashSet<int>();
    private readonly HashSet<int> boundPressureButtons = new HashSet<int>();
    private readonly List<SinkingWall> walls = new List<SinkingWall>();
    private readonly Dictionary<int, Vector3> previousWallPositions = new Dictionary<int, Vector3>();

    private bool playerInitialized;
    private bool previousGrounded;
    private bool previousDead;
    private bool previousPoison;
    private bool worldInitialized;
    private bool previousSpirit;
    private int previousDeaths;
    private bool failPlayed;
    private int stepVariant;
    private float nextFootstepAt;
    private float nextScanAt;
    private float lastWallSoundAt = -100f;
    private string currentSceneName;

    // Read from the HUD/SoulWorldController of the supplied project (Deaths: 0 / 5).
    // This is a temporary threshold cue; it is not an authoritative game-over event.
    private const int DeathLimitCue = 5;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimeAudio()
    {
        if (instance != null) return;
        GameObject host = new GameObject("SHELL SFX (runtime only)");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<SHELLSoundAuto>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.dopplerLevel = 0f;
        audioSource.volume = 0.65f;

        string[] clipNames = {
            "01_footstep_grass_01", "01_footstep_grass_02", "02_jump", "03_death",
            "04_soul_transition", "05_stage_clear", "06_stage_fail",
            "07_button_toggle", "08_button_pressure", "09_wall_vanish", "10_poison_enter"
        };
        foreach (string name in clipNames)
        {
            AudioClip clip = Resources.Load<AudioClip>("SHELL_SFX/" + name);
            if (clip == null)
                Debug.LogWarning("[SHELL SFX] Missing audio: Assets/Resources/SHELL_SFX/" + name + ".wav");
            else
                clips[name] = clip;
        }
        currentSceneName = SceneManager.GetActiveScene().name;
    }

    private void OnEnable() { SceneManager.sceneLoaded += SceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= SceneLoaded; }
    private void Start() { ScanScene(true); }

    private void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        int oldLevel = LevelNumber(currentSceneName);
        int nextLevel = LevelNumber(scene.name);
        if (oldLevel > 0 && nextLevel > oldLevel)
            Play("05_stage_clear", 0.90f);

        currentSceneName = scene.name;
        boundWeightButtons.Clear();
        boundPressureButtons.Clear();
        walls.Clear();
        previousWallPositions.Clear();
        player = null;
        soulController = null;
        worldManager = null;
        playerInitialized = false;
        worldInitialized = false;
        failPlayed = false;
        previousDeaths = 0;
        nextScanAt = 0f;
        ScanScene(true);
    }

    private static int LevelNumber(string sceneName)
    {
        if (sceneName == "Stage01" || sceneName.StartsWith("Stage01_", System.StringComparison.Ordinal)) return 1;
        if (sceneName == "Stage02") return 2;
        if (sceneName == "Stage03") return 3;
        if (sceneName == "Ending") return 4;
        return 0;
    }

    private void ScanScene(bool force)
    {
        if (!force && Time.unscaledTime < nextScanAt) return;
        nextScanAt = Time.unscaledTime + 0.80f;

        if (player == null)
        {
            PlayerController found = FindFirstObjectByType<PlayerController>();
            if (found != null)
            {
                player = found;
                playerBody = player.GetComponent<Rigidbody2D>();
                playerAnimator = player.GetComponentInChildren<Animator>();
                playerPoison = player.GetComponent<PoisonSwim>();
                playerInitialized = false;
            }
        }

        if (soulController == null)
        {
            soulController = FindFirstObjectByType<SoulWorldController>();
            if (soulController != null)
            {
                previousDeaths = soulController.DeathCount;
                failPlayed = previousDeaths >= DeathLimitCue;
            }
        }

        if (worldManager == null)
        {
            worldManager = FindFirstObjectByType<WorldStateManager>();
            worldInitialized = false;
        }

        foreach (WeightButton button in FindObjectsByType<WeightButton>(FindObjectsSortMode.None))
        {
            int id = button.GetInstanceID();
            if (!boundWeightButtons.Contains(id) && button.onPressed != null)
            {
                button.onPressed.AddListener(() => Play("08_button_pressure", 0.68f));
                boundWeightButtons.Add(id);
            }
        }
        foreach (PressureButton button in FindObjectsByType<PressureButton>(FindObjectsSortMode.None))
        {
            int id = button.GetInstanceID();
            if (!boundPressureButtons.Contains(id) && button.onPressed != null)
            {
                button.onPressed.AddListener(() => Play("07_button_toggle", 0.72f));
                boundPressureButtons.Add(id);
            }
        }
        foreach (SinkingWall wall in FindObjectsByType<SinkingWall>(FindObjectsSortMode.None))
        {
            int id = wall.GetInstanceID();
            if (previousWallPositions.ContainsKey(id)) continue;
            walls.Add(wall);
            previousWallPositions[id] = wall.transform.position;
        }
    }

    private void Update()
    {
        ScanScene(false);
        if (Time.timeScale <= 0f) return;
        UpdatePlayerSounds();
        UpdateWorldSound();
        UpdateFailureCue();
    }

    private void LateUpdate()
    {
        if (Time.timeScale <= 0f) return;
        // Play once when an existing SinkingWall starts moving downwards.
        for (int i = walls.Count - 1; i >= 0; i--)
        {
            SinkingWall wall = walls[i];
            if (wall == null) { walls.RemoveAt(i); continue; }
            int id = wall.GetInstanceID();
            Vector3 current = wall.transform.position;
            if (previousWallPositions.TryGetValue(id, out Vector3 previous))
            {
                if (current.y < previous.y - 0.002f && Time.unscaledTime - lastWallSoundAt >= 0.7f)
                {
                    Play("09_wall_vanish", 0.70f);
                    lastWallSoundAt = Time.unscaledTime;
                }
            }
            previousWallPositions[id] = current;
        }
    }

    private void UpdatePlayerSounds()
    {
        if (player == null || playerBody == null) return;
        bool inPoison = playerPoison != null && playerPoison.IsSwimming;
        bool grounded = false;
        if (playerAnimator != null)
            grounded = playerAnimator.GetBool("IsGrounded");
        else
            grounded = Mathf.Abs(playerBody.linearVelocity.y) < 0.08f;

        if (!playerInitialized)
        {
            previousGrounded = grounded;
            previousDead = player.IsDead;
            previousPoison = inPoison;
            playerInitialized = true;
            return;
        }

        if (!previousDead && player.IsDead)
            Play("03_death", 0.80f);
        if (!previousPoison && inPoison)
            Play("10_poison_enter", 0.64f);
        if (previousGrounded && !grounded && playerBody.linearVelocity.y > 0.65f && !player.IsDead && !inPoison)
            Play("02_jump", 0.76f);

        if (!player.IsDead && grounded && !inPoison && Mathf.Abs(playerBody.linearVelocity.x) > 0.7f &&
            Time.unscaledTime >= nextFootstepAt)
        {
            Play(stepVariant++ % 2 == 0 ? "01_footstep_grass_01" : "01_footstep_grass_02", 0.43f);
            nextFootstepAt = Time.unscaledTime + 0.34f;
        }

        previousGrounded = grounded;
        previousDead = player.IsDead;
        previousPoison = inPoison;
    }

    private void UpdateWorldSound()
    {
        if (worldManager == null) return;
        bool isSpirit = worldManager.IsSpirit;
        if (!worldInitialized)
        {
            previousSpirit = isSpirit;
            worldInitialized = true;
            return;
        }
        if (isSpirit != previousSpirit)
            Play("04_soul_transition", 0.65f);
        previousSpirit = isSpirit;
    }

    private void UpdateFailureCue()
    {
        if (soulController == null) return;
        int deaths = soulController.DeathCount;
        if (deaths >= DeathLimitCue && previousDeaths < DeathLimitCue && !failPlayed)
        {
            // The supplied project has a counter but no definitive game-over event.
            // Cue at 5 deaths as a preview, not a guaranteed "failed stage" rule.
            Play("06_stage_fail", 0.88f);
            failPlayed = true;
        }
        if (deaths == 0 && previousDeaths != 0)
            failPlayed = false;
        previousDeaths = deaths;
    }

    private void Play(string key, float strength)
    {
        if (audioSource != null && clips.TryGetValue(key, out AudioClip clip) && clip != null)
            audioSource.PlayOneShot(clip, strength);
    }

    // Explicit cues for later, if the team implements dedicated win/lose events.
    public static void PlayClear() { if (instance != null) instance.Play("05_stage_clear", 0.90f); }
    public static void PlayFail() { if (instance != null) instance.Play("06_stage_fail", 0.88f); }
}
