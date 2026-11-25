using System.Collections.Generic;
using UnityEngine;

// 정해진 지점들을 따라 움직이는 이동 발판 (일반 오브젝트·타일맵 모두 사용 가능)
// - 출발점 = 발판의 현재 중심(눈에 보이는 범위의 중심)
// - points = 발판 "중심"이 지나갈 지점들 (타일맵이어도 보이는 곳 기준으로 찍으면 됨)
// - PingPong: 출발점 → A → B → A → 출발점 → ... (왕복)
// - Loop:     출발점 → A → B → C → 출발점 → ... (순환, 사각형 궤도 등)
// - 플레이어는 PlayerController가 Velocity를 읽어서 같이 움직임
// - 떨어진 시체(Shell)는 이 스크립트가 같이 옮김
// - 영혼세계에서는 현실세계와 함께 멈춤
[DefaultExecutionOrder(-10)]    // 플레이어보다 먼저 움직여서 이번 프레임 속도를 넘겨줌
[RequireComponent(typeof(Rigidbody2D))]
public class MovingPlatform : MonoBehaviour, IWorldPausable
{
    public enum PathMode { PingPong, Loop, Once }

    [Header("경로 (출발점 = 발판의 현재 중심)")]
    [SerializeField] PathMode mode = PathMode.PingPong;
    [Tooltip("Optional starting corner. If empty, the platform starts at its visual center.")]
    [SerializeField] Transform startPoint;
    [SerializeField] Transform attachedPathFrame;
    [SerializeField] Transform[] points;                    // 발판 중심이 지나갈 지점 (1개 이상)

    [Header("이동")]
    [SerializeField, Min(0.01f)] float speed = 2f;          // 초당 이동 거리
    [SerializeField, Min(0f)] float waitTime = 0.5f;        // 지점에서 멈추는 시간
    [Tooltip("해제: 왕복은 양 끝, 순환은 출발점에서만 멈춤 / 체크: 모든 지점에서 멈춤")]
    [SerializeField] bool waitAtEveryPoint = false;
    [SerializeField] bool moveOnStart = true;               // 해제하면 레버 등으로 StartMoving() 호출 시 출발

    // 이번 물리 프레임의 이동 속도 (멈춰 있으면 0). PlayerController가 읽음
    [SerializeField] bool stabilizeShellRiders;

    public Vector2 Velocity { get; private set; }
    public Vector2 StartPosition => route != null ? route[0] : (Vector2)transform.position;
    public event System.Action PathChanged;

    Rigidbody2D rb;
    Vector2[] route;            // transform이 갈 위치. [0] = 출발점
    Vector2 centerOffset;       // transform 위치 - 발판 중심
    Matrix4x4 startPointFrame;
    int targetIndex;
    int direction = 1;          // PingPong 전용. 1: 정방향, -1: 역방향
    float waitTimer;
    bool moving;
    bool paused;

    // 발판 위에 올라탄 시체
    readonly HashSet<Rigidbody2D> riders = new();
    readonly Dictionary<Rigidbody2D, float> shellCarrierSpeeds = new();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // 타일맵은 transform 위치와 보이는 위치가 다르므로, 보이는 중심을 기준으로 경로를 만듦
        centerOffset = (Vector2)transform.position - GetVisualCenter();
        Transform pathFrame = attachedPathFrame != null ? attachedPathFrame : transform;
        startPointFrame = pathFrame.localToWorldMatrix;
        if (attachedPathFrame != null)
            centerOffset = (Vector2)(transform.position - attachedPathFrame.position);

        Vector2 startPosition = startPoint != null
            ? GetRoutePoint(startPoint)
            : (Vector2)transform.position;
        var list = new List<Vector2> { startPosition };
        if (points != null)
            foreach (var p in points)
                if (p != null) list.Add(GetRoutePoint(p));
        route = list.ToArray();

        if (route.Length < 2)
        {
            Debug.LogWarning($"[MovingPlatform] {name}: 지점이 1개 이상 필요합니다.");
            enabled = false;
            return;
        }

        if (startPoint != null)
            rb.position = startPosition;

