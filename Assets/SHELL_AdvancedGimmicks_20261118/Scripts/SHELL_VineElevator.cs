using UnityEngine;

// V2: set travelOffset.Y in Inspector to control total vertical travel distance.
// Green leaves trail behind the lift while moving. No yellow glow sprites required.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(SpriteRenderer))]
public sealed class SHELL_VineElevator : MonoBehaviour
{
    [Header("TEAM SETTINGS - Move range (X=0; Y=height, negative Y=down)")]
    public Vector2 travelOffset = new Vector2(0f, 3.2f);
    [Min(0.1f)] public float speed = 1.4f;
    [Min(0f)] public float pauseAtDestination = 0.6f;
    public bool autoCycle = false;
    public bool returnAfterArrival = true;

    [Header("Appearance")]
    public Sprite restingSprite;
    public Sprite movingSprite;
    public ParticleSystem fallingLeaves;

    [Header("Optional audio")]
    public AudioClip motionSound;
    [Range(0f, 1f)] public float soundVolume = 0.28f;

    Rigidbody2D body;
    SpriteRenderer visual;
    AudioSource audioSource;
    Vector2 origin;
    bool moving;
    bool movingUp;
    float pauseUntil;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        visual = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        origin = body.position;
        if (restingSprite != null) visual.sprite = restingSprite;
        if (fallingLeaves == null)
        {
            Transform fx = transform.Find("SHELL_FallingLeaves_FX");
            if (fx != null) fallingLeaves = fx.GetComponent<ParticleSystem>();
        }
        if (fallingLeaves != null) fallingLeaves.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void Start() { if (autoCycle) Activate(); }

    void FixedUpdate()
    {
        if (!moving)
        {
            if (Time.time >= pauseUntil)
            {
                if (autoCycle) { if (movingUp) Deactivate(); else Activate(); }
                else if (returnAfterArrival && movingUp) Deactivate();
            }
            return;
        }
        Vector2 dest = origin + (movingUp ? travelOffset : Vector2.zero);
        Vector2 next = Vector2.MoveTowards(body.position, dest, speed * Time.fixedDeltaTime);
        body.MovePosition(next);
        if (Vector2.Distance(next, dest) <= 0.001f)
        {
            moving = false;
            pauseUntil = Time.time + pauseAtDestination;
            if (restingSprite != null) visual.sprite = restingSprite;
            if (fallingLeaves != null) fallingLeaves.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    public void Activate() { BeginMotion(true); }
    public void Deactivate() { BeginMotion(false); }
    public void Toggle() { BeginMotion(!movingUp); }
    public void SetActiveState(bool active) { BeginMotion(active); }
    public void EnableAutoCycleInDemo() { autoCycle = true; }

    void BeginMotion(bool up)
    {
        movingUp = up;
        moving = true;
        if (movingSprite != null) visual.sprite = movingSprite;
        if (motionSound != null && audioSource != null) audioSource.PlayOneShot(motionSound, soundVolume);
        if (fallingLeaves != null && !fallingLeaves.isPlaying) fallingLeaves.Play();
    }

    public void ReturnToStartInstantly()
    {
        moving = false;
        movingUp = false;
        body.position = origin;
        transform.position = new Vector3(origin.x, origin.y, transform.position.z);
        pauseUntil = Time.time + pauseAtDestination;
        if (restingSprite != null) visual.sprite = restingSprite;
        if (fallingLeaves != null) fallingLeaves.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f,0.95f,0.35f,1f);
        Vector3 start = Application.isPlaying ? new Vector3(origin.x,origin.y,transform.position.z) : transform.position;
        Vector3 end = start + new Vector3(travelOffset.x, travelOffset.y, 0);
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(start, 0.13f);
        Gizmos.DrawWireSphere(end, 0.13f);
    }
}
