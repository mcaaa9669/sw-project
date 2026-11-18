using System.Collections;
using UnityEngine;

// V2: width is editable directly in Inspector without manually resizing art or collider.
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public sealed class SHELL_CrumblingMossPlatform : MonoBehaviour
{
    [Header("Sprites - 32px tiles / three tile cap-center-cap strip")]
    public Sprite intact;
    public Sprite cracked;
    public Sprite fractured;
    public Sprite collapsing;

    [Header("TEAM SETTINGS - Platform Width (world units)")]
    [Min(2.0f)] public float platformWidth = 3f;
    [Min(0.08f)] public float collisionThickness = 0.18f;
    public float collisionTopY = 0.30f;

    [Header("Break and restoration delay")]
    [Min(0.05f)] public float firstCrackTime = 0.30f;
    [Min(0.05f)] public float secondCrackTime = 0.27f;
    [Min(0.05f)] public float breakTime = 0.20f;
    [Min(0.1f)] public float restoreTime = 3f;
    public bool restoreAutomatically = true;

    [Header("Audio")]
    public AudioClip crackSound;
    [Range(0f, 1f)] public float soundVolume = 0.35f;

    private SpriteRenderer visual;
    private BoxCollider2D solidCollider;
    private AudioSource audioSource;
    private Coroutine breaking;
    private bool isBroken;

    void Awake()
    {
        visual = GetComponent<SpriteRenderer>();
        solidCollider = GetComponent<BoxCollider2D>();
        audioSource = GetComponent<AudioSource>();
        if (intact != null) visual.sprite = intact;
        UpdateWidth();
    }

    void OnValidate() { UpdateWidth(); }

    public void UpdateWidth()
    {
        float w = Mathf.Max(2f, platformWidth);
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(w, 1f);
        }
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.offset = new Vector2(0, collisionTopY);
            col.size = new Vector2(Mathf.Max(0.4f, w - 0.12f), Mathf.Max(0.08f, collisionThickness));
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Rigidbody2D body = collision.rigidbody;
        if (body == null || body.bodyType != RigidbodyType2D.Dynamic) return;
        if (breaking == null && !isBroken) breaking = StartCoroutine(BreakRoutine());
    }

    IEnumerator BreakRoutine()
    {
        yield return new WaitForSeconds(firstCrackTime);
        SetVisual(cracked);
        if (crackSound != null && audioSource != null) audioSource.PlayOneShot(crackSound, soundVolume);
        yield return new WaitForSeconds(secondCrackTime);
        SetVisual(fractured);
        yield return new WaitForSeconds(breakTime);
        SetVisual(collapsing);
        if (solidCollider != null) solidCollider.enabled = false;
        isBroken = true;
        if (restoreAutomatically)
        {
            yield return new WaitForSeconds(restoreTime);
            ResetPlatform();
        }
        breaking = null;
    }

    void SetVisual(Sprite sprite) { if (visual != null && sprite != null) visual.sprite = sprite; }

    public void ResetPlatform()
    {
        if (breaking != null) { StopCoroutine(breaking); breaking = null; }
        isBroken = false;
        if (solidCollider == null) solidCollider = GetComponent<BoxCollider2D>();
        if (visual == null) visual = GetComponent<SpriteRenderer>();
        if (solidCollider != null) solidCollider.enabled = true;
        SetVisual(intact);
        UpdateWidth();
    }
}
