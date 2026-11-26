using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public sealed class SHELLJungyoonVFXRuntime : MonoBehaviour
{
    private const string CorpsePrefabPath = "SHELL_JungyoonVFX/FX_CorpseSpawn";
    private const string SoulPrefabPath = "SHELL_JungyoonVFX/FX_SoulShift";
    private const string ForestLeavesPath = "SHELL_JungyoonVFX/FX_ForestLeaves";
    private const string LandingPath = "SHELL_JungyoonVFX/FX_LandingDust";
    private const string CheckpointPath = "SHELL_JungyoonVFX/FX_CheckpointPulse";
    private const string GearHitPath = "SHELL_JungyoonVFX/FX_GearHit";
    private const string SpikeHitPath = "SHELL_JungyoonVFX/FX_SpikeHit";
    private const string DeviceActivatePath = "SHELL_JungyoonVFX/FX_DeviceActivate";

    private static SHELLJungyoonVFXRuntime instance;
    private static GameObject corpsePrefab;
    private static GameObject soulPrefab;
    private static bool corpsePrefabChecked;
    private static bool soulPrefabChecked;
    private static GameObject landingPrefab;
    private static GameObject checkpointPrefab;
    private static GameObject gearHitPrefab;
    private static GameObject spikeHitPrefab;
    private static GameObject deviceActivatePrefab;

    private PlayerController player;
    private WorldType previousWorld;
    private bool worldInitialized;
    private bool returnEffectPending;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntime()
    {
        if (instance != null)
            return;

        GameObject host = new GameObject("SHELL Jungyoon VFX (runtime)");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<SHELLJungyoonVFXRuntime>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BindScene();
        AddForestLeaves(SceneManager.GetActiveScene());
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        WorldStateManager.OnWorldChanged += OnWorldChanged;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        WorldStateManager.OnWorldChanged -= OnWorldChanged;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindScene();
        AddForestLeaves(scene);
    }

    private static void AddForestLeaves(Scene scene)
    {
        if (scene.name != "Stage01")
            return;

        foreach (SHELLForestLeaves leaves in FindObjectsByType<SHELLForestLeaves>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (leaves.gameObject.scene == scene)
                return;
        }

        GameObject prefab = Resources.Load<GameObject>(ForestLeavesPath);
        if (prefab != null && prefab.activeSelf &&
            prefab.TryGetComponent(out SHELLForestLeaves settings) && settings.CanPlay)
            SceneManager.MoveGameObjectToScene(Instantiate(prefab), scene);
    }

    private void BindScene()
    {
        returnEffectPending = false;
        player = FindFirstObjectByType<PlayerController>();
        WorldStateManager worldState = FindFirstObjectByType<WorldStateManager>();
        worldInitialized = worldState != null;
        if (worldInitialized)
            previousWorld = worldState.CurrentWorld;
    }

    private void OnWorldChanged(WorldType world)
    {
        if (!worldInitialized)
        {
            previousWorld = world;
            worldInitialized = true;
            return;
        }

        if (world == previousWorld)
            return;

        previousWorld = world;
        SHELLSoundAuto.PlaySoulTransition();
        returnEffectPending = world == WorldType.Real;
        if (!returnEffectPending)
            PlaySoulShift();
    }

    private void LateUpdate()
    {
        if (!returnEffectPending)
            return;

        // Let the world-return listeners move the player to the checkpoint first.
        returnEffectPending = false;
        PlaySoulShift();
    }

    private void PlaySoulShift()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (player != null)
            Spawn(SoulPrefabPath, ref soulPrefab, ref soulPrefabChecked, player.transform.position);
    }

    public static void PlayCorpseSpawn(Vector3 position)
    {
        SHELLSoundAuto.PlayCorpseSpawn();
        Spawn(CorpsePrefabPath, ref corpsePrefab, ref corpsePrefabChecked, position);
    }

    public static void PlayLanding(Vector3 position) => PlayBurst(LandingPath, ref landingPrefab, position);

    public static void PlayCheckpointPulse(Vector3 position) =>
        PlayBurst(CheckpointPath, ref checkpointPrefab, position);

    public static void PlayHazardImpact(Scene scene, DeathType type, Vector3 position)
    {
        if (scene.name != "Stage01")
            return;

        // Unparented so hiding the victim or freezing the hazard does not stop playback.
        if (type == DeathType.saw)
            PlayBurst(GearHitPath, ref gearHitPrefab, position);
        else if (type == DeathType.Spike)
            PlayBurst(SpikeHitPath, ref spikeHitPrefab, position);
    }

    public static void PlayDeviceActivation(Component device)
    {
        if (device.gameObject.scene.name != "Stage01")
            return;

        Renderer renderer = device.GetComponentInChildren<Renderer>();
        Vector3 center = renderer != null ? renderer.bounds.center : device.transform.position;
        PlayBurst(DeviceActivatePath, ref deviceActivatePrefab, center);
    }

    private static void PlayBurst(string path, ref GameObject prefab, Vector3 position)
    {
        if (prefab == null)
            prefab = Resources.Load<GameObject>(path);

        if (prefab != null && prefab.activeSelf &&
            prefab.TryGetComponent(out SHELLVFXFramePlayer settings) && settings.CanPlay)
            Instantiate(prefab, position, Quaternion.identity);
    }

    private static void Spawn(string resourcePath, ref GameObject prefab, ref bool checkedForPrefab, Vector3 position)
    {
        if (!checkedForPrefab)
        {
            prefab = Resources.Load<GameObject>(resourcePath);
            checkedForPrefab = true;
            if (prefab == null)
                Debug.LogWarning($"SHELL VFX prefab not found at Resources/{resourcePath}. Build it from the SHELL/Jungyoon VFX menu.");
        }

        if (prefab != null)
            Instantiate(prefab, position, Quaternion.identity);
    }
}
