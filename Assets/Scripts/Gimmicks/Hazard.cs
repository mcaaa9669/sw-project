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
        TryKill(collision.collider, collision.contactCount > 0 ? collision.GetContact(0).point : null);
    }

    void TryKill(Collider2D other, Vector2? contact = null)
    {
        if (!isActive || !other.TryGetComponent(out IKillable target))
            return;

        if (target is not PlayerController player)
        {
            target.Kill(deathType);
            return;
        }

        Transform anchor = deathType switch
        {
            DeathType.saw when stickShellToSaw => transform,
            DeathType.Spike => GetComponentInParent<RotatingSpikeBlock>()?.transform,
            _ => null
        };
        bool wasAlive = !player.IsDead;
        Vector2 hit = contact ?? other.ClosestPoint(transform.position);
        Vector3 effectPosition = new Vector3(hit.x, hit.y, player.transform.position.z);
        player.Kill(deathType, anchor);
        if (wasAlive && player.IsDead)
            SHELLJungyoonVFXRuntime.PlayHazardImpact(gameObject.scene, deathType, effectPosition);
    }
}
