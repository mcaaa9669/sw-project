using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
public class PoisonSwim : MonoBehaviour
{
    [Header("수면 점프 시작 깊이")]
    [SerializeField, Range(0.1f, 0.8f)]
    private float surfaceSubmergeRatio = 0.3f;

    private Collider2D bodyCollider;
    private Collider2D poisonArea;
    private bool surfaceJumping;

    [Header("수면 탈출 점프")]
    [SerializeField] private float surfaceJumpSpeed = 6f;

    [SerializeField] private float sinkGravityScale = 0.6f;
    [SerializeField] private float swimSpeed = 3f;
    [SerializeField] private float maxSinkSpeed = 2f;

    private Rigidbody2D rb;
    private float originalGravityScale;
    private bool isInPoison;
    private bool jumpHeld;
    public bool IsSwimming => isInPoison;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravityScale = rb.gravityScale;
        bodyCollider = GetComponent<Collider2D>();
    }

    // 기존 PlayerController에서 점프 입력을 전달
    public void SetJumpHeld(bool held)
    {
        if (jumpHeld != held)
            Debug.Log($"[수영 입력] 누름={held}, 독 안={isInPoison}");

        jumpHeld = held;
    }

    private void FixedUpdate()
    {
        if (!isInPoison || poisonArea == null) return;

        Bounds body = bodyCollider.bounds;
        float surfaceY = poisonArea.bounds.max.y;

        // 발이 이 높이에 오면, 하반신이 일부 잠긴 상태
        float jumpStartFeetY =
            surfaceY - body.size.y * surfaceSubmergeRatio;

        if (surfaceJumping)
        {
            // 점프 중에는 수영 코드로 세로 속도를 덮어쓰지 않음.
            // 다시 충분히 잠겼을 때 수영으로 복귀.
            if (rb.linearVelocity.y <= 0f)
            {
                surfaceJumping = false;
                rb.gravityScale = sinkGravityScale;
            }
            else
            {
                return;
            }
        }

        Vector2 velocity = rb.linearVelocity;

        if (jumpHeld)
        {
            // 이미 몸 전체가 나온 상태에서는 수면 점프 금지
            bool partlySubmerged = body.min.y < surfaceY;

            if (partlySubmerged && body.min.y >= jumpStartFeetY)
            {
                surfaceJumping = true;
                rb.gravityScale = originalGravityScale;
                velocity.y = surfaceJumpSpeed;
            }
            else if (body.min.y < jumpStartFeetY)
            {
                velocity.y = swimSpeed;
            }
        }
        else if (velocity.y < -maxSinkSpeed)
        {
            velocity.y = -maxSinkSpeed;
        }

        rb.linearVelocity = velocity;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<PoisonTrigger>(out _))
            return;

        poisonArea = other;
        isInPoison = true;
        rb.gravityScale = sinkGravityScale;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other != poisonArea) return;

        poisonArea = null;
        isInPoison = false;
        surfaceJumping = false;
        rb.gravityScale = originalGravityScale;
    }

    private void OnDisable()
    {
        ResetPoisonState();
    }

    public void ResetPoisonState()
    {
        isInPoison = false;
        jumpHeld = false;
        
        poisonArea = null;
        surfaceJumping = false;

        if (rb != null)
            rb.gravityScale = originalGravityScale;
    }
}
