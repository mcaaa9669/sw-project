using UnityEngine;

public class FallDeathZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        // ↓ 여기에 가시 스크립트에서 플레이어를 죽이는 줄을 그대로 복사
        // 예: other.GetComponent<PlayerDeath>().Die("Fall");
    }
}