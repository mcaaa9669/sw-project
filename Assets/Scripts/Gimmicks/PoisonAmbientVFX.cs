using System.Collections.Generic;
using UnityEngine;

// Visual effects only: no collider, damage, or player-controller changes.
// Put this on the visual part of a poison prefab (or its prefab variant).
[DisallowMultipleComponent]
public class PoisonAmbientVFX : MonoBehaviour
{
    [Header("Poison sprite (auto-detected when empty)")]
    [SerializeField] private SpriteRenderer poisonSprite;

    [Header("Amount (small numbers recommended)")]
    [Range(0, 20)] public int bubbleCount = 8;
    [Range(0, 10)] public int mistCount = 3;

    [Header("Movement")]
    [Range(0.1f, 3f)] public float riseSpeed = 1f;
    [Range(0.25f, 3f)] public float bubbleSize = 1f;

    private class Puff
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public bool mist;
        public float age;
        public float duration;
        public float startX;
        public float startY;
        public float size;
        public float drift;
        public float phase;
    }

    private readonly List<Puff> puffs = new List<Puff>();
    private Texture2D pixelTexture;
    private Sprite pixelSprite;

    private void Start()
    {
        if (poisonSprite == null)
            poisonSprite = GetComponentInChildren<SpriteRenderer>();

        if (poisonSprite == null)
        {
            Debug.LogWarning("PoisonAmbientVFX: Poison SpriteRenderer not found.", this);
            enabled = false;
            return;
        }

        MakePixelSprite();
        for (int i = 0; i < bubbleCount + mistCount; i++)
        {
            bool isMist = i >= bubbleCount;
            GameObject particle = new GameObject(isMist ? "PoisonMist_Visual" : "PoisonBubble_Visual");
            particle.transform.SetParent(transform, false);
            SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();
            sr.sprite = pixelSprite;
            sr.sortingLayerID = poisonSprite.sortingLayerID;
            sr.sortingOrder = poisonSprite.sortingOrder + 1;

            Puff puff = new Puff { transform = particle.transform, renderer = sr, mist = isMist };
            ResetPuff(puff);
            puff.age = Random.Range(0f, puff.duration); // stagger at start
            puffs.Add(puff);
        }
    }

    private void Update()
    {
        if (poisonSprite == null) return;

        foreach (Puff puff in puffs)
        {
            puff.age += Time.deltaTime * riseSpeed;
            if (puff.age >= puff.duration)
                ResetPuff(puff);

            float progress = Mathf.Clamp01(puff.age / puff.duration);
            float rise = poisonSprite.bounds.size.y * (puff.mist ? 0.75f : 0.55f);
            float x = puff.startX + Mathf.Sin(progress * 5f + puff.phase) * puff.drift * progress;
            float y = puff.startY + progress * rise;
            puff.transform.position = new Vector3(x, y, poisonSprite.transform.position.z - 0.01f);

            // Fixed world-space appearance regardless of poison prefab scale.
            Vector3 parentScale = transform.lossyScale;
            float sx = Mathf.Max(0.001f, Mathf.Abs(parentScale.x));
            float sy = Mathf.Max(0.001f, Mathf.Abs(parentScale.y));
            float size = puff.size * bubbleSize * (puff.mist ? 1.15f + progress * 0.45f : 1f);
            puff.transform.localScale = new Vector3(size / sx, size / sy, 1f);

            float fade = Mathf.Sin(progress * Mathf.PI);
            
            Color c = puff.mist
                ? new Color(1f, 0.65f, 0.86f, 0.12f * fade)
                : new Color(1f, 0.85f, 0.95f, 0.70f * fade);

            puff.renderer.color = c;
        }
    }

    private void ResetPuff(Puff puff)
    {
        Bounds b = poisonSprite.bounds;
        puff.age = 0f;
        puff.duration = puff.mist ? Random.Range(2.5f, 4f) : Random.Range(1.3f, 2.6f);
        puff.startX = Random.Range(b.min.x + b.size.x * 0.07f, b.max.x - b.size.x * 0.07f);
        puff.startY = b.max.y - b.size.y * Random.Range(0.08f, 0.25f);
        puff.size = Mathf.Clamp(b.size.y * (puff.mist ? 0.18f : Random.Range(0.07f, 0.12f)), 0.035f, 0.25f);
        puff.drift = Random.Range(-0.15f, 0.15f) * b.size.y;
        puff.phase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void MakePixelSprite()
    {
        const int n = 10;
        pixelTexture = new Texture2D(n, n, TextureFormat.RGBA32, false);
        pixelTexture.filterMode = FilterMode.Point;
        pixelTexture.wrapMode = TextureWrapMode.Clamp;
        Color32[] colors = new Color32[n * n];

        for (int y = 0; y < n; y++)
        for (int x = 0; x < n; x++)
        {
            float dx = (x - 4.5f) / 4.5f;
            float dy = (y - 4.5f) / 4.5f;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            byte alpha = (byte)(dist <= 0.58f ? 80 : dist <= 0.88f ? 240 : 0);
            // Tiny upper-left highlight keeps the bubble readable in pixel art.
            if ((x == 3 || x == 4) && y == 7) alpha = 255;
            colors[y * n + x] = new Color32(255, 255, 255, alpha);
        }

        pixelTexture.SetPixels32(colors);
        pixelTexture.Apply();
        pixelSprite = Sprite.Create(pixelTexture, new Rect(0, 0, n, n),
            new Vector2(0.5f, 0.5f), n);
    }

    private void OnDestroy()
    {
        if (pixelSprite != null) Destroy(pixelSprite);
        if (pixelTexture != null) Destroy(pixelTexture);
    }
}