        targetIndex = 1;
        moving = moveOnStart;
    }

    // 보이는 범위의 중심 (렌더러 → 콜라이더 → transform 순)
    Vector2 GetVisualCenter()
    {
        var r = GetComponentInChildren<Renderer>();
        if (r != null) return r.bounds.center;

        var c = GetComponentInChildren<Collider2D>();
        if (c != null) return c.bounds.center;

        return transform.position;
    }

    // 레버 등에서 연결
    public void StartMoving() => moving = true;
    public void StopMoving() => moving = false;

    // 경로를 화면에 그릴 때 사용 (발판 중심 기준 월드 좌표)
    public bool IsLoop => mode == PathMode.Loop;

    public Vector2[] GetPathPoints()
    {
        if (route == null) return System.Array.Empty<Vector2>();
        var result = new Vector2[route.Length];
        for (int i = 0; i < route.Length; i++)
            result[i] = route[i] - centerOffset;
        return result;
    }

    Vector2 GetRoutePoint(Transform point)
    {
        // Attached points move with the block in edit mode, but keep the original
        // world frame when the rigidbody moves during play.
        Transform frame = attachedPathFrame != null ? attachedPathFrame : transform;
        bool usesFrame = point.parent == frame && (attachedPathFrame != null || point == startPoint);
        Vector2 center = usesFrame
            ? (Vector2)startPointFrame.MultiplyPoint3x4(point.localPosition)
            : (Vector2)point.position;
        return center + centerOffset;
    }

    void RefreshExplicitRoute()
    {
        if (startPoint == null || points == null) return;

        Vector2 previousStart = route[0];
        Vector2 newStart = GetRoutePoint(startPoint);
        bool changed = !newStart.Equals(previousStart);
        route[0] = newStart;

        int index = 1;
        foreach (var point in points)
        {
            if (point == null) continue;
            if (index >= route.Length) break;

            Vector2 position = GetRoutePoint(point);
            changed |= !route[index].Equals(position);
            route[index++] = position;
        }

        if (!changed) return;
        if (!moving && rb.position == previousStart)
            rb.position = newStart;

        PathChanged?.Invoke();
    }

    void FixedUpdate()
    {
        Velocity = Vector2.zero;
        RefreshExplicitRoute();

        if (paused) return;
        if (!moving || waitTimer > 0f)
        {
            if (moving)
                waitTimer -= Time.fixedDeltaTime;
            if (stabilizeShellRiders)
                SyncShellRiders(Vector2.zero);
            return;
        }

        Vector2 current = rb.position;
        Vector2 next = Vector2.MoveTowards(current, route[targetIndex], speed * Time.fixedDeltaTime);
        Vector2 delta = next - current;

        Velocity = delta / Time.fixedDeltaTime;
        rb.MovePosition(next);
        if (stabilizeShellRiders)
            SyncShellRiders(Velocity);
        else
            CarryRiders(delta);

        if (next == route[targetIndex])
            ArriveAtTarget();
    }

    // 지점 도착: 다음 목표를 정하고, 필요하면 잠시 대기
    // 지점 도착: 다음 목표를 정하고, 필요하면 잠시 대기
    void ArriveAtTarget()
    {
        int arrived = targetIndex;
        bool isEnd;

        if (mode == PathMode.Once)
        {
            // 마지막 지점에 도착하면 그 자리에 멈춤 (다시 안 돌아감)
            if (arrived == route.Length - 1)
            {
                moving = false;
                return;
            }
            targetIndex = arrived + 1;
            isEnd = false;
        }
        else if (mode == PathMode.Loop)
        {
            targetIndex = (arrived + 1) % route.Length;
            isEnd = arrived == 0;                                   // 한 바퀴 돌아 출발점
        }
        else
        {
            int next = arrived + direction;
            if (next < 0 || next >= route.Length)
            {
                direction = -direction;
                next = arrived + direction;
            }
            targetIndex = next;
            isEnd = arrived == 0 || arrived == route.Length - 1;    // 양 끝
        }

        if (waitAtEveryPoint || isEnd)
            waitTimer = waitTime;
    }

    // 올라탄 시체를 발판이 움직인 만큼 같이 옮김 (플레이어는 PlayerController가 처리)
    void CarryRiders(Vector2 delta)
    {
        if (delta == Vector2.zero) return;

        riders.RemoveWhere(r => r == null || !r.simulated);

        foreach (var rider in riders)
        {
            if (rider.bodyType != RigidbodyType2D.Dynamic) continue;
            rider.position += delta;
        }
    }

    // 위에서 밟고 있는 시체만 탑승으로 인정
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (stabilizeShellRiders)
            OnCollisionStay2D(collision);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        Rigidbody2D other = collision.rigidbody;
        if (other == null || other.GetComponent<Shell>() == null) return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y < -0.5f)
            {
                riders.Add(other);
                return;
            }
        }

        RemoveRider(other);
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.rigidbody != null)
            RemoveRider(collision.rigidbody);
    }

    void RemoveRider(Rigidbody2D rider)
    {
        riders.Remove(rider);
        shellCarrierSpeeds.Remove(rider);
    }

    void SyncShellRiders(Vector2 carrierVelocity)
    {
        riders.RemoveWhere(rider =>
        {
            if (rider != null && rider.simulated) return false;

            shellCarrierSpeeds.Remove(rider);
            return true;
        });

        foreach (var rider in riders)
        {
            if (rider.bodyType != RigidbodyType2D.Dynamic)
            {
                shellCarrierSpeeds.Remove(rider);
                continue;
            }

            Vector2 velocity = rider.linearVelocity;
            shellCarrierSpeeds.TryGetValue(rider, out float previousCarrierSpeed);
            float relativeSpeed = velocity.x - previousCarrierSpeed;

            // Keep deliberate pushes and remove small slip relative to the platform.
            if (Mathf.Abs(relativeSpeed) < 0.25f)
                relativeSpeed = 0f;

            // The contact solver sees the same motion on both bodies, even at a stop.
            rider.linearVelocity = new Vector2(carrierVelocity.x + relativeSpeed, carrierVelocity.y);
            shellCarrierSpeeds[rider] = carrierVelocity.x;
        }
    }

    public void MoveWhileRattling(Vector2 position)
    {
        if (stabilizeShellRiders)
            SyncShellRiders((position - rb.position) / Time.fixedDeltaTime);
        rb.MovePosition(position);
    }

    public void SetWorldPaused(bool value)
    {
        paused = value;
    }

    // 씬 뷰에서 경로 표시 (발판 중심 기준)
    void OnDrawGizmos()
    {
        if (TryGetComponent(out PlatformPathLine trail) && trail.HasVisualPath)
            return;

        var path = new List<Vector2>();

        if (Application.isPlaying && route != null)
        {
            foreach (var p in route) path.Add(p - centerOffset);
        }
        else
        {
            path.Add(startPoint != null ? (Vector2)startPoint.position : GetVisualCenter());
            if (points != null)
                foreach (var p in points)
                    if (p != null) path.Add(p.position);
        }

        if (path.Count < 2) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < path.Count - 1; i++)
            Gizmos.DrawLine(path[i], path[i + 1]);

        if (mode == PathMode.Loop)
            Gizmos.DrawLine(path[^1], path[0]);

        foreach (var p in path)
            Gizmos.DrawWireSphere(p, 0.15f);
    }
}
