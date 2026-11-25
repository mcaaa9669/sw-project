// SHELL SFX5 - runtime-only automatic event wiring (25 Nov 2026).
// Only this new file is required. No changes to scenes, prefabs or gameplay scripts.
// Uses the already-installed SHELLSFX8.cs, SHELLSFX8UIHover.cs and five WAV files.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(100)]
public sealed class SHELLSFX5AutoWiring : MonoBehaviour
{
    private static SHELLSFX5AutoWiring instance;
    private readonly List<Lever> levers = new List<Lever>();
    private readonly Dictionary<Lever, bool> leverStates = new Dictionary<Lever, bool>();
    private readonly List<MovingPlatform> platforms = new List<MovingPlatform>();
    private readonly Dictionary<MovingPlatform, AudioSource> platformSources = new Dictionary<MovingPlatform, AudioSource>();
    private readonly HashSet<RespawnPoint> checkpoints = new HashSet<RespawnPoint>();
    private float nextScanTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var host = new GameObject("SHELL SFX5 (runtime auto-wiring)");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<SHELLSFX5AutoWiring>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Scan();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        ClearTrackedObjects();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) ClearTrackedObjects();
        nextScanTime = 0f;
        Scan();
    }

    private void Update()
    {
        // Rescan for dynamically created UI buttons and objects, including during Pause.
        if (Time.unscaledTime >= nextScanTime)
        {
            nextScanTime = Time.unscaledTime + 0.75f;
            Scan();
        }

        // 11 ON / 12 OFF - play only on an actual Lever.IsPulled change.
        for (int i = levers.Count - 1; i >= 0; --i)
        {
            Lever lever = levers[i];
            if (lever == null) continue;
            if (!lever.isActiveAndEnabled) continue;
            bool pulled = lever.IsPulled;
            bool previous;
            if (!leverStates.TryGetValue(lever, out previous))
            {
                leverStates[lever] = pulled;
                continue;
            }
            if (pulled == previous) continue;
            leverStates[lever] = pulled;
            if (pulled) SHELLSFX8.PlayLeverOn();
            else SHELLSFX8.PlayLeverOff();
        }

        // 16 Moving platform - continuous while truly moving; fade and stop when paused.
        for (int i = platforms.Count - 1; i >= 0; --i)
        {
            MovingPlatform platform = platforms[i];
            if (platform == null) continue;
            AudioSource source;
            if (!platformSources.TryGetValue(platform, out source) || source == null || source.clip == null)
                continue;

            bool moving = platform.isActiveAndEnabled &&
                          platform.Velocity.sqrMagnitude > 0.0016f && Time.timeScale > 0f;
            if (moving && !source.isPlaying) source.Play();
            float targetVolume = moving ? 0.27f : 0f;
            source.volume = Mathf.MoveTowards(source.volume, targetVolume, Time.unscaledDeltaTime * 1.8f);
            if (!moving && source.volume < 0.001f && source.isPlaying) source.Stop();
        }
    }

    private void Scan()
    {
        foreach (Lever lever in FindObjectsByType<Lever>(FindObjectsSortMode.None))
        {
            if (leverStates.ContainsKey(lever)) continue;
            leverStates.Add(lever, lever.IsPulled);
            levers.Add(lever);
        }

        foreach (MovingPlatform platform in FindObjectsByType<MovingPlatform>(FindObjectsSortMode.None))
        {
            if (platformSources.ContainsKey(platform)) continue;
            AudioSource source = platform.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;
            source.clip = SHELLSFX8.Clip(SHELLSFX8.PlatformMove);
            platformSources.Add(platform, source);
            platforms.Add(platform);
        }

        foreach (RespawnPoint checkpoint in FindObjectsByType<RespawnPoint>(FindObjectsSortMode.None))
        {
            if (!checkpoints.Add(checkpoint)) continue;
            checkpoint.Reached += OnCheckpointReached;
        }

        // 18 UI: only buttons belonging to SHELL's Title and Pause menus.
        // Existing hover component handles both pointer hover and keyboard selection.
        foreach (Button button in FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            if (button.GetComponentInParent<TitleSaveMenu>() == null &&
                button.GetComponentInParent<PauseMenuController>() == null)
                continue;
            if (button.GetComponent<SHELLSFX8UIHover>() == null)
                button.gameObject.AddComponent<SHELLSFX8UIHover>();
        }

        // Remove destroyed objects from the trackers when changing or editing scenes.
        levers.RemoveAll(item => item == null);
        platforms.RemoveAll(item => item == null);
        var deadLevers = new List<Lever>();
        foreach (Lever item in leverStates.Keys) if (item == null) deadLevers.Add(item);
        foreach (Lever item in deadLevers) leverStates.Remove(item);
        var deadPlatforms = new List<MovingPlatform>();
        foreach (MovingPlatform item in platformSources.Keys) if (item == null) deadPlatforms.Add(item);
        foreach (MovingPlatform item in deadPlatforms) platformSources.Remove(item);
        checkpoints.RemoveWhere(item => item == null);
    }

    private void OnCheckpointReached(PlayerController player)
    {
        // RespawnPoint fires Reached only after PlayerController.SetRespawnPoint succeeds.
        SHELLSFX8.PlayCheckpoint();
    }

    private void ClearTrackedObjects()
    {
        foreach (RespawnPoint point in checkpoints)
            if (point != null) point.Reached -= OnCheckpointReached;
        checkpoints.Clear();
        levers.Clear();
        leverStates.Clear();
        platforms.Clear();
        platformSources.Clear();
    }
}
