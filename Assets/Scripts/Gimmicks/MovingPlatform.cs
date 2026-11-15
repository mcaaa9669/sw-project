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
    public enum PathMode { PingPong, Loop }

    [Header("경로 (출발점 = 발판의 현재 중심)")]
    [SerializeField] PathMode mode = PathMode.PingPong;
    [SerializeField] Transform[] points;                    // 발판 중심이 지나갈 지점 (1개 이상)

    [Header("이동")]
    [SerializeField, Min(0.01f)] float speed = 2f;          // 초당 이동 거리
    [SerializeField, Min(0f)] float waitTime = 0.5f;        // 지점에서 멈추는 시간
    [Tooltip("해제: 왕복은 양 끝, 순환은 출발점에서만 멈춤 / 체크: 모든 지점에서 멈춤")]
    [SerializeField] bool waitAtEveryPoint = false;
    [SerializeField] bool moveOnStart = true;               // 해제하면 레버 등으로 StartMoving() 호출 시 출발

    // 이번 물리 프레임의 이동 속도 (멈춰 있으면 0). PlayerController가 읽음
    public Vector2 Velocity { get; private set; }

    Rigidbody2D rb;
    Vector2[] route;            // transform이 갈 위치. [0] = 출발점
    Vector2 centerOffset;       // transform 위치 - 발판 중심
    int targetIndex;
    int direction = 1;          // PingPong 전용. 1: 정방향, -1: 역방향
    float waitTimer;
    bool moving;
    bool paused;

    // 발판 위에 올라탄 시체
    readonly HashSet<Rigidbody2D> riders = new();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // 타일맵은 transform 위치와 보이는 위치가 다르므로, 보이는 중심을 기준으로 경로를 만듦
        centerOffset = (Vector2)transform.position - GetVisualCenter();

        var list = new List<Vector2> { transform.position };
        if (points != null)
            foreach (var p in points)
                if (p != null) list.Add((Vector2)p.position + centerOffset);
        route = list.ToArray();

        if (route.Length < 2)
        {
            Debug.LogWarning($"[MovingPlatform] {name}: 지점이 1개 이상 필요합니다.");
            enabled = false;
            return;
        }

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

    void FixedUpdate()
    {
        Velocity = Vector2.zero;

        if (!moving || paused) return;

        if (waitTimer > 0f)
        {
            waitTimer -= Time.fixedDeltaTime;
            return;
        }

        Vector2 current = rb.position;
        Vector2 next = Vector2.MoveTowards(current, route[targetIndex], speed * Time.fixedDeltaTime);
        Vector2 delta = next - current;

        Velocity = delta / Time.fixedDeltaTime;
        rb.MovePosition(next);
        CarryRiders(delta);

        if (next == route[targetIndex])
            ArriveAtTarget();
    }

    // 지점 도착: 다음 목표를 정하고, 필요하면 잠시 대기
    void ArriveAtTarget()
    {
        int arrived = targetIndex;
        bool isEnd;

        if (mode == PathMode.Loop)
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

        riders.Remove(other);
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.rigidbody != null)
            riders.Remove(collision.rigidbody);
    }

    public void SetWorldPaused(bool value)
    {
        paused = value;
    }

    // 씬 뷰에서 경로 표시 (발판 중심 기준)
    void OnDrawGizmos()
    {
        var path = new List<Vector2>();

        if (Application.isPlaying && route != null)
        {
            foreach (var p in route) path.Add(p - centerOffset);
        }
        else
        {
            path.Add(GetVisualCenter());
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
