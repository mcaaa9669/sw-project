
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SoulWorldController : MonoBehaviour
{
    [SerializeField] private GameHUD gameHUD;
    [SerializeField] private float maxSoulTime = 5f;

    public event Action OnSoulWorldEntered;
    public event Action OnReturnRequested;

    private float remainingTime;
    private bool isSoulWorld = false;
    private int deathCount = 0;

    public bool IsSoulWorld => isSoulWorld;
    public int DeathCount => deathCount;

    // 사망 신호 수신
    public void HandlePlayerDeath()
    {
        // 영혼세계에서 중복 신호 무시
        if (isSoulWorld)
            return;

        deathCount++;

        gameHUD.SetDeathCount(deathCount);

        // 영혼세계 진입
        EnterSoulWorld();
    }

    // 영혼세계 진입 및 타이머 시작
    public void EnterSoulWorld()
    {
        if (isSoulWorld)
            return;

        isSoulWorld = true;
        remainingTime = maxSoulTime;

        gameHUD.SetSoulTime(remainingTime, maxSoulTime);
        gameHUD.ShowSoulTimer(true);

        OnSoulWorldEntered?.Invoke();
    }

    private void Update()
    {
        // 영혼세계가 아니면 타이머 실행 X
        if (!isSoulWorld)
            return;

        // 남은 시간 감소
        remainingTime = Mathf.Max(
            0f,
            remainingTime - Time.deltaTime
        );

        // 게이지 갱신
        gameHUD.SetSoulTime(remainingTime, maxSoulTime);

        // E키로 조기 복귀
        bool pressedE =
            Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame;

        // 시간 종료 또는 E키 입력
        if (remainingTime <= 0f || pressedE)
        {
            RequestReturn();
        }
    }

    // 현실세계 복귀 요청
    private void RequestReturn()
    {
        isSoulWorld = false;

        gameHUD.ShowSoulTimer(false);

        OnReturnRequested?.Invoke();
    }

    // 스테이지 재시작 시 초기화
    public void ResetDeathCount()
    {
        deathCount = 0;
        gameHUD.SetDeathCount(deathCount);
    }
}
