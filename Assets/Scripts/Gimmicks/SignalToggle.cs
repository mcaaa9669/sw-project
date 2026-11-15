using UnityEngine;

// LaserSignal 상태에 맞춰 보이기·충돌을 켜고 끄는 오브젝트 (레이저, 교차 발판)
// - 레이저:      On ✔  Off ✔  Stopped ✘   (레버가 켜진 뒤 계속 켜져 있음)
// - 파란 발판:   On ✔  Off ✘  Stopped (원하는 대로)
// - 노란 발판:   On ✘  Off ✔  Stopped (원하는 대로)
// 주의: WorldObject와 같은 방식(렌더러·콜라이더 on/off)이라 한 오브젝트에 같이 붙이지 말 것
public class SignalToggle : MonoBehaviour
{
    [SerializeField] LaserSignal signal;

    [Header("이 상태일 때 켜짐")]
    [SerializeField] bool activeWhenOn = true;
    [SerializeField] bool activeWhenOff = false;
    [SerializeField] bool activeWhenStopped = false;

    Renderer[] renderers;
    Collider2D[] colliders;

    void Awake()
    {
        if (signal == null)
            signal = FindFirstObjectByType<LaserSignal>();

        // 자식까지 포함 (레이저의 LineRenderer, 트리거 콜라이더 포함)
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider2D>(true);
    }

    void OnEnable()
    {
        if (signal == null) return;

        signal.StateChanged += Apply;
        Apply(signal.CurrentState);
    }

    void OnDisable()
    {
        if (signal != null)
            signal.StateChanged -= Apply;
    }

    void Apply(LaserSignal.State state)
    {
        bool active = state switch
        {
            LaserSignal.State.On => activeWhenOn,
            LaserSignal.State.Off => activeWhenOff,
            _ => activeWhenStopped,
        };

        foreach (var r in renderers) if (r != null) r.enabled = active;
        foreach (var c in colliders) if (c != null) c.enabled = active;
    }
}
