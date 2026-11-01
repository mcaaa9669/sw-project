using UnityEngine;

// 특정 세계에서만 보이고 밟히는 오브젝트 (SFR-004, SFR-005)
// 영혼세계 전용 발판·장애물이나, 현실에만 있는 벽 등에 붙여서 사용
public class WorldObject : MonoBehaviour
{
    public enum VisibleIn { SpiritOnly, RealOnly }

    [SerializeField] VisibleIn visibleIn = VisibleIn.SpiritOnly;

    public VisibleIn Visible => visibleIn;

    Renderer[] renderers;
    Collider2D[] colliders;

    void Awake()
    {
        // 자식까지 포함 (타일맵 렌더러·콜라이더도 포함됨)
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider2D>(true);
    }

    void OnEnable()
    {
        WorldStateManager.OnWorldChanged += Apply;
        Apply(WorldStateManager.Instance != null ? WorldStateManager.Instance.CurrentWorld : WorldType.Real);
    }

    void OnDisable()
    {
        WorldStateManager.OnWorldChanged -= Apply;
    }

    // SetActive를 끄면 이벤트를 못 받으므로, 보이기·충돌만 켜고 끔
    void Apply(WorldType world)
    {
        bool active = (world == WorldType.Spirit) == (visibleIn == VisibleIn.SpiritOnly);

        foreach (var r in renderers) if (r != null) r.enabled = active;
        foreach (var c in colliders) if (c != null) c.enabled = active;
    }
}