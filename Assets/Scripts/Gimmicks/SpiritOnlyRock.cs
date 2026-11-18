using UnityEngine;

// 영혼세계에서만 보이고 밀 수 있는 돌
// - 현실세계: 안 보이고 제자리에 고정됨. 플레이어·시체는 그냥 통과
//             (콜라이더를 트리거로만 바꾸므로 RockPlatform의 돌 감지에는 계속 잡힘 → 발판은 계속 덜컥거림)
// - 영혼세계: 보이고, 영혼이 밀어서 발판 밖으로 떨어뜨릴 수 있음
// - 영혼세계에서 돌을 치워두면, 현실세계로 돌아왔을 때 발판이 움직이기 시작함
// ※ WorldObject(영혼세계 전용)를 쓰면 현실세계에서 콜라이더가 꺼져서
//    돌이 바닥을 뚫고 떨어지고 발판도 돌이 없다고 판단하므로 이 스크립트를 따로 씀
[RequireComponent(typeof(Rigidbody2D))]
public class SpiritOnlyRock : MonoBehaviour
{
    Rigidbody2D rb;
    Renderer[] renderers;
    Collider2D[] colliders;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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

    void Apply(WorldType world)
    {
        bool spirit = world == WorldType.Spirit;

        // 보이기
        foreach (var r in renderers)
            if (r != null) r.enabled = spirit;

        // 현실세계에서는 트리거(통과 가능), 영혼세계에서는 단단한 물체
        foreach (var c in colliders)
            if (c != null) c.isTrigger = !spirit;

        // 현실세계에서는 고정, 영혼세계에서는 밀 수 있게
        rb.bodyType = spirit ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }
}