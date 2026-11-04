using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
public class PoisonSwim : MonoBehaviour
{
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
        Debug.Log($"[영역 이탈] {other.name}");

        Debug.Log(
        $"[영역 진입] {other.name}, " +
        $"독 감지={other.GetComponentInParent<PoisonTrigger>() != null}"
        );

        if (!other.TryGetComponent<PoisonTrigger>(out _))
        return;

        isInPoison = true;
        rb.gravityScale = sinkGravityScale;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.TryGetComponent<PoisonTrigger>(out _))
            return;

        // 위로 헤엄치면서 독 영역의 위쪽으로 나왔는지 확인
        bool leavingSurface =
            jumpHeld &&
            rb.linearVelocity.y > 0f &&
            rb.position.y >= other.bounds.max.y;

        isInPoison = false;
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

        if (rb != null)
            rb.gravityScale = originalGravityScale;
    }
}
