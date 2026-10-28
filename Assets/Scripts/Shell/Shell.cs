using UnityEngine;

// 플레이어가 죽은 자리에 남는 shell
// 사망 종류에 따라 PlayerController가 아래 함수 중 하나를 호출
[RequireComponent(typeof(Rigidbody2D))]
public class Shell : MonoBehaviour
{
    Rigidbody2D rb;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    // 가시: 죽은 위치에 고정
    public void OnSpikeDeath()
    {
        Freeze();
    }

    // 레이저: 물리 법칙에 따라 떨어지거나 경사로에서 미끄러짐
    public void OnLaserDeath()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.freezeRotation = true;                 // 굴러가지 않고 미끄러지게
    }

    // 독: 3초 뒤 그때의 위치에 고정
    public void OnPoisonDeath()
    {
        Freeze();
    }

    void Freeze()
    {
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;
    }
}