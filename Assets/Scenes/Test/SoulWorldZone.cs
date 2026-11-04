using UnityEngine;

// 플레이어가 지나가면 영혼세계 기능을 켜거나 끔
[RequireComponent(typeof(Collider2D))]
public class SoulWorldZone : MonoBehaviour
{
    [SerializeField] bool enableSoulWorld = true;

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        player.SetSoulWorldEnabled(enableSoulWorld);
    }
}