using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;

public static class SHELLGimmickVFX
{
    private const string ButtonPath = "SHELL_JungyoonVFX/FX_ButtonPress";
    private const string PlatformPath = "SHELL_JungyoonVFX/FX_PlatformReveal";
    private static GameObject buttonPrefab;
    private static GameObject platformPrefab;

    public static void PlayButtonPress(SpriteRenderer button, Transform fallback)
    {
        Transform anchor = button != null ? button.transform : fallback;
        // Some plates use both pressure scripts on the same visible button.
        if (anchor.Find("FX_ButtonPress(Clone)") != null)
            return;

        if (buttonPrefab == null)
            buttonPrefab = Resources.Load<GameObject>(ButtonPath);
        if (buttonPrefab == null || !buttonPrefab.activeSelf ||
            !buttonPrefab.TryGetComponent(out SHELLVFXFramePlayer settings) || !settings.CanPlay)
            return;

        Bounds bounds = button != null ? button.bounds : new Bounds(anchor.position, Vector3.one * 0.6f);
        Vector3 origin = bounds.center + Vector3.up * (bounds.extents.y * 0.3f);
        GameObject pulse = Object.Instantiate(buttonPrefab, origin, Quaternion.identity);
        float scale = Mathf.Clamp(bounds.size.x * 0.55f, 0.25f, 0.62f);
        pulse.transform.localScale = new Vector3(scale, scale * 0.64f, 1f);
        pulse.transform.SetParent(anchor, true);

        if (button != null)
        {
            SpriteRenderer renderer = pulse.GetComponent<SpriteRenderer>();
            renderer.sortingLayerID = button.sortingLayerID;
            renderer.sortingOrder = button.sortingOrder + 2;
        }
    }

    public static void InvokeWithPlatformReveal(UnityEvent signal)
    {
        if (signal == null)
            return;

        // Capture visibility before the original callbacks, then check their final result.
        List<GameObject> hiddenTargets = null;
        for (int i = 0; i < signal.GetPersistentEventCount(); i++)
        {
            if (signal.GetPersistentMethodName(i) != nameof(GameObject.SetActive) ||
                signal.GetPersistentTarget(i) is not GameObject target || target == null ||
                target.activeInHierarchy)
                continue;

            hiddenTargets ??= new List<GameObject>();
            if (!hiddenTargets.Contains(target))
                hiddenTargets.Add(target);
        }

        signal.Invoke();

        if (hiddenTargets == null)
            return;

        foreach (GameObject target in hiddenTargets)
            PlayPlatformReveal(target);
    }

    public static void PlayPlatformReveal(GameObject platform)
    {
        if (platform == null || !platform.activeInHierarchy ||
            platform.transform.Find("FX_PlatformReveal(Clone)") != null ||
            !TryGetPlatformBounds(platform, out Bounds bounds, out Renderer surface))
            return;

        if (platformPrefab == null)
            platformPrefab = Resources.Load<GameObject>(PlatformPath);
        if (platformPrefab == null || !platformPrefab.activeSelf ||
            !platformPrefab.TryGetComponent(out SHELLPlatformEdgeSparkle settings) || !settings.CanPlay)
            return;

        GameObject effect = Object.Instantiate(platformPrefab, bounds.center, Quaternion.identity);
        effect.transform.SetParent(platform.transform, true);
        effect.GetComponent<SHELLPlatformEdgeSparkle>().FitToSurface(bounds.extents, surface);
    }

    private static bool TryGetPlatformBounds(GameObject target, out Bounds bounds, out Renderer surface)
    {
        bounds = default;
        surface = null;
        int groundLayer = LayerMask.NameToLayer("Ground");
        int spiritLayer = LayerMask.NameToLayer("SpiritWorld");
        bool movingPlatform = target.GetComponentInChildren<MovingPlatform>() != null;
        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>())
        {
            int layer = renderer.gameObject.layer;
            if (!renderer.enabled || renderer is not (SpriteRenderer or TilemapRenderer) ||
                (!movingPlatform && layer != groundLayer && layer != spiritLayer) ||
                renderer.bounds.size.x <= 0f || renderer.bounds.size.y <= 0f)
                continue;

            if (surface == null)
            {
                surface = renderer;
                bounds = renderer.bounds;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        return surface != null;
    }
}
