using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, IKillable
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float jumpForce = 5f;
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundRadius = 0.1f;
    [SerializeField] LayerMask groundLayer;

    [Header("Death")]
    [SerializeField] Shell shellPrefab;                        // Shell.prefab 연결
    [SerializeField] float poisonDelay = 5f;                   // 독 사망까지 걸리는 시간 (명세: 5초)

    [Header("Respawn")]
    [SerializeField] RespawnPoint respawnPoint;                // 현재 부활 지점. 비우면 시작 위치에서 부활
    [SerializeField] float respawnDelay = 1f;                  // 영혼세계 없이 부활할 때 걸리는 시간

    [Header("Soul World")]
    [SerializeField] bool useSoulWorld = true;                 // 끄면 영혼세계 없이 바로 부활
    [SerializeField] SoulWorldController soulWorldController;  // 비우면 씬에서 자동으로 찾음
    [SerializeField] Color soulTint = new Color(0.6f, 0.85f, 1f, 0.5f); // 영혼 외형 (임시)

    Rigidbody2D rb;
    Collider2D col;
    PoisonSwim poisonSwim;
    SpriteRenderer[] renderers;
    Color[] originalColors;
    bool[] originalEnabled;

    float inputX;
    bool jumpRequested;

    bool isDead;        // 현실의 몸이 죽었는지 (영혼 상태 포함)
    bool isSoul;        // 영혼으로 활동 중인지
    Coroutine poisonRoutine;
    Vector3 startPosition;
    Collider2D soulShellCollider;   // 영혼이 빠져나온 shell (겹쳐 있으므로 충돌 무시)

    public bool IsDead => isDead;
    public bool IsSoul => isSoul;
    bool CanControl => !isDead || isSoul;   // 살아 있거나 영혼일 때만 조작 가능

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        poisonSwim = GetComponent<PoisonSwim>();
        startPosition = transform.position;

        // 자식 Sprite까지 포함해서 외형 변경
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[renderers.Length];
        originalEnabled = new bool[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].color;
            originalEnabled[i] = renderers[i].enabled;
        }

        if (useSoulWorld && soulWorldController == null)
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
        if (!CanControl) return;
        if (Time.timeScale == 0f) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        if (poisonSwim != null)
            poisonSwim.SetJumpHeld(kb.spaceKey.isPressed || kb.wKey.isPressed);

        inputX = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
        if (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) jumpRequested = true;
    }

    void FixedUpdate()
    {
        if (!CanControl) return;

        bool grounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
        rb.linearVelocity = new Vector2(inputX * moveSpeed, rb.linearVelocity.y);

        if (jumpRequested && grounded)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpRequested = false;
    }

    // ===== 사망 =====

    // Hazard가 보내는 사망 신호 (IKillable). 영혼 상태에서는 무시
    public void Kill(DeathType deathType)
    {
        if (isDead) return;

        switch (deathType)
        {
            case DeathType.Spike:
            case DeathType.laser:
            case DeathType.saw:
            case DeathType.Fall:
                Die(deathType);
                break;

            case DeathType.poison:
                // 아직 사망 확정 아님. 이미 중독 중이면 타이머를 새로 시작하지 않음
                if (poisonRoutine == null)
                    poisonRoutine = StartCoroutine(PoisonRoutine());
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

        if (isDead) yield break;
        Die(DeathType.poison);
    }

    // 사망 확정: shell 생성 → 영혼세계 진입 (영혼세계가 없으면 잠시 뒤 부활)
    void Die(DeathType deathType)
    {
        isDead = true;

        if (poisonRoutine != null)
        {
            StopCoroutine(poisonRoutine);
            poisonRoutine = null;
        }

        inputX = 0f;
        jumpRequested = false;

    if (poisonSwim != null)
    {
        poisonSwim.ResetPoisonState();
        poisonSwim.enabled = false;
    }
        rb.linearVelocity = Vector2.zero;

        // shell을 먼저 만들어야, 영혼세계 진입 시 현실세계와 함께 멈춤
        Shell shell = SpawnShellFor(deathType);

        if (useSoulWorld && soulWorldController != null && shell != null)
        {
            BecomeSoul(shell);
        }
        else
        {
            // 낭떠러지(shell 없음) 또는 영혼세계 미사용: 몸을 숨기고 잠시 뒤 부활
            rb.simulated = false;
            if (col) col.enabled = false;
            SetBodyVisible(false);
            StartCoroutine(RespawnAfterDelay());
        }
    }

    // 사망 종류별 shell 생성 (shell이 없는 사망은 null)
    Shell SpawnShellFor(DeathType deathType)
    {
        if (shellPrefab == null) return null;

        switch (deathType)
        {
            case DeathType.Spike:
                {
                    Shell shell = SpawnShell();
                    shell.OnSpikeDeath();       // 그 자리에 고정
                    return shell;
                }
            case DeathType.poison:
                {
                    Shell shell = SpawnShell();
                    shell.OnPoisonDeath();      // 그 자리에 고정
                    return shell;
                }
            case DeathType.laser:
            case DeathType.saw:
                {
                    Shell shell = SpawnShell();
                    shell.OnLaserDeath();       // 물리 적용 (떨어짐, 미끄러짐)
                    return shell;
                }
            default:
                return null;                // Fall 등: shell 없음
        }
    }

    Shell SpawnShell()
    {
        return Instantiate(shellPrefab, transform.position, Quaternion.identity);
    }

    // ===== 영혼 =====

    // 죽은 자리에서 영혼으로 활동 시작
    void BecomeSoul(Shell shell)
    {
        isSoul = true;

        // 영혼과 shell이 같은 위치에 겹쳐 있으므로 서로 밀어내지 않게 함
        soulShellCollider = shell.GetComponent<Collider2D>();
        if (col && soulShellCollider)
            Physics2D.IgnoreCollision(col, soulShellCollider, true);

        // 영혼은 독에 영향받지 않음
        if (poisonSwim != null) poisonSwim.enabled = false;

        SetSoulLook(true);

        // death +1, 5초 타이머 시작, OnSoulWorldEntered → WorldStateManager가 세계 전환
        soulWorldController.HandlePlayerDeath();
    }

    // 영혼세계 종료(5초 경과 또는 E키) → 부활
    void HandleReturn()
    {
        if (!isDead) return;
        Revive(GetRespawnPosition());
    }

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        HandleReturn();
    }

    Vector3 GetRespawnPosition()
    {
        return respawnPoint != null ? respawnPoint.transform.position : startPosition;
    }

    // ===== 체크포인트 =====

    // RespawnPoint가 호출. 앞 순서 지점으로는 되돌아가지 않음, 영혼은 갱신 불가
    public bool SetRespawnPoint(RespawnPoint point)
    {
        if (isDead) return false;
        if (point == null || point == respawnPoint) return false;
        if (respawnPoint != null && point.Order < respawnPoint.Order) return false;

        respawnPoint = point;
        return true;
    }

    // ===== 부활 =====

    public void Revive(Vector3 position)
    {
        if (soulShellCollider != null && col != null)
            Physics2D.IgnoreCollision(col, soulShellCollider, false);

        soulShellCollider = null;

        isSoul = false;
        isDead = false;
        inputX = 0f;
        jumpRequested = false;

        transform.position = position;

        if (poisonSwim != null)
        {
            poisonSwim.ResetPoisonState();
            poisonSwim.enabled = true;
        }

        rb.linearVelocity = Vector2.zero;
        rb.simulated = true;

        if (col) col.enabled = true;

        SetSoulLook(false);
        SetBodyVisible(true);
    }

    void SetSoulLook(bool soul)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            renderers[i].color = soul
                ? originalColors[i] * soulTint
                : originalColors[i];
        }
    }

    void SetBodyVisible(bool visible)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;

            renderers[i].enabled =
                visible && originalEnabled[i];
        }
    }
}