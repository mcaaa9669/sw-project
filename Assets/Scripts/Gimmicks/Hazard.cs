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
        if (!isActive) return;
        // 죽을 수 있는 오브젝트일 때
        if (other.TryGetComponent(out IKillable target))
        {
            target.Kill(deathType);
        }
    }
}
