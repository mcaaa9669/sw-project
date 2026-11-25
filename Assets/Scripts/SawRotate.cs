using UnityEngine;

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

    void Update()
    {
        if (paused) return;

        // 기존 방식: 계속 회전
        if (!useAngleLimit)
        {
            float direction = clockwise ? -1f : 1f;

            transform.Rotate(
                0f,
                0f,
                rotationSpeed * direction * Time.deltaTime
            );
            return;
        }

        // 각도 제한 방식: min ~ max 왕복
        currentAngle += rotationSpeed * swingDir * Time.deltaTime;

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

        transform.localRotation = Quaternion.Euler(0f, 0f, startAngle + currentAngle);
    }

    void OnValidate()
    {
        // min이 max보다 커지지 않게
        if (minAngle > maxAngle) minAngle = maxAngle;
    }

    public void SetWorldPaused(bool value)
    {
        paused = value;
    }
}