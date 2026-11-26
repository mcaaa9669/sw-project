using UnityEngine;

[DisallowMultipleComponent]
public sealed class SHELLForestLeaves : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SpriteRenderer[] leaves = System.Array.Empty<SpriteRenderer>();
    [SerializeField] private bool effectEnabled = true;
    [SerializeField, Range(0f, 1f)] private float intensity = 0.75f;
    [SerializeField, Range(3, 6)] private int leafCount = 5;

    private Camera sceneCamera;
    private float elapsed;

    public bool CanPlay => enabled && effectEnabled && intensity > 0f;

    private void Awake()
    {
        HideLeaves();
    }

    private void LateUpdate()
    {
        if (targetCamera == null && sceneCamera == null)
            sceneCamera = Camera.main;
        Camera camera = targetCamera != null ? targetCamera : sceneCamera;
        if (!CanPlay || camera == null)
        {
            HideLeaves();
            return;
        }

        elapsed += Time.deltaTime;
        float depth = Vector3.Dot(transform.position - camera.transform.position, camera.transform.forward);
        for (int i = 0; i < leaves.Length; i++)
        {
            SpriteRenderer leaf = leaves[i];
            leaf.enabled = i < leafCount;
            if (!leaf.enabled)
                continue;

            float phase = i * 2.17f;
            float x = Mathf.Repeat(0.08f + i * 0.37f + elapsed * (0.021f + i * 0.002f)
                + 0.028f * Mathf.Sin(elapsed * 0.55f + phase), 1.08f) - 0.04f;
            float y = 1.04f - Mathf.Repeat((i + 0.55f) / leafCount + elapsed * 0.045f, 1.08f);
            leaf.transform.position = camera.ViewportToWorldPoint(new Vector3(x, y, depth));
            leaf.transform.rotation = Quaternion.Euler(0f, 0f, 28f * Mathf.Sin(elapsed * 1.3f + phase));
            float scale = 0.34f + 0.12f * Mathf.Sin(elapsed * 1.8f + phase);
            leaf.transform.localScale = new Vector3(scale, 0.39f + (i % 3) * 0.05f, 1f);

            float edgeFade = Mathf.Clamp01(Mathf.Min(x, 1f - x, y, 1f - y) / 0.08f);
            leaf.color = new Color(0.83f, 0.9f, 0.68f, intensity * edgeFade * (0.55f + (i % 3) * 0.09f));
        }
    }

    private void OnDisable()
    {
        HideLeaves();
    }

    private void HideLeaves()
    {
        foreach (SpriteRenderer leaf in leaves)
            leaf.enabled = false;
    }
}
