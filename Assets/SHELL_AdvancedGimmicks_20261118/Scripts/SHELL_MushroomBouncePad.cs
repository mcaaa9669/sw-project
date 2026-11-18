using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public sealed class SHELL_MushroomBouncePad : MonoBehaviour
{
    [Header("Animation")]
    public Sprite idle;
    public Sprite compressed;
    public Sprite burst;
    public Sprite recovered;

    [Header("Bounce physics")]
    [Min(1f)] public float bounceVelocity = 12f;
    [Min(0.02f)] public float cooldown = 0.28f;
    public bool requireLandingFromAbove = true;

    [Header("Optional sound")]
    public AudioClip bounceSound;
    [Range(0f, 1f)] public float soundVolume = 0.35f;

    SpriteRenderer visual;
    AudioSource audioSource;
    float nextBounceTime;
    Coroutine animationRoutine;

    void Awake()
    {
        visual = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        if (idle != null) visual.sprite = idle;
    }

    void OnCollisionEnter2D(Collision2D hit) { TryBounce(hit); }
    void OnCollisionStay2D(Collision2D hit) { TryBounce(hit); }

    void TryBounce(Collision2D hit)
    {
        if (Time.time < nextBounceTime) return;
        Rigidbody2D body = hit.rigidbody;
        if (body == null || body.bodyType != RigidbodyType2D.Dynamic) return;
        // Ignore collisions with the stem or sides to avoid surprise launches.
        if (requireLandingFromAbove && body.worldCenterOfMass.y < transform.position.y + 0.15f) return;
        if (body.linearVelocity.y > 0.75f) return;
        nextBounceTime = Time.time + cooldown;
        body.linearVelocity = new Vector2(body.linearVelocity.x, bounceVelocity);
        if (bounceSound != null && audioSource != null) audioSource.PlayOneShot(bounceSound, soundVolume);
        if (animationRoutine != null) StopCoroutine(animationRoutine);
        animationRoutine = StartCoroutine(BounceAnimation());
    }

    IEnumerator BounceAnimation()
    {
        if (compressed != null) visual.sprite = compressed;
        yield return new WaitForSeconds(0.07f);
        if (burst != null) visual.sprite = burst;
        yield return new WaitForSeconds(0.11f);
        if (recovered != null) visual.sprite = recovered;
        yield return new WaitForSeconds(0.12f);
        if (idle != null) visual.sprite = idle;
        animationRoutine = null;
    }
}
