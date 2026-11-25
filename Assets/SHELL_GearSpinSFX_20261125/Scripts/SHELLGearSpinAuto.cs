// SHELL gear/saw rotation audio. Additive; does not alter existing game objects/scenes/prefabs.
// Supports the project's SawRotate and replacement gear prefabs with gear-related names.
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SHELLGearSpinAuto : MonoBehaviour
{
    const string ClipPath = "SHELL_GearSpin/13_saw_spin_LOOP";
    static SHELLGearSpinAuto manager;
    static AudioClip clip;
    float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (manager != null) return;
        var host = new GameObject("__SHELL_GearSpin_Runtime");
        DontDestroyOnLoad(host);
        manager = host.AddComponent<SHELLGearSpinAuto>();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += SceneLoaded;
        Scan();
    }

    void OnDisable() => SceneManager.sceneLoaded -= SceneLoaded;

    void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        nextScan = 0f;
        Scan();
    }

    void Update()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 1.0f;
        Scan();
    }

    void Scan()
    {
        if (clip == null) clip = Resources.Load<AudioClip>(ClipPath);
        if (clip == null) return;

        // The project's main rotating saw/gear behaviour (directly verified in SawRotate.cs).
        foreach (var saw in FindObjectsByType<SawRotate>(FindObjectsSortMode.None))
            if (saw != null) Attach(saw.transform);

        // Replacement models may rotate using Animator or a different script.
        // Match only gear/saw-named objects; do NOT touch other game gimmicks.
        foreach (var visual in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            if (visual == null) continue;
            Transform candidate = null;
            Transform p = visual.transform;
            while (p != null)
            {
                if (IsGearName(p.name)) { candidate = p; break; }
                p = p.parent;
            }
            if (candidate == null && visual.sprite != null && IsGearName(visual.sprite.name))
                candidate = visual.transform;
            if (candidate != null) Attach(candidate);
        }
    }

    static bool IsGearName(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        string n = name.ToLowerInvariant().Replace('_', ' ').Replace('-', ' ');
        return n.StartsWith("saw") || n.Contains(" gear") || n.StartsWith("gear") ||
               n.StartsWith("cog") || n.Contains(" cog") || n.Contains("톱니") ||
               n.Contains("기어");
    }

    static void Attach(Transform target)
    {
        if (target == null || target.GetComponent<SHELLGearSpinEmitter>() != null) return;
        // If a matching child is already wired, don't add another source to its parent.
        if (target.GetComponentInChildren<SHELLGearSpinEmitter>() != null) return;
        target.gameObject.AddComponent<SHELLGearSpinEmitter>();
    }
}
