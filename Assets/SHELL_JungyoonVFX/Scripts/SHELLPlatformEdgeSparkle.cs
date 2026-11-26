using UnityEngine;

[DisallowMultipleComponent]
public sealed class SHELLPlatformEdgeSparkle : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] glints = System.Array.Empty<SpriteRenderer>();
    [SerializeField, Range(0.3f, 0.4f)] private float duration = 0.35f;
    [SerializeField] private bool effectEnabled = true;
    [SerializeField, Range(0f, 1f)] private float intensity = 0.65f;

    private Vector2 extents = new Vector2(1.8f, 0.35f);
    private Color tint = new Color(0.88f, 0.94f, 0.64f);
    private Renderer targetSurface;
    private float elapsed;

    public bool CanPlay => enabled && effectEnabled && intensity > 0f;

    public void FitToSurface(Vector2 surfaceExtents, Renderer surface)
    {
        targetSurface = surface;
        extents = surfaceExtents;
        if (surface.gameObject.layer == LayerMask.NameToLayer("SpiritWorld"))
            tint = new Color(0.65f, 0.9f, 0.98f);

        foreach (SpriteRenderer glint in glints)
        {
            glint.sortingLayerID = surface.sortingLayerID;
            glint.sortingOrder = surface.sortingOrder + 2;
        }
        DrawGlints(0f);
    }

    private void Awake()
    {
        DrawGlints(0f);
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        if (elapsed >= duration || (targetSurface != null && !targetSurface.enabled))
        {
            Destroy(gameObject);
            return;
        }
        DrawGlints(elapsed / duration);
    }

    private void OnDisable()
    {
        Destroy(gameObject);
    }

    private void DrawGlints(float progress)
    {
        for (int i = 0; i < glints.Length; i++)
        {
            SpriteRenderer glint = glints[i];
            float flash = Mathf.Clamp01(1f - Mathf.Abs(progress - (0.16f + i * 0.115f)) * 5.5f);
            glint.enabled = CanPlay && flash > 0f;
            if (!glint.enabled)
                continue;

            Vector2 position = i switch
            {
                0 => new Vector2(-extents.x, extents.y * 0.2f),
                1 => new Vector2(-extents.x * 0.8f, extents.y),
                2 => new Vector2(-extents.x * 0.34f, extents.y),
                3 => new Vector2(extents.x * 0.14f, extents.y),
                4 => new Vector2(extents.x * 0.72f, extents.y),
                _ => new Vector2(extents.x, extents.y * 0.45f)
            };
            glint.transform.localPosition = position;
            glint.transform.localScale = Vector3.one * (0.76f + flash * 0.53f);
            glint.color = new Color(tint.r, tint.g, tint.b, flash * intensity);
        }
    }
}
