
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

    public bool IsSoulWorld => isSoulWorld;

    // 사망 처리 후 영혼세계 진입 시 호출
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
        if (!isSoulWorld)
            return;

        remainingTime =
            Mathf.Max(0f, remainingTime - Time.deltaTime);

        gameHUD.SetSoulTime(remainingTime, maxSoulTime);

        bool pressedE = Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame;

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
}
