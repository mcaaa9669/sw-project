using System;
using UnityEngine;

// 2-3 장치의 bool 신호
// - 정지: 레버를 당기기 전 / 반대편 레버로 끈 뒤 (레이저 꺼짐)
// - 작동: 레이저가 계속 켜져 있고, 시체가 레이저를 통과할 때마다 신호 토글됨
// - SignalToggle들이 이 신호를 따라 발판 두 세트를 교차로 보여줌
public class LaserSignal : MonoBehaviour
{
    public enum State { Stopped, Waiting, Laser0, Laser1 }

    [SerializeField] bool runOnStart = false;           // 체크하면 레버 없이 바로 작동

    public event Action<State> StateChanged;

    public State CurrentState { get; private set; } = State.Stopped;
    public bool IsRunning => CurrentState != State.Stopped;
    internal bool HasInitialized { get; private set; }

    void Start()
    {
        SetState(runOnStart ? State.Waiting : State.Stopped);
        HasInitialized = true;
    }

    // 레버 ②: 장치 전부 작동 (On 쪽 발판부터 보임)
    public void StartDevices()
    {
        if (!IsRunning)
            SetState(State.Waiting);
    }

    // 레버 ⑤: 장치 전부 꺼짐
    public void StopDevices()
    {
        SetState(State.Stopped);
    }

    // LaserSensor: "나는 몇 번 레이저다"를 그대로 전달
    public void Select(State laser)
    {
        if (!IsRunning || CurrentState == laser) return;   // 같은 레이저에 또 걸려도 무시
        SetState(laser);
    }

    void SetState(State state)
    {
        CurrentState = state;
        StateChanged?.Invoke(state);
    }
}
