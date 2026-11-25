// SHELL SFX: only 11, 12, 16, 17, 18 are active.
// Original class/folder names are kept for compatibility with the previous 8-pack.
// 13 Saw / 14 Conveyor / 15 Laser are deliberately excluded from THIS pack.
using System.Collections.Generic;
using UnityEngine;

public static class SHELLSFX8
{
    public const string LeverOn = "11_lever_on";
    public const string LeverOff = "12_lever_off";
    public const string PlatformMove = "16_platform_move";
    public const string Checkpoint = "17_checkpoint";
    public const string MenuHover = "18_menu_hover";

    // Old public constants kept only to avoid breaking scripts written for the 8-pack.
    // The corresponding original clips are NEVER played by this helper.
    public const string Saw = "13_saw_spin_LOOP";
    public const string Conveyor = "14_conveyor_LOOP";
    public const string LaserOn = "15_laser_activate";

    private static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    private static AudioSource oneShotSource;

    public static bool IsEnabled(string key)
    {
        return key == LeverOn || key == LeverOff || key == PlatformMove ||
               key == Checkpoint || key == MenuHover;
    }

    public static AudioClip Clip(string key)
    {
        // Reject previous 13/14/15 sounds even if WAVs were left in a prior installation.
        if (!IsEnabled(key)) return null;
        if (cache.TryGetValue(key, out AudioClip cached)) return cached;
        AudioClip clip = Resources.Load<AudioClip>("SHELL_SFX8/" + key);
        if (clip != null) cache[key] = clip;
        else Debug.LogWarning("[SHELL SFX5] Missing WAV: " + key);
        return clip;
    }

    public static void Play(string key, float volume = 0.6f)
    {
        AudioClip clip = Clip(key);
        if (clip == null || !Application.isPlaying) return;
        if (oneShotSource == null)
        {
            GameObject host = new GameObject("__SHELL_SFX5_OneShots");
            Object.DontDestroyOnLoad(host);
            oneShotSource = host.AddComponent<AudioSource>();
            oneShotSource.playOnAwake = false;
            oneShotSource.loop = false;
            oneShotSource.spatialBlend = 0f;
            oneShotSource.volume = 1f;
        }
        oneShotSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    public static void PlayLeverOn() => Play(LeverOn, 0.65f);
    public static void PlayLeverOff() => Play(LeverOff, 0.65f);
    public static void PlayPlatformMove() => Play(PlatformMove, 0.47f);
    public static void PlayCheckpoint() => Play(Checkpoint, 0.55f);
    public static void PlayMenuHover() => Play(MenuHover, 0.28f);

    // Backward compatibility: explicitly do NOT play the excluded laser sound.
    public static void PlayLaserOn() { }
}
