
using UnityEngine;
using UnityEngine.InputSystem;

public class HUDTestController : MonoBehaviour
{
    [SerializeField] private GameHUD gameHUD;

    private int deathCount = 0;

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        // T키를 누르면 사망 횟수 증가
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            deathCount++;
            gameHUD.SetDeathCount(deathCount);
        }

        // R키를 누르면 초기화
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            deathCount = 0;
            gameHUD.SetDeathCount(deathCount);
        }
    }
}

