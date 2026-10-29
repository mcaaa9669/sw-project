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
   


    [Header("Respawn")]
    [SerializeField] RespawnPoint respawnPoint;                // 현재 부활 지점. 비우면 시작 위치에서 부활
    [SerializeField] float respawnDelay = 1f;                  // 사망 후 부활까지 걸리는 시간

    [Header("Soul World (아직 사용 안 함)")]
    [SerializeField] bool useSoulWorld = false;                // 켜면 사망 시 영혼세계로 전환
    [SerializeField] SoulWorldController soulWorldController;  // 사망 신호를 보낼 곳

    Rigidbody2D rb;
    float inputX;
    bool jumpRequested;
    SpriteRenderer sr;
    Collider2D col;

    bool isDead;
    Coroutine poisonRoutine;
    Vector3 startPosition;

    public bool IsDead => isDead;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        startPosition = transform.position;

        // Inspector에서 연결 안 했으면 씬에서 찾아봄
        if (useSoulWorld&&soulWorldController == null)
            soulWorldController = FindFirstObjectByType<SoulWorldController>();
    }

    void OnEnable()
    {
        if (useSoulWorld && soulWorldController != null)
            soulWorldController.OnReturnRequested += HandleReturn;
    }

    void OnDisable()
    {
        if (soulWorldController != null)
            soulWorldController.OnReturnRequested -= HandleReturn;
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

        if (useSoulWorld && soulWorldController != null)
        {
            // 영혼세계 사용 시: death +1, 5초 타이머 → 끝나면 HandleReturn()에서 부활
            soulWorldController.HandlePlayerDeath();
        }
        else
        {
            // 지금은 영혼세계 없이 respawnDelay(1초) 뒤 자동 부활
            StartCoroutine(RespawnAfterDelay());
        }
    }

    // 부활 처리 (자동 부활 또는 영혼세계 복귀 신호)
    void HandleReturn()
    {
        if (!isDead) return;
        Revive(respawnPoint != null ? respawnPoint.transform.position : startPosition);
    }

    // 체크포인트 갱신 (RespawnPoint가 호출). 앞 순서 지점으로는 되돌아가지 않음
    public bool SetRespawnPoint(RespawnPoint point)
    {
        if (point == null || point == respawnPoint) return false;
        if (respawnPoint != null && point.Order < respawnPoint.Order) return false;

        respawnPoint = point;
        return true;
    }

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        HandleReturn();
    }

    // 현재 위치에 shell 생성
    Shell SpawnShell()
    {
        return Instantiate(shellPrefab, transform.position, Quaternion.identity);
    }

    // 부활
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