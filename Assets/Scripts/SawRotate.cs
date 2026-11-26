using UnityEngine;

// 톱니 회전
// - 톱니에 붙은 시체(Shell)는 톱니의 자식이라 같이 돌아감
// - 플레이어는 GetPointVelocity()로 발밑 지점의 속도를 받아서 같이 움직임 (MovingPlatform.Velocity와 같은 방식)
// 플레이어보다 먼저 돌아야, 플레이어가 "이번 프레임에 돈 뒤의 시체 위치"를 기준으로 따라갈 수 있음
[DefaultExecutionOrder(-100)]
public class SawRotate : MonoBehaviour, IWorldPausable
{
    // 회전 속도 (초당 각도)
    public float rotationSpeed = 120f;

    // 체크하면 시계방향, 해제하면 반시계방향 (각도 제한 사용 시: 처음 출발 방향)
    public bool clockwise = false;

    [Header("각도 제한 (왕복 회전)")]
    // 체크하면 minAngle ~ maxAngle 사이를 왕복, 해제하면 기존처럼 계속 회전
    public bool useAngleLimit = false;

    // 배치된 처음 회전값 기준 각도 (+ = 반시계, - = 시계)
    public float minAngle = -90f;
    public float maxAngle = 90f;

    // 현재 회전 속도 (초당 각도, + = 반시계, - = 시계). 멈춰 있으면 0
    public float AngularVelocity { get; private set; }

    bool paused;    // 영혼세계에서 현실세계와 함께 멈춤

    float startAngle;    // 씬에 배치된 원래 Z 회전
    float currentAngle;  // startAngle 기준 현재 각도
    float swingDir;      // 왕복 중 현재 방향 (+1 / -1)

    void Start()
    {
        startAngle = transform.localEulerAngles.z;
        currentAngle = Mathf.Clamp(0f, minAngle, maxAngle);
        swingDir = clockwise ? -1f : 1f;
    }

    // 플레이어 물리(FixedUpdate)와 같은 타이밍에 돌려야
    // 시체 위 플레이어가 받는 속도와 실제 회전이 어긋나지 않음
    void FixedUpdate()
    {
        AngularVelocity = 0f;
        if (paused) return;

        float dt = Time.fixedDeltaTime;

        // 기존 방식: 계속 회전
        if (!useAngleLimit)
        {
            float direction = clockwise ? -1f : 1f;
            AngularVelocity = rotationSpeed * direction;
            transform.Rotate(0f, 0f, AngularVelocity * dt);
            return;
        }

        // 각도 제한 방식: min ~ max 왕복
        float before = currentAngle;
        currentAngle += rotationSpeed * swingDir * dt;

        if (currentAngle >= maxAngle)
        {
            currentAngle = maxAngle;
            swingDir = -1f;
        }
        else if (currentAngle <= minAngle)
        {
            currentAngle = minAngle;
            swingDir = 1f;
        }

        AngularVelocity = (currentAngle - before) / dt;
        transform.localRotation = Quaternion.Euler(0f, 0f, startAngle + currentAngle);
    }

    // 월드 좌표의 한 점이 톱니와 같이 돌 때의 속도 (에스컬레이터처럼 따라가기용)
    // v = ω × r  →  (-ω·r.y, ω·r.x)
    public Vector2 GetPointVelocity(Vector2 worldPoint)
    {
        if (paused) return Vector2.zero;

        Vector2 r = worldPoint - (Vector2)transform.position;
        float w = AngularVelocity * Mathf.Deg2Rad;
        return new Vector2(-w * r.y, w * r.x);
    }

    void OnValidate()
    {
        // min이 max보다 커지지 않게
        if (minAngle > maxAngle) minAngle = maxAngle;
    }

    public void SetWorldPaused(bool value)
    {
        paused = value;
        if (value) AngularVelocity = 0f;
    }
}