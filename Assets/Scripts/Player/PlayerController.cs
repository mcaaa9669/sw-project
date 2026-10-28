using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour,IKillable
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float jumpForce = 5f;
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundRadius = 0.1f;
    [SerializeField] LayerMask groundLayer;

    [Header("Death")]
    [SerializeField] Shell shellPrefab;                        // Shell.prefab 연결
    [SerializeField] float poisonDelay = 3f;                   // 독 사망까지 걸리는 시간
    [SerializeField] SoulWorldController soulWorldController;  // 사망 신호를 보낼 곳


    Rigidbody2D rb;
    float inputX;
    bool jumpRequested;
    SpriteRenderer sr;
    Collider2D col;

    bool isDead;
    Coroutine poisonRoutine;

    public bool IsDead => isDead;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        // Inspector에서 연결 안 했으면 씬에서 찾아봄
        if (soulWorldController == null)
            soulWorldController = FindFirstObjectByType<SoulWorldController>();
    }


        void Update()
    {
        if (isDead) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        inputX = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
        if (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) jumpRequested = true;
    }

    void FixedUpdate()
    {
        if (isDead) return;

        bool grounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
        rb.linearVelocity = new Vector2(inputX * moveSpeed, rb.linearVelocity.y);

        if (jumpRequested && grounded)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpRequested = false;
    }
    // Hazard가 보내는 사망 신호 (IKillable)
    public void Kill(DeathType deathType)
    {
        if (isDead) return;

        switch (deathType)
        {
            case DeathType.Spike:
                Die();
                SpawnShell().OnSpikeDeath();      // 그 자리에 고정
                break;

            case DeathType.laser:
                Die();
                SpawnShell().OnLaserDeath();      // 물리 적용 (떨어짐, 미끄러짐)
                break;

            case DeathType.poison:
                // 아직 사망 확정 아님. 이미 중독 중이면 타이머를 새로 시작하지 않음
                if (poisonRoutine == null)
                    poisonRoutine = StartCoroutine(PoisonRoutine());
                break;

            case DeathType.Fall:
                Die();                            // shell 없음
                break;

            default:
                Debug.LogWarning($"[PlayerController] 아직 처리하지 않은 사망 종류: {deathType}");
                break;
        }
    }

    // 독: poisonDelay 뒤 '그때의 위치'에서 사망
    IEnumerator PoisonRoutine()
    {
        yield return new WaitForSeconds(poisonDelay);
        poisonRoutine = null;

        if (isDead) yield break;                  // 그 사이 다른 이유로 이미 죽었으면 무시
        Die();
        SpawnShell().OnPoisonDeath();             // 그 자리에 고정
    }

    // 사망 확정: 플레이어 정지 + 영혼세계에 사망 신호 1회 전달
    void Die()
    {
        isDead = true;

        // 중독 중에 다른 이유로 죽으면 독 타이머 취소
        if (poisonRoutine != null)
        {
            StopCoroutine(poisonRoutine);
            poisonRoutine = null;
        }

        inputX = 0f;
        jumpRequested = false;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        if (col) col.enabled = false;
        if (sr) sr.enabled = false;

        // 사망 신호 전달 → death +1, 5초 타이머 시작, OnSoulWorldEntered 발생
        if (soulWorldController != null)
            soulWorldController.HandlePlayerDeath();
        else
            Debug.LogWarning("[PlayerController] SoulWorldController가 없어 사망 신호를 보내지 못했습니다.");
    }

    // 현재 위치에 shell 생성
    Shell SpawnShell()
    {
        return Instantiate(shellPrefab, transform.position, Quaternion.identity);
    }

    // 부활 (리스폰·영혼세계 복귀 쪽에서 호출)
    public void Revive(Vector3 position)
    {
        transform.position = position;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = true;
        if (col) col.enabled = true;
        if (sr) sr.enabled = true;
        isDead = false;
    }
}