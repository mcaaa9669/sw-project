using UnityEngine;

// 주기적으로 떨어지는 종유석
// 구조: 루트(Rigidbody2D, Collider2D 트리거, Hazard, FallingSpike) + 자식 Visual(SpriteRenderer)
// 흐름: Hanging(매달림) → Warning(흔들림) → Falling(낙하) → Hidden(사라짐) → Hanging
// - 박자는 자체 시계(clock)로 셈: 영혼세계에서 멈춰도 종유석끼리 패턴이 어긋나지 않음
// - 사망 처리는 같은 오브젝트의 Hazard가 담당 (Death Type = Icicle)
[RequireComponent(typeof(Rigidbody2D))]
public class FallingSpike : MonoBehaviour, IWorldPausable
{
    enum Phase { Hanging, Warning, Falling, Hidden }

    [Header("박자")]
    [SerializeField, Min(0.1f)] float interval = 3f;        // 떨어지는 주기 (초)
    [SerializeField, Min(0f)] float offset = 0f;            // 첫 예고까지 지연 (패턴용)

    [Header("낙하")]
    [SerializeField, Min(0f)] float warnTime = 0.5f;        // 흔들리는 시간
    [SerializeField, Min(0.1f)] float fallSpeed = 14f;      // 초당 낙하 거리 (일정 속도)
    [SerializeField, Min(0.1f)] float fallDistance = 8f;    // 이만큼 떨어지면 사라짐
    [SerializeField, Min(0f)] float respawnTime = 0.8f;     // 사라진 뒤 다시 매달리기까지

    [Header("흔들림 연출")]
    [SerializeField] Transform visual;                      // 비우면 첫 렌더러의 Transform
    [SerializeField, Min(0f)] float shakeAmount = 0.06f;
    [SerializeField, Min(0f)] float shakeSpeed = 60f;

    [Header("장치 연동 (비우면 항상 작동)")]
    [Tooltip("연결하면 LaserSignal이 Stopped일 때 박자가 멈춤 (레버 ⑤로 같이 꺼짐)")]
    [SerializeField] LaserSignal signal;

    Rigidbody2D rb;
    Renderer[] renderers;
    Collider2D[] colliders;
    Vector2 startPos;
    Vector3 visualStartLocal;

    Phase phase = Phase.Hanging;
    float clock;            // 자체 시계 (영혼세계·장치 정지 중에는 안 흐름)
    float nextWarnTime;     // 다음 예고 시작 시각 (clock 기준)
    float phaseTimer;
    bool paused;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider2D>(true);

        if (visual == null && renderers.Length > 0)
            visual = renderers[0].transform;
        if (visual != null)
            visualStartLocal = visual.localPosition;

        startPos = rb.position;
        nextWarnTime = offset;
    }

    void FixedUpdate()
    {
        if (paused) return;

        float dt = Time.fixedDeltaTime;
        bool running = signal == null || signal.IsRunning;

        // 박자: 시간이 되면 다음 박자를 예약하고, 매달려 있을 때만 예고 시작
        // (떨어지는 중이라 놓친 박자는 건너뜀 → 리듬이 밀리지 않음)
        if (running)
        {
            clock += dt;
            if (clock >= nextWarnTime)
            {
                nextWarnTime += interval;
                if (phase == Phase.Hanging)
                {
                    phase = Phase.Warning;
                    phaseTimer = warnTime;
                }
            }
        }

        switch (phase)
        {
            case Phase.Warning:
                phaseTimer -= dt;
                Shake();
                if (phaseTimer <= 0f)
                {
                    ResetShake();
                    phase = Phase.Falling;
                }
                break;

            case Phase.Falling:
                Vector2 next = rb.position + Vector2.down * (fallSpeed * dt);
                rb.MovePosition(next);
                if (startPos.y - next.y >= fallDistance)
                    Hide();
                break;

            case Phase.Hidden:
                phaseTimer -= dt;
                if (phaseTimer <= 0f)
                    Reappear();
                break;
        }
    }

    void Shake()
    {
        if (visual == null) return;
        float x = Mathf.Sin(phaseTimer * shakeSpeed) * shakeAmount;
        visual.localPosition = visualStartLocal + new Vector3(x, 0f, 0f);
    }

    void ResetShake()
    {
        if (visual != null)
            visual.localPosition = visualStartLocal;
    }

    void Hide()
    {
        SetVisible(false);
        phase = Phase.Hidden;
        phaseTimer = respawnTime;
    }

    void Reappear()
    {
        // 숨겨진 상태에서 원위치로 순간이동 (보간 잔상 방지로 transform도 같이)
        rb.position = startPos;
        transform.position = startPos;
        SetVisible(true);
        phase = Phase.Hanging;
    }

    // 숨겨진 동안에는 판정도 꺼서 Hazard가 죽이지 않게 함
    void SetVisible(bool value)
    {
        foreach (var r in renderers) if (r != null) r.enabled = value;
        foreach (var c in colliders) if (c != null) c.enabled = value;
    }

    public void SetWorldPaused(bool value)
    {
        paused = value;
    }

    // 한 박자 안에 예고+낙하+재등장이 다 끝나야 리듬이 안 밀림
    void OnValidate()
    {
        float cycle = warnTime + fallDistance / fallSpeed + respawnTime;
        if (cycle > interval)
            Debug.LogWarning($"[FallingSpike] {name}: 한 번 떨어지는 데 {cycle:0.00}초 > 주기 {interval}초. " +
                             "박자가 건너뛰어집니다.", this);
    }

    // 씬 뷰에 낙하 범위 표시
    void OnDrawGizmosSelected()
    {
        Vector3 from = Application.isPlaying ? (Vector3)startPos : transform.position;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(from, from + Vector3.down * fallDistance);
        Gizmos.DrawWireSphere(from + Vector3.down * fallDistance, 0.2f);
    }
}