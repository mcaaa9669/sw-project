using UnityEngine;

[DisallowMultipleComponent]
public sealed class SHELLVFXFramePlayer : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private SpriteRenderer frameRenderer;
    [SerializeField, Min(0.01f)] private float duration = 0.36f;
    [SerializeField] private bool destroyAtEnd = true;
    [SerializeField] private bool effectEnabled = true;
    [SerializeField, Range(0f, 1f)] private float intensity = 1f;

    private SpriteRenderer spriteRenderer;
    private float elapsed;
    private int currentFrame = -1;

    public bool CanPlay => enabled && effectEnabled && intensity > 0f;
    public float Progress => Mathf.Clamp01(elapsed / duration);

    private void Awake()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        if (!CanPlay)
        {
            foreach (SpriteRenderer renderer in renderers)
                renderer.enabled = false;
            enabled = false;
            return;
        }

        if (intensity < 1f)
        {
            foreach (SpriteRenderer renderer in renderers)
            {
                Color color = renderer.color;
                color.a *= intensity;
                renderer.color = color;
            }
        }

        spriteRenderer = frameRenderer != null ? frameRenderer : GetComponent<SpriteRenderer>();
        if (spriteRenderer == null || frames == null || frames.Length == 0)
        {
            Debug.LogWarning("SHELLVFXFramePlayer requires a SpriteRenderer and animation frames.", this);
            enabled = false;
            return;
        }

        spriteRenderer.sprite = frames[0];
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        if (elapsed >= duration)
        {
            if (destroyAtEnd)
                Destroy(gameObject);
            else
                enabled = false;
            return;
        }

        int frame = Mathf.Min(frames.Length - 1, (int)(elapsed / duration * frames.Length));
        if (frame == currentFrame)
            return;

        currentFrame = frame;
        spriteRenderer.sprite = frames[frame];
    }

    public void Configure(Sprite[] animationFrames, float animationDuration, SpriteRenderer target = null)
    {
        frames = animationFrames;
        duration = animationDuration;
        frameRenderer = target;
    }
}
