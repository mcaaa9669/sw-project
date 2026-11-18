using UnityEngine;

// 레이저에 붙이는 센서: 시체(Shell)가 레이저를 통과하면 LaserSignal을 뒤집음
// - 레이저 루트의 트리거 콜라이더(BoxCollider2D)로 감지
// - 레이저가 꺼져 있으면(SignalToggle이 콜라이더를 끔) 감지하지 않음
// - 톱니와 함께 도는 시체는 Kinematic이라 정적 트리거와도 OnTriggerEnter2D가 호출됨
[RequireComponent(typeof(Collider2D))]
public class LaserSensor : MonoBehaviour
{
    [SerializeField] LaserSignal signal;
    [SerializeField] LaserSignal.State laserId = LaserSignal.State.Laser0;   // 1번 레이저는 Laser1로

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

        lastToggleTime = Time.time;
        signal.Select(laserId);
    }
}
