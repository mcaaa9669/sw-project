using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, IKillable
{

    [SerializeField] float fallGravityMultiplier = 2f;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float jumpForce = 5f;
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundRadius = 0.1f;
    [SerializeField] LayerMask groundLayer;

    [SerializeField] Animator animator;          // 인스펙터에서 연결 
    [SerializeField] SpriteRenderer spriteRenderer; // 좌우 반전용
    // 파라미터 이름 오타 방지 + 성능
    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int GroundedHash = Animator.StringToHash("IsGrounded");

    [Header("Death")]
    [SerializeField] Shell shellPrefab;                        // Shell.prefab 연결
    [SerializeField] float poisonDelay = 5f;                   // 독 사망까지 걸리는 시간 (명세: 5초)

    [Header("Respawn")]
    [SerializeField] RespawnPoint respawnPoint;                // 현재 부활 지점. 비우면 시작 위치에서 부활
    [SerializeField] float respawnDelay = 1f;                  // 영혼세계 없이 부활할 때 걸리는 시간

    [Header("Soul World")]
    [SerializeField] bool useSoulWorld = true;
    [SerializeField] bool soulWorldOnFall = true;              // 추가: 끄면 낭떠러지 사망은 영혼세계 없이 바로 부활// 끄면 영혼세계 없이 바로 부활
    [SerializeField] SoulWorldController soulWorldController;  // 비우면 씬에서 자동으로 찾음
    [SerializeField] Color soulTint = new Color(0.6f, 0.85f, 1f, 0.5f); // 영혼 외형 (임시)


    [SerializeField, Range(0f, 1f)]
    float swimMoveMultiplier = 0.5f;

    public event System.Action FallRespawned;

    Rigidbody2D rb;
    Collider2D col;           // 추가: 끄면 낭떠러지 사망은 영혼세계 없이 바로 부활
    PoisonSwim poisonSwim;
    SpriteRenderer[] renderers;
    Color[] originalColors;
    bool[] originalEnabled;

    float inputX;
    bool jumpRequested;
    bool wasWallBlocked;
    readonly ContactPoint2D[] groundContacts = new ContactPoint2D[16];
    MovingPlatform groundPlatform;
    readonly RaycastHit2D[] movingGroundHits = new RaycastHit2D[8];
    Vector2 groundVelocity;     // 밟고 있는 이동 발판의 속도
    Vector2 lastGroundVelocity; // 직전 물리 프레임에 맞춰 준 발판 속도 (방향이 꺾일 때 비교용)
    bool onMovingPlatform;      // 이동 발판을 밟고 있는지
    readonly RaycastHit2D[] wallHits = new RaycastHit2D[16];

    [SerializeField] LayerMask wallLayer;
    [SerializeField] float wallCheckDistance = 0.03f;

    [SerializeField] float jumpBufferTime = 0.12f;
    float jumpBufferTimer;
    [Tooltip("발판에서 떨어진 직후에도 점프를 허용하는 시간 (코요테 타임)")]
    [SerializeField] float coyoteTime = 0.1f;
    float coyoteTimer;

    bool isDead;        // 현실의 몸이 죽었는지 (영혼 상태 포함)
    bool isSoul;        // 영혼으로 활동 중인지
    Coroutine poisonRoutine;
    Vector3 startPosition;
    Collider2D soulShellCollider;   // 영혼이 빠져나온 shell (겹쳐 있으므로 충돌 무시)

    public bool IsDead => isDead;
    public bool IsSoul => isSoul;
    bool CanControl => !isDead || isSoul;   // 살아 있거나 영혼일 때만 조작 가능

    [SerializeField, Range(0f, 1f)]
    float surfaceMoveMultiplier = 0.5f;

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

        if (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
        {
            jumpRequested = true;

            Debug.Log(
                $"[점프 입력] 위치={rb.position}, 세로속도={rb.linearVelocity.y:F2}",
                this
            );
        }

    }

    void FixedUpdate()
    {
        if (!CanControl) return;
        // 영혼이 자기 시체와 더 이상 겹치지 않으면 다시 부딪히게 함
        // (처음엔 겹쳐서 생기므로 충돌을 꺼두고, 완전히 빠져나온 뒤 켬)
        if (isSoul && soulShellCollider != null &&
            !col.Distance(soulShellCollider).isOverlapped)
        {
            Physics2D.IgnoreCollision(col, soulShellCollider, false);
            soulShellCollider = null;
        }

        MovingPlatform previousPlatform = groundPlatform;
        float previousPlatformVerticalSpeed = groundVelocity.y;
        bool grounded = IsGrounded(out bool onBelt, out float beltSpeed);
        bool inPoison = poisonSwim != null
        && poisonSwim.isActiveAndEnabled
        && poisonSwim.IsSwimming;

        bool swimActive = poisonSwim != null
    && poisonSwim.isActiveAndEnabled;

        // 물 밖에서 발판에 내려앉으면 수면 점프 상태 해제.
        // 점프해서 올라가는 중의 접촉은 착지로 처리하지 않음.
        if (swimActive && !inPoison &&
            grounded && rb.linearVelocity.y <= 0f)
        {
            poisonSwim.ClearSurfaceJump();
        }

        bool surfaceJumping = swimActive
            && poisonSwim.IsSurfaceJumping;

        float speedMultiplier = inPoison
            ? swimMoveMultiplier
            : (surfaceJumping ? surfaceMoveMultiplier : 1f);

        float targetSpeed = inputX * moveSpeed * speedMultiplier;

        if (onBelt && inputX * beltSpeed < 0f)
        {
            targetSpeed *= 0.6f;
        }
        // 벽을 향한 이동만 차단. 반대쪽 이동은 가능.
        bool wallBlocked =
            inputX != 0f && IsWallAhead(Mathf.Sign(inputX));

        if (wallBlocked)
            targetSpeed = 0f;

        if (wallBlocked != wasWallBlocked)
        {
            Debug.Log(
                $"[이동 판정] 벽 차단={wallBlocked}, " +
                $"방향={inputX}, 바닥={grounded}",
                this
            );
        }

        wasWallBlocked = wallBlocked;

        Vector2 velocity = rb.linearVelocity;
        bool carryByDisplacement = groundPlatform != null && groundPlatform.IsLoop && !inPoison;
        if (!onBelt || inputX != 0f)
            velocity.x = targetSpeed + (carryByDisplacement ? 0f : groundVelocity.x);

        // 이동 발판 위에서는 세로 속도를 발판에 맞춤 (덜그럭거림 방지)
        // 점프해서 발판보다 빠르게 올라가는 중이면 건드리지 않음
        // The previous physics step can still have upward platform speed at the top corner.
        // Match the new vertical speed before it carries the player off the horizontal leg.
        bool keptLoopSupport = carryByDisplacement && groundPlatform == previousPlatform &&
            velocity.y <= Mathf.Max(previousPlatformVerticalSpeed, groundVelocity.y) + 0.1f;
        if (keptLoopSupport || (grounded && groundVelocity.y != 0f &&
            velocity.y <= groundVelocity.y + 0.5f))
        {
            velocity.y = groundVelocity.y;
        }
        lastGroundVelocity = onMovingPlatform ? groundVelocity : Vector2.zero;

        if (jumpRequested)
        {
            jumpBufferTimer = jumpBufferTime;

            Debug.Log(
                $"[점프 접수] 바닥={grounded}, 독 안={inPoison}, " +
                $"대기시간={jumpBufferTimer:F2}",
                this
            );
        }

        // 점프 입력이 없으면 타이머를 줄임
        if (jumpRequested)
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - Time.fixedDeltaTime);

        jumpRequested = false;

        // 바닥에 있으면 타이머를 채우고, 공중이면 줄임
        // 올라가는 중(점프 직후)의 접촉은 바닥으로 치지 않음 → 이단 점프 방지
        if (grounded && (keptLoopSupport || rb.linearVelocity.y <= groundVelocity.y + 0.01f))
            coyoteTimer = coyoteTime;
        else
            coyoteTimer = Mathf.Max(0f, coyoteTimer - Time.fixedDeltaTime);

        bool jumping = jumpBufferTimer > 0f && coyoteTimer > 0f && !inPoison;
        if (jumping)
        {
            velocity.y = jumpForce;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;   // 한 번 쓰면 바로 소모

            Debug.Log($"[점프 실행] 바닥={grounded} (코요테 점프={!grounded})", this);
        }

        if (carryByDisplacement && !jumping)
            CarryOnLoopPlatform(groundVelocity.x * Time.fixedDeltaTime);
        else if (carryByDisplacement && jumping)
            velocity.x += groundVelocity.x;

        rb.linearVelocity = velocity;

        bool swimming = poisonSwim != null
            && poisonSwim.isActiveAndEnabled
            && poisonSwim.IsSwimming;

        // 공중에서 내려갈 때만 추가 중력 적용
        if (!grounded && !swimming && rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Physics2D.gravity
                * rb.gravityScale
                * (Mathf.Max(1f, fallGravityMultiplier) - 1f)
                * Time.fixedDeltaTime;
        }

        animator.SetFloat(SpeedHash, Mathf.Abs(rb.linearVelocity.x - (carryByDisplacement && !jumping ? 0f : groundVelocity.x)));
        animator.SetBool(GroundedHash, grounded);

        if (jumping)
        {
            groundPlatform = null;
            groundVelocity = Vector2.zero;
        }

        if (inputX != 0) spriteRenderer.flipX = inputX < 0;
    }

    bool IsGrounded(out bool onBelt, out float beltSpeed)
    {
        onBelt = false;
        beltSpeed = 0f;
        MovingPlatform previousPlatform = groundPlatform;
        float previousVerticalSpeed = groundVelocity.y;
        groundVelocity = Vector2.zero;
        groundPlatform = null;
        bool grounded = false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayer);
        filter.useTriggers = false;

        int count = col.GetContacts(filter, groundContacts);
        Bounds bounds = col.bounds;
        for (int i = 0; i < count; i++)
        {
            ContactPoint2D contact = groundContacts[i];
            Collider2D floor = contact.collider == col ? contact.otherCollider : contact.collider;
            if (floor == null || contact.normal.y < 0.9f || contact.point.y >= bounds.center.y)
                continue;

            MovingPlatform platform = floor.GetComponentInParent<MovingPlatform>();
            float supportedWidth = bounds.extents.x * (platform != null ? 1f : 0.5f);
            if (Mathf.Abs(contact.point.x - bounds.center.x) > supportedWidth + 0.01f)
                continue;

            grounded = true;
            if (platform != null)
            {
                groundPlatform = platform;
                groundVelocity = platform.Velocity;
            }

            if (floor.usedByEffector &&
                floor.TryGetComponent<SurfaceEffector2D>(out var effector) &&
                effector.isActiveAndEnabled)
            {
                onBelt = true;
                beltSpeed = effector.speed;
                break;
            }
        }

        // Tile seams and direction changes can leave a short gap between physics contacts.
        if (!grounded)
        {
            int hits = col.Cast(Vector2.down, filter, movingGroundHits, 0.06f);
            float closestDistance = float.PositiveInfinity;
            for (int i = 0; i < hits; i++)
            {
                RaycastHit2D hit = movingGroundHits[i];
                if (hit.collider == null || hit.normal.y < 0.9f || hit.point.y >= bounds.center.y ||
                    hit.distance >= closestDistance)
                    continue;

                MovingPlatform platform = hit.collider.GetComponentInParent<MovingPlatform>();
                if (platform == null)
                    continue;

                float supportSpeed = platform.Velocity.y;
                if (platform == previousPlatform)
                    supportSpeed = Mathf.Max(previousVerticalSpeed, supportSpeed);
                if (rb.linearVelocity.y > supportSpeed + 0.1f)
                    continue;

                grounded = true;
                groundPlatform = platform;
                groundVelocity = platform.Velocity;
                closestDistance = hit.distance;
            }
        }

        return grounded;
    }

    void CarryOnLoopPlatform(float offset)
    {
        if (Mathf.Abs(offset) < 0.0001f)
            return;

        float direction = Mathf.Sign(offset);
        float distance = Mathf.Abs(offset);
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(wallLayer | groundLayer);
        filter.useTriggers = false;
        int count = col.Cast(Vector2.right * direction, filter, wallHits, distance);
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = wallHits[i];
            if (hit.rigidbody != null && hit.rigidbody.gameObject == groundPlatform.gameObject)
                continue;

            if (hit.normal.x * direction < -0.1f && hit.normal.y < 0.9f)
                distance = Mathf.Min(distance, hit.distance);
        }

        rb.position += Vector2.right * (distance * direction);
    }

    bool IsWallAhead(float direction)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(wallLayer);
        filter.useTriggers = false;

        Vector2 moveDirection = new Vector2(direction, 0f);

        int count = col.Cast(
            moveDirection,
            filter,
            wallHits,
            Mathf.Max(0.001f, wallCheckDistance)
        );

        for (int i = 0; i < count; i++)
        {
            Vector2 normal = wallHits[i].normal;

            // 진행 방향을 막는 면인지 확인.
            // 평평한 바닥은 제외하고 모서리의 비스듬한 면은 포함.
            bool facesPlayer = normal.x * direction < -0.1f;
            bool isSteep = normal.y < 0.9f;

            if (facesPlayer && isSteep)
            {
                if (!wasWallBlocked)
                {
                    Debug.Log(
                        $"[벽 감지 대상] 이름={wallHits[i].collider.name}, " +
                        $"방향={direction}, 접촉면={normal}, " +
                        $"거리={wallHits[i].distance:F3}",
                        this
                    );
                }

                return true;
            }
        }

        return false;
    }
    // ===== 사망 =====

    // Hazard가 보내는 사망 신호 (IKillable). 영혼 상태에서는 무시
    public void Kill(DeathType deathType)
    {
        Kill(deathType, null);
    }

    public void Kill(DeathType deathType, Transform spikeAnchor)
    {
        if (isSoul && deathType == DeathType.Fall)
        {
            if (soulWorldController != null)
                soulWorldController.RequestReturn();

            if (isSoul)
                HandleReturn();
            return;
        }

        if (isDead) return;

        switch (deathType)
        {
            case DeathType.Spike:
            case DeathType.laser:
            case DeathType.saw:
            case DeathType.Icicle:
            case DeathType.Fall:
                Die(deathType, spikeAnchor);
                break;

            case DeathType.poison:
                if (poisonRoutine == null)
                    poisonRoutine = StartCoroutine(PoisonRoutine());
                break;

            case DeathType.DeadZone:
                Die(deathType, spikeAnchor);
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
    void Die(DeathType deathType, Transform spikeAnchor = null)
    {
        isDead = true;

        if (poisonRoutine != null)
        {
            StopCoroutine(poisonRoutine);
            poisonRoutine = null;
        }

        inputX = 0f;
        jumpRequested = false;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;

        rb.linearVelocity = Vector2.zero;

        // shell을 먼저 만들어야, 영혼세계 진입 시 현실세계와 함께 멈춤
        Shell shell = SpawnShellFor(deathType, spikeAnchor);

        bool canUseSoulWorld = useSoulWorld && soulWorldController != null;

        if (canUseSoulWorld && shell != null)
        {
            BecomeSoul(shell);
        }
        else if (canUseSoulWorld && soulWorldOnFall && deathType == DeathType.Fall)
        {
            // 낭떠러지: shell 없이 리스폰 포인트에서 영혼으로 시작
            transform.position = GetRespawnPosition();
            rb.linearVelocity = Vector2.zero;
            BecomeSoul(null);
        }
        else
        {
            if (deathType == DeathType.Fall)
            {
                SoulWorldController deathCounter = soulWorldController != null
                    ? soulWorldController
                    : FindFirstObjectByType<SoulWorldController>();
                deathCounter?.RegisterDeath();
            }

            // 몸을 숨기고 잠시 뒤 체크포인트에서 부활
            rb.simulated = false;
            if (col) col.enabled = false;
            SetBodyVisible(false);
            StartCoroutine(RespawnAfterDelay(deathType));
        }
    }

    // 사망 종류별 shell 생성 (shell이 없는 사망은 null)
    Shell SpawnShellFor(DeathType deathType, Transform spikeAnchor)
    {
        if (shellPrefab == null) return null;

        switch (deathType)
        {
            case DeathType.Spike:
            case DeathType.saw when spikeAnchor != null:
                {
                    Shell shell = SpawnShell();
                    shell.OnSpikeDeath(spikeAnchor);      // 그 자리에 고정
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
            case DeathType.Icicle:
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


        if (shell != null)
        {
            // 영혼과 shell이 같은 위치에 겹쳐 있으므로 서로 밀어내지 않게 함
            soulShellCollider = shell.GetComponent<Collider2D>();
            if (col && soulShellCollider)
                Physics2D.IgnoreCollision(col, soulShellCollider, true);
        }

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

    IEnumerator RespawnAfterDelay(DeathType deathType)
    {
        yield return new WaitForSeconds(respawnDelay);
        if (!isDead) yield break;

        HandleReturn();
        if (deathType == DeathType.Fall)
            FallRespawned?.Invoke();
    }

    Vector3 GetRespawnPosition()
    {
        return respawnPoint != null ? respawnPoint.transform.position : startPosition;
    }

    // ===== 체크포인트 =====

    public bool IsCurrentRespawnPoint(RespawnPoint point) => point != null && respawnPoint == point;

    // RespawnPoint가 호출. 앞 순서 지점으로는 되돌아가지 않음, 영혼은 갱신 불가
    public bool SetRespawnPoint(RespawnPoint point)
    {
        if (isDead) return false;
        if (point == null || point == respawnPoint) return false;
        if (respawnPoint != null && point.Order < respawnPoint.Order) return false;

        respawnPoint = point;
        point.ActivateMarker();
        return true;
    }

    // 스테이지에 따라 영혼세계 기능을 켜고 끔 (1-3부터 켬)
    public void SetSoulWorldEnabled(bool enabled)
    {
        useSoulWorld = enabled;

        if (enabled && soulWorldController == null)
        {
            soulWorldController = FindFirstObjectByType<SoulWorldController>();
            if (soulWorldController != null)
                soulWorldController.OnReturnRequested += HandleReturn;
        }
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
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;

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