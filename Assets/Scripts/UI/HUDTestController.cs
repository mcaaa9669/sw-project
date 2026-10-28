
using UnityEngine;
using UnityEngine.InputSystem;

public class HUDTestController : MonoBehaviour
{
    [SerializeField] private GameHUD gameHUD;

    private int deathCount = 0;

    private float remainingTime = 5f;
    private const float MaxTime = 5f;
    private bool isSoulWorld = false;

    private void Update()
    {
        if (Keyboard.current != null)
        {
            // T키: 사망 횟수 증가
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                deathCount++;
                gameHUD.SetDeathCount(deathCount);
            }

            // Y키: 영혼세계 진입 테스트
            if (Keyboard.current.yKey.wasPressedThisFrame)
            {
                remainingTime = MaxTime;
                isSoulWorld = true;

                gameHUD.SetSoulTime(remainingTime, MaxTime);
                gameHUD.ShowSoulTimer(true);
            }

            // E키: 현실세계 복귀 테스트
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                ExitSoulWorld();
            }

            // R키: 테스트 초기화
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                deathCount = 0;
                gameHUD.SetDeathCount(0);
                ExitSoulWorld();
            }
        }

        // 영혼세계 활동 중 시간 감소
        if (isSoulWorld)
        {
            remainingTime -= Time.deltaTime;
            remainingTime = Mathf.Max(0f, remainingTime);

            gameHUD.SetSoulTime(remainingTime, MaxTime);

            if (remainingTime <= 0f)
            {
                ExitSoulWorld();
            }
        }
    }

    private void ExitSoulWorld()
    {
        isSoulWorld = false;
        gameHUD.ShowSoulTimer(false);
    }
}
