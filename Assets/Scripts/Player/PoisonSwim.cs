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

    // [중력 반전 대응] 플레이어 중력 방향 (+1 평소, -1 반전)
    // 세로 방향 계산에 g를 곱해 "플레이어 기준 위쪽"으로 처리함.
    // 평소 상태(g = +1)에서는 기존 동작과 완전히 같음.
    PlayerController player;
    float GravitySign => player != null && player.IsGravityFlipped ? -1f : 1f;

    public bool IsSwimming => isInPoison;

    public bool IsSurfaceJumping { get; private set; }

    public void ClearSurfaceJump()
    {
        IsSurfaceJumping = false;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // [중력 반전 대응] 크기만 저장하고 방향은 적용할 때 GravitySign으로 곱함
        originalGravityScale = Mathf.Abs(rb.gravityScale);
        player = GetComponent<PlayerController>();
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

        // [중력 반전 대응] 반전 상태면 수영은 아래로, 가라앉기는 위로
        float g = GravitySign;
        Vector2 velocity = rb.linearVelocity;

        if (jumpHeld)
            velocity.y = swimSpeed * g;
        else if (velocity.y * g < -maxSinkSpeed)
            velocity.y = -maxSinkSpeed * g;

        rb.linearVelocity = velocity;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<PoisonTrigger>(out _))
            return;

        IsSurfaceJumping = false;
        isInPoison = true;
        rb.gravityScale = sinkGravityScale * GravitySign;   // [중력 반전 대응] 부호 유지
        Debug.Log($"[독 진입] 영역={other.name}, 독 안={isInPoison}", this);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.TryGetComponent<PoisonTrigger>(out _))
            return;

        // [중력 반전 대응] 반전 상태면 독의 아랫면을 수면으로 보고, 아래로 빠져나갈 때 수면 탈출
        float g = GravitySign;
        bool leavingSurface =
            jumpHeld &&
            rb.linearVelocity.y * g > 0f &&
            (g > 0f ? rb.position.y >= other.bounds.max.y
                    : rb.position.y <= other.bounds.min.y);

        isInPoison = false;
        rb.gravityScale = originalGravityScale * g;
        Debug.Log($"[독 이탈] 영역={other.name}, 독 안={isInPoison}, 수면 탈출={leavingSurface}", this);

        if (leavingSurface)
        {
            IsSurfaceJumping = true;

            Vector2 velocity = rb.linearVelocity;
            velocity.y = g * Mathf.Max(velocity.y * g, surfaceJumpSpeed);   // [중력 반전 대응]
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
            rb.gravityScale = originalGravityScale * GravitySign;   // [중력 반전 대응]
    }
}