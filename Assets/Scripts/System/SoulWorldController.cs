
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SoulWorldController : MonoBehaviour
{
    [SerializeField] private GameHUD gameHUD;
    [SerializeField] private float maxSoulTime = 5f;
    [SerializeField, Min(1)]
    private int maxDeaths = 5;
    
    private void Start()
    {
        UpdateDeathDisplay();
    }

    private void UpdateDeathDisplay()
    {
        if (gameHUD != null)
            gameHUD.SetDeathCount(deathCount, maxDeaths);
    }

    public event Action OnSoulWorldEntered;
    public event Action OnReturnRequested;

    private float stageTimeOverride;
    private float activeDuration;
    private float remainingTime;
    private bool isSoulWorld = false;
    private int deathCount = 0;

    public bool IsSoulWorld => isSoulWorld;
    public int DeathCount => deathCount;

    public void SetStageTimeOverride(float seconds) => stageTimeOverride = seconds;

    // 사망 신호 수신
    public void HandlePlayerDeath()
    {
        // 영혼세계에서 중복 신호 무시
        if (isSoulWorld)
            return;

        RegisterDeath();
        EnterSoulWorld();
    }

    public void RegisterDeath()
    {
        deathCount++;
        UpdateDeathDisplay();
    }

    // 영혼세계 진입 및 타이머 시작
    public void EnterSoulWorld()
    {
        if (isSoulWorld)
            return;

        isSoulWorld = true;
        activeDuration = stageTimeOverride > 0f ? stageTimeOverride : maxSoulTime;
        remainingTime = activeDuration;

        if (gameHUD != null)
        {
            gameHUD.SetSoulTime(remainingTime, activeDuration);
            gameHUD.ShowSoulTimer(true);
        }

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
        if (gameHUD != null)
            gameHUD.SetSoulTime(remainingTime, activeDuration);

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
    public void RequestReturn()
    {
        if (!isSoulWorld)
            return;

        isSoulWorld = false;

        if (gameHUD != null)
            gameHUD.ShowSoulTimer(false);

        OnReturnRequested?.Invoke();
    }

    // 스테이지 재시작 시 초기화
    // 스테이지 재시작 시 초기화
    public void ResetDeathCount()
    {
        deathCount = 0;
        UpdateDeathDisplay();
    }

    
}
