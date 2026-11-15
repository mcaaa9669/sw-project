using UnityEngine;

// 레이저에 붙이는 센서: 시체(Shell)가 레이저를 통과하면 LaserSignal을 뒤집음
// - 레이저 루트의 트리거 콜라이더(BoxCollider2D)로 감지
// - 레이저가 꺼져 있으면(SignalToggle이 콜라이더를 끔) 감지하지 않음
// - 톱니와 함께 도는 시체는 Kinematic이라 정적 트리거와도 OnTriggerEnter2D가 호출됨
[RequireComponent(typeof(Collider2D))]
public class LaserSensor : MonoBehaviour
{
    [SerializeField] LaserSignal signal;

    [Tooltip("같은 통과가 두 번 세지지 않도록 기다리는 시간 (초)")]
    [SerializeField, Min(0f)] float cooldown = 0.15f;

    float lastToggleTime = float.NegativeInfinity;

    void Awake()
    {
        if (signal == null)
            signal = FindFirstObjectByType<LaserSignal>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (signal == null || !signal.IsRunning) return;
        if (other.GetComponentInParent<Shell>() == null) return;
        if (Time.time - lastToggleTime < cooldown) return;

        lastToggleTime = Time.time;
        signal.Toggle();
    }
}
