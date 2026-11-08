using UnityEngine;

public class Hazard : MonoBehaviour
{
    [SerializeField] private bool isActive = true;
    [SerializeField] private DeathType deathType = DeathType.None;

    public void SetLethal(bool active)
    {
        isActive = active;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryKill(other);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryKill(collision.collider);
    }

    void TryKill(Collider2D other)
    {
        if (!isActive || !other.TryGetComponent(out IKillable target))
            return;

        if (deathType == DeathType.Spike && target is PlayerController player)
        {
            var rotatingBlock = GetComponentInParent<RotatingSpikeBlock>();
            if (rotatingBlock != null)
            {
                player.Kill(deathType, rotatingBlock.transform);
                return;
            }
        }

        target.Kill(deathType);
    }
}