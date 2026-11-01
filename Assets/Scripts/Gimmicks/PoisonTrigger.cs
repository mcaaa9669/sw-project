using UnityEngine;

public class PoisonTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player == null || player.IsDead) return;

        player.Kill(DeathType.poison);
    }
}