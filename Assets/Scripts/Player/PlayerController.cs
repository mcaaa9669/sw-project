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
    // 톱니에 붙은 시체는 기울어져 있으므로 이 정도 경사까지 바닥으로 인정 (0.5 ≈ 60도)
    [SerializeField, Range(0f, 1f)] float sawShellMinNormalY = 0.5f;

    [SerializeField] Animator animator;          // 인스펙터에서 연결
    [SerializeField] SpriteRenderer spriteRenderer; // 좌우 반전용
    // 파라미터 이름 오타 방지 + 성능
    static readonly int InteractStateHash = Animator.StringToHash("Base Layer.PL_Interact");
    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int SwimCycleHash = Animator.StringToHash("SwimCycle");
    static readonly int SwimPoseHash = Animator.StringToHash("SwimPose");
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
    float swimCycle;
    bool jumpRequested;
    bool wasWallBlocked;
    Shell previousGroundedShell;
    bool wasFalling;
    int terrainLayer;

    [SerializeField] float animGroundGrace = 0.08f;   // 바닥 판정이 잠깐 끊겨도 점프 애니메이션으로 안 넘어가는 시간
    float airTime;
    readonly ContactPoint2D[] groundContacts = new ContactPoint2D[16];
    MovingPlatform groundPlatform;
    readonly RaycastHit2D[] movingGroundHits = new RaycastHit2D[8];
    Vector2 groundVelocity;     // 밟고 있는 이동 발판의 속도
    Vector2 lastGroundVelocity; // 직전 물리 프레임에 맞춰 준 발판 속도 (방향이 꺾일 때 비교용)
    bool onMovingPlatform;      // 이동 발판을 밟고 있는지

    // ===== 톱니에 붙은 시체 위에 고정 =====
    // 조작이 없으면 밟고 있는 시체의 "그 자리"에 발이 붙은 것처럼 같이 움직임
    [SerializeField, Min(0f)] float sawShellRideGrace = 0.08f; // 접촉이 잠깐 끊겨도 고정을 유지하는 시간
    Transform groundSawShell;   // 이번 물리 프레임에 밟은 톱니 시체
    Transform rideShell;        // 지금 고정되어 있는 시체
    Vector2 rideLocalFoot;      // 그 시체 기준 발 위치 (시체의 로컬 좌표)
    float rideGraceTimer;
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

    // ===== 중력 방향 =====
    // +1: 평소(아래로 떨어짐), -1: 반전(천장 쪽으로 떨어짐)
    // 바닥 판정·점프·낙하 가속은 전부 이 값을 곱한 "플레이어 기준 위쪽"으로 계산함
    [Header("Gravity Flip")]
    [Tooltip("죽고 부활하면 중력을 원래대로 되돌림")]
    [SerializeField] bool resetGravityOnRevive = true;
    float gravitySign = 1f;

    public bool IsGravityFlipped => gravitySign < 0f;
    public event System.Action<bool> GravityFlipped;   // true = 반전됨

    public bool IsDead => isDead;
    public bool IsStandingOnShell => previousGroundedShell != null;
    public bool IsSoul => isSoul;
    bool CanControl => !isDead || isSoul;   // 살아 있거나 영혼일 때만 조작 가능

    [SerializeField, Range(0f, 1f)]
    float surfaceMoveMultiplier = 0.5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        poisonSwim = GetComponent<PoisonSwim>();
        terrainLayer = LayerMask.NameToLayer("Ground");
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
        // PoisonSwim 등이 gravityScale을 양수로 덮어써도 현재 중력 방향을 유지
        ApplyGravityScaleSign();
        float g = gravitySign;
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
        bool grounded = IsGrounded(out bool onBelt, out float beltSpeed, out Shell groundedShell, out bool onTerrain);
        if (!grounded && rb.linearVelocity.y * g < -0.1f)
            wasFalling = true;

        if (grounded)
        {
            if (groundedShell != null && groundedShell != previousGroundedShell && !isDead && !isSoul &&
                (wasFalling || Mathf.Abs(rb.linearVelocity.x) > 0.1f))
                SHELLSoundAuto.PlayCorpseStep();

            if (wasFalling && onTerrain && groundedShell == null && !onBelt && !isDead &&
                (poisonSwim == null || !poisonSwim.IsSwimming))
            {
                Vector2 feet = FootPoint();
                SHELLJungyoonVFXRuntime.PlayLanding(new Vector3(feet.x, feet.y, transform.position.z));
            }

            wasFalling = false;
        }
        previousGroundedShell = groundedShell;

        bool inPoison = poisonSwim != null
        && poisonSwim.isActiveAndEnabled
        && poisonSwim.IsSwimming;

        bool swimActive = poisonSwim != null
    && poisonSwim.isActiveAndEnabled;

        // 물 밖에서 발판에 내려앉으면 수면 점프 상태 해제.
        // 점프해서 올라가는 중의 접촉은 착지로 처리하지 않음.
        if (swimActive && !inPoison &&
            grounded && rb.linearVelocity.y * g <= 0f)
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
        // (세로 비교는 전부 g를 곱해 "플레이어 기준 위쪽"으로 계산)
        bool keptLoopSupport = carryByDisplacement && groundPlatform == previousPlatform &&
            velocity.y * g <= Mathf.Max(previousPlatformVerticalSpeed * g, groundVelocity.y * g) + 0.1f;
        if (keptLoopSupport || (grounded && groundVelocity.y != 0f &&
            velocity.y * g <= groundVelocity.y * g + 0.5f))
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
        if (grounded && (keptLoopSupport || rb.linearVelocity.y * g <= groundVelocity.y * g + 0.01f))
            coyoteTimer = coyoteTime;
        else
            coyoteTimer = Mathf.Max(0f, coyoteTimer - Time.fixedDeltaTime);

        // 톱니에 붙은 시체 위: 조작이 없으면 시체에 고정된 것처럼 따라감
        velocity = RideSawShell(velocity, grounded);

        bool jumping = jumpBufferTimer > 0f && coyoteTimer > 0f && !inPoison;
        if (jumping)
        {
            // 점프하면 시체 고정 해제
            rideShell = null;
            rideGraceTimer = 0f;

            velocity.y = jumpForce * g;
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
        if (!grounded && !swimming && rideShell == null && rb.linearVelocity.y * g < 0f)
        {
            // gravityScale에 부호가 들어 있으므로 반전 상태면 자동으로 위쪽 가속
            rb.linearVelocity += Physics2D.gravity
                * rb.gravityScale
                * (Mathf.Max(1f, fallGravityMultiplier) - 1f)
                * Time.fixedDeltaTime;
        }

        animator.SetFloat(SpeedHash, Mathf.Abs(rb.linearVelocity.x));
        // 애니메이션 전용: 짧은 접촉 끊김은 무시 (물리 판정은 grounded 그대로 사용)
        // 위로 올라가는 중(점프 직후)이면 여유 시간 없이 바로 공중 처리
        bool rising = (rb.linearVelocity.y - groundVelocity.y) * g > 1f;
        airTime = (grounded && !rising) ? 0f : airTime + Time.fixedDeltaTime;
        animator.SetBool(GroundedHash, !rising && airTime < animGroundGrace);
        animator.SetFloat(SpeedHash, Mathf.Abs(rb.linearVelocity.x - (carryByDisplacement && !jumping ? 0f : groundVelocity.x)));
        animator.SetBool(GroundedHash, grounded);

        if (jumping)
        {
            groundPlatform = null;
            groundVelocity = Vector2.zero;
        }

        bool playingInteraction = animator.GetCurrentAnimatorStateInfo(0).fullPathHash == InteractStateHash;
        int swimPose = 0;
        if (swimming && !grounded && !playingInteraction)
        {
            Vector2 swimVelocity = rb.linearVelocity;
            swimVelocity.y *= g;   // 반전 상태면 몸 기준 위쪽으로 판정 (몸이 뒤집혀 있으므로)
            float verticalThreshold = Mathf.Max(0.25f, Mathf.Abs(swimVelocity.x) * 0.5f);
            swimPose = swimVelocity.y > verticalThreshold ? 2
                : swimVelocity.y < -verticalThreshold ? 3
                : 1;
        }
        // Keep the stroke phase when switching between ascent, descent and horizontal swimming.
        swimCycle = swimPose == 0 ? 0f : Mathf.Repeat(swimCycle + Time.fixedDeltaTime * 1.5f, 1f);
        animator.SetFloat(SwimCycleHash, swimCycle);
        animator.SetInteger(SwimPoseHash, swimPose);

        if (inputX != 0 && !playingInteraction)
            spriteRenderer.flipX = inputX < 0;
    }

    public void PlayInteraction(Vector3 targetPosition)
    {
        if (!animator || !spriteRenderer || !animator.HasState(0, InteractStateHash))
            return;

        float targetDeltaX = targetPosition.x - transform.position.x;
        if (!Mathf.Approximately(targetDeltaX, 0f))
            spriteRenderer.flipX = targetDeltaX < 0f;

        animator.SetInteger(SwimPoseHash, 0);
        animator.Play(InteractStateHash, 0, 0f);
    }

    bool IsGrounded(out bool onBelt, out float beltSpeed, out Shell groundedShell, out bool onTerrain)
    {
        onBelt = false;
        beltSpeed = 0f;
        groundedShell = null;
        onTerrain = false;
        MovingPlatform previousPlatform = groundPlatform;
        float previousVerticalSpeed = groundVelocity.y;
        groundVelocity = Vector2.zero;
        groundPlatform = null;
        groundSawShell = null;
        bool grounded = false;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(groundLayer);
        filter.useTriggers = false;

        float g = gravitySign;
        int count = col.GetContacts(filter, groundContacts);
        Bounds bounds = col.bounds;
        for (int i = 0; i < count; i++)
        {
            ContactPoint2D contact = groundContacts[i];
            Collider2D floor = contact.collider == col ? contact.otherCollider : contact.collider;
            if (floor == null)
                continue;

            // 톱니에 붙은 시체인지 (시체는 톱니의 자식이 됨 → Shell.OnSpikeDeath)
            // 톱니 시체는 돌면서 기울어지므로 경사 허용 범위를 넓힘
            SawRotate saw = floor.GetComponentInParent<SawRotate>();
            float minNormalY = saw != null ? sawShellMinNormalY : 0.9f;

            // 반전 상태면 천장 면(법선이 아래)을 바닥으로 인정
            if (contact.normal.y * g < minNormalY || (contact.point.y - bounds.center.y) * g >= 0f)
                continue;

            MovingPlatform platform = floor.GetComponentInParent<MovingPlatform>();
            float supportedWidth = bounds.extents.x * (platform != null ? 1f : 0.5f);
            if (Mathf.Abs(contact.point.x - bounds.center.x) > supportedWidth + 0.01f)
                continue;

            grounded = true;

            onTerrain |= floor.gameObject.layer == terrainLayer;
            Shell shell = floor.GetComponentInParent<Shell>();
            if (shell != null)
                groundedShell = shell;

            // 톱니에 붙은 Shell을 밟으면 발밑 지점의 회전 속도를 적용
            if (saw != null)
            {
                Vector2 foot = FootPoint();
                groundVelocity = saw.GetPointVelocity(foot);

                groundSawShell = floor.attachedRigidbody != null
                    ? floor.attachedRigidbody.transform
                    : floor.transform;
            }
            else if (platform != null)
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
            int hits = col.Cast(Vector2.down * g, filter, movingGroundHits, 0.06f);
            float closestDistance = float.PositiveInfinity;
            for (int i = 0; i < hits; i++)
            {
                RaycastHit2D hit = movingGroundHits[i];
                if (hit.collider == null || hit.normal.y * g < 0.9f || (hit.point.y - bounds.center.y) * g >= 0f ||
                    hit.distance >= closestDistance)
                    continue;

                MovingPlatform platform = hit.collider.GetComponentInParent<MovingPlatform>();
                if (platform == null)
                    continue;

                float supportSpeed = platform.Velocity.y;
                if (platform == previousPlatform)
                    supportSpeed = g > 0f
                        ? Mathf.Max(previousVerticalSpeed, supportSpeed)
                        : Mathf.Min(previousVerticalSpeed, supportSpeed);
                if ((rb.linearVelocity.y - supportSpeed) * g > 0.1f)
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

            if (hit.normal.x * direction < -0.1f && hit.normal.y * gravitySign < 0.9f)
                distance = Mathf.Min(distance, hit.distance);
        }

        rb.position += Vector2.right * (distance * direction);
    }

    // 톱니에 붙은 시체 위에 고정
    // - 처음 올라선 순간, 시체 기준으로 발 위치를 기억해 둠
    // - 조작이 없는 동안은 매 물리 프레임마다 "시체가 움직인 뒤의 그 자리"로 정확히 옮겨 줌
    //   → 시체가 돌면서 오르내리고 기울어도 플레이어는 그 위에 붙어서 같이 감 (에스컬레이터)
    // - 좌우 이동하면 고정을 풀고(톱니 속도는 groundVelocity로 계속 더해짐), 멈추면 새 자리에 다시 고정
    Vector2 RideSawShell(Vector2 velocity, bool grounded)
    {
        float dt = Time.fixedDeltaTime;

        if (grounded && groundSawShell != null)
            rideGraceTimer = sawShellRideGrace;
        else
            rideGraceTimer -= dt;

        Transform shell = groundSawShell != null ? groundSawShell : rideShell;

        if (inputX != 0f || shell == null || rideGraceTimer <= 0f)
        {
            rideShell = null;
            return velocity;
        }

        Vector2 foot = FootPoint();

        // 새로 올라탄 시체면 지금 발 위치를 그 시체 기준으로 기억
        if (rideShell != shell)
        {
            rideShell = shell;
            rideLocalFoot = shell.InverseTransformPoint(foot);
        }

        // 시체가 이번 프레임에 이동한 뒤의 "그 자리"
        Vector2 target = rideShell.TransformPoint(rideLocalFoot);

        // 너무 멀어졌으면(리스폰·순간이동 등) 고정 해제
        if ((target - foot).sqrMagnitude > 0.25f)
        {
            rideShell = null;
            return velocity;
        }

        Vector2 follow = (target - foot) / dt;

        // 이번 물리 단계에서 더해질 중력을 미리 빼서 아래로 처지지 않게 함
        follow -= Physics2D.gravity * rb.gravityScale * dt;

        return follow;
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
            bool isSteep = normal.y * gravitySign < 0.9f;

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

    // ===== 중력 반전 =====

    // 발이 닿는 쪽 끝점 (평소엔 콜라이더 아래, 반전 시 위)
    Vector2 FootPoint()
    {
        Bounds b = col.bounds;
        return new Vector2(b.center.x, gravitySign > 0f ? b.min.y : b.max.y);
    }

    void ApplyGravityScaleSign()
    {
        float scale = Mathf.Abs(rb.gravityScale) * gravitySign;
        if (!Mathf.Approximately(rb.gravityScale, scale))
            rb.gravityScale = scale;
    }

    public void ToggleGravity() => SetGravityFlipped(!IsGravityFlipped);

    // GravityFlipper 등 기믹이 호출. flipped = true면 천장 쪽으로 떨어짐
    public void SetGravityFlipped(bool flipped)
    {
        float sign = flipped ? -1f : 1f;
        if (Mathf.Approximately(sign, gravitySign)) return;

        gravitySign = sign;
        ApplyGravityScaleSign();

        // 몸을 위아래로 뒤집음 (스프라이트 + 콜라이더 함께)
        Vector3 scale = transform.localScale;
        scale.y = Mathf.Abs(scale.y) * sign;
        transform.localScale = scale;

        // 바닥/발판 관련 상태 초기화 (반전 직후 이전 바닥을 기준으로 점프하지 않게)
        coyoteTimer = 0f;
        rideShell = null;
        rideGraceTimer = 0f;
        groundPlatform = null;
        groundVelocity = Vector2.zero;
        wasFalling = true;

        GravityFlipped?.Invoke(flipped);
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
        if (shell == null)
            SHELLSoundAuto.PlayDeath();

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
        Shell shell = Instantiate(shellPrefab, transform.position, Quaternion.identity);
        if (shell != null)
            SHELLJungyoonVFXRuntime.PlayCorpseSpawn(shell.transform.position);
        return shell;
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
        return respawnPoint != null ? respawnPoint.RespawnPosition : startPosition;
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
        animator.SetInteger(SwimPoseHash, 0);
        previousGroundedShell = null;
        wasFalling = false;
        inputX = 0f;
        jumpRequested = false;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;

        if (resetGravityOnRevive)
            SetGravityFlipped(false);
        wasFalling = false;

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
