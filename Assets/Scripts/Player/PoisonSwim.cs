using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PoisonSwim : MonoBehaviour
{
    [SerializeField] float sinkGravityScale = 0.6f;
    [SerializeField] float swimSpeed = 1.5f;
    [SerializeField] float maxSinkSpeed = 2f;
    [SerializeField] float surfaceJumpSpeed = 3f;

    Rigidbody2D rb;
    float originalGravityScale;
    bool isInPoison;
    bool jumpHeld;

    public bool IsSwimming => isInPoison;

    public bool IsSurfaceJumping { get; private set; }

    public void ClearSurfaceJump()
    {
        IsSurfaceJumping = false;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravityScale = rb.gravityScale;
    }

    public void SetJumpHeld(bool held)
    {
        if (jumpHeld != held)
            Debug.Log($"[수영 입력] 누름={held}, 독 안={isInPoison}", this);

        jumpHeld = held;
    }

    void FixedUpdate()
    {
        if (!isInPoison) return;

        Vector2 velocity = rb.linearVelocity;

        if (jumpHeld)
            velocity.y = swimSpeed;
        else if (velocity.y < -maxSinkSpeed)
            velocity.y = -maxSinkSpeed;

        rb.linearVelocity = velocity;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<PoisonTrigger>(out _))
            return;

        IsSurfaceJumping = false;
        isInPoison = true;
        rb.gravityScale = sinkGravityScale;
        Debug.Log($"[독 진입] 영역={other.name}, 독 안={isInPoison}", this);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.TryGetComponent<PoisonTrigger>(out _))
            return;

        bool leavingSurface =
            jumpHeld &&
            rb.linearVelocity.y > 0f &&
            rb.position.y >= other.bounds.max.y;

        isInPoison = false;
        rb.gravityScale = originalGravityScale;
        Debug.Log($"[독 이탈] 영역={other.name}, 독 안={isInPoison}, 수면 탈출={leavingSurface}", this);

        if (leavingSurface)
        {
            IsSurfaceJumping = true;

            Vector2 velocity = rb.linearVelocity;
            velocity.y = Mathf.Max(velocity.y, surfaceJumpSpeed);
            rb.linearVelocity = velocity;
        }
    }

    void OnDisable()
    {
        ResetPoisonState();
    }

    public void ResetPoisonState()
    {
        isInPoison = false;
        jumpHeld = false;
        IsSurfaceJumping = false;

        if (rb != null)
            rb.gravityScale = originalGravityScale;
    }
}