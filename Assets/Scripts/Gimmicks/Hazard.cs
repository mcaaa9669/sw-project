using UnityEngine;

public class Hazard : MonoBehaviour
{
    [SerializeField] private bool isActive = true;
    [SerializeField] private DeathType deathType = DeathType.None;
    [SerializeField] private bool stickShellToSaw;

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

        if (stickShellToSaw && deathType == DeathType.saw && target is PlayerController sawVictim)
        {
            sawVictim.Kill(deathType, transform);
            return;
        }

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