using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PoisonSwim : MonoBehaviour
{
    [SerializeField] private float sinkGravityScale = 0.6f;
    [SerializeField] private float swimSpeed = 3f;
    [SerializeField] private float maxSinkSpeed = 2f;

    private Rigidbody2D rb;
    private float originalGravityScale;
    private bool isInPoison;
    private bool jumpHeld;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravityScale = rb.gravityScale;
    }

    // 기존 PlayerController에서 점프 입력을 전달
    public void SetJumpHeld(bool held)
    {
        jumpHeld = held;
    }

    private void FixedUpdate()
    {
        if (!isInPoison) return;

        Vector2 velocity = rb.linearVelocity;

        if (jumpHeld)
        {
            velocity.y = swimSpeed;
        }
        else if (velocity.y < -maxSinkSpeed)
        {
            velocity.y = -maxSinkSpeed;
        }

        rb.linearVelocity = velocity;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.GetComponentInParent<PoisonTrigger>())
            return;

        isInPoison = true;
        rb.gravityScale = sinkGravityScale;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.GetComponentInParent<PoisonTrigger>())
            return;

        isInPoison = false;
        rb.gravityScale = originalGravityScale;
    }

    private void OnDisable()
    {
        if (rb != null)
            rb.gravityScale = originalGravityScale;

        isInPoison = false;
        jumpHeld = false;
    }
}