using System;
using System.Collections.Generic;
using UnityEngine;

// 현재 세계 종류
public enum WorldType { Real, Spirit }

// 영혼세계 동안 멈춰야 하는 스크립트가 구현 (예: 톱니 회전, 움직이는 발판)
public interface IWorldPausable
{
    void SetWorldPaused(bool paused);
}

// SFR-004 현실세계 ↔ 영혼세계 전환 관리
// - SoulWorldController의 진입/복귀 이벤트를 받아 세계를 바꿈
// - 영혼세계 동안 현실세계의 물리·애니메이션·스크립트를 일시정지하고, 복귀하면 재개
// - 세계가 바뀌면 OnWorldChanged 이벤트로 WorldObject 등에 알림
public class WorldStateManager : MonoBehaviour
{
    public static WorldStateManager Instance { get; private set; }
    public static event Action<WorldType> OnWorldChanged;

    [SerializeField] SoulWorldController soulWorldController;

    [Header("환경 효과 (임시, 연출 담당이 교체)")]
    [SerializeField] bool changeCameraBackground = true;
    [SerializeField] Color spiritBackgroundColor = new Color(0.12f, 0.10f, 0.25f);

    public WorldType CurrentWorld { get; private set; } = WorldType.Real;
    public bool IsSpirit => CurrentWorld == WorldType.Spirit;

    struct FrozenBody
    {
        public Rigidbody2D rb;
        public RigidbodyType2D bodyType;
        public Vector2 velocity;
        public float angularVelocity;
    }

    readonly List<FrozenBody> frozenBodies = new();
    readonly List<(Animator animator, float speed)> pausedAnimators = new();
    readonly List<IWorldPausable> pausedScripts = new();

    Camera cam;
    Color realBackgroundColor;
    int spiritLayer;

    void Awake()
    {
        Instance = this;
        spiritLayer = LayerMask.NameToLayer("SpiritWorld");

        if (soulWorldController == null)
            soulWorldController = FindFirstObjectByType<SoulWorldController>();

        cam = Camera.main;
        if (cam != null) realBackgroundColor = cam.backgroundColor;
    }

    void OnEnable()
    {
        if (soulWorldController == null) return;
        soulWorldController.OnSoulWorldEntered += EnterSpiritWorld;
        soulWorldController.OnReturnRequested += ReturnToRealWorld;
    }

    void OnDisable()
    {
        if (soulWorldController == null) return;
        soulWorldController.OnSoulWorldEntered -= EnterSpiritWorld;
        soulWorldController.OnReturnRequested -= ReturnToRealWorld;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        // 게임 시작 시 현실세계 활성화
        OnWorldChanged?.Invoke(CurrentWorld);
    }

    void EnterSpiritWorld()
    {
        if (IsSpirit) return;
        CurrentWorld = WorldType.Spirit;

        FreezeRealWorld();
        if (changeCameraBackground && cam != null) cam.backgroundColor = spiritBackgroundColor;

        OnWorldChanged?.Invoke(CurrentWorld);
    }

    void ReturnToRealWorld()
    {
        if (!IsSpirit) return;
        CurrentWorld = WorldType.Real;

        UnfreezeRealWorld();
        if (changeCameraBackground && cam != null) cam.backgroundColor = realBackgroundColor;

        OnWorldChanged?.Invoke(CurrentWorld);
    }

    // ===== 현실세계 일시정지 =====

    void FreezeRealWorld()
    {
        frozenBodies.Clear();
        pausedAnimators.Clear();
        pausedScripts.Clear();

        // 1. 움직이는 물체(Shell, 상자 등) 정지
        //    simulated를 끄면 콜라이더도 사라지므로, Kinematic으로 바꿔서 멈추기만 함
        foreach (var rb in FindObjectsByType<Rigidbody2D>(FindObjectsSortMode.None))
        {
            if (!IsRealWorldObject(rb.gameObject)) continue;
            if (!rb.simulated || rb.bodyType == RigidbodyType2D.Static) continue;

            frozenBodies.Add(new FrozenBody
            {
                rb = rb,
                bodyType = rb.bodyType,
                velocity = rb.linearVelocity,
                angularVelocity = rb.angularVelocity
            });

            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // 2. 애니메이션 정지
        foreach (var animator in FindObjectsByType<Animator>(FindObjectsSortMode.None))
        {
            if (!IsRealWorldObject(animator.gameObject)) continue;
            pausedAnimators.Add((animator, animator.speed));
            animator.speed = 0f;
        }

        // 3. IWorldPausable 스크립트 정지 (톱니 회전 등)
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (mb is not IWorldPausable pausable) continue;
            if (!IsRealWorldObject(mb.gameObject)) continue;
            pausable.SetWorldPaused(true);
            pausedScripts.Add(pausable);
        }
    }

    void UnfreezeRealWorld()
    {
        foreach (var f in frozenBodies)
        {
            if (f.rb == null) continue;
            f.rb.bodyType = f.bodyType;
            f.rb.linearVelocity = f.velocity;
            f.rb.angularVelocity = f.angularVelocity;
        }

        foreach (var (animator, speed) in pausedAnimators)
        {
            if (animator != null) animator.speed = speed;
        }

        foreach (var pausable in pausedScripts)
        {
            if (pausable is MonoBehaviour mb && mb != null)
                pausable.SetWorldPaused(false);
        }

        frozenBodies.Clear();
        pausedAnimators.Clear();
        pausedScripts.Clear();
    }

    // 플레이어(영혼)와 영혼세계 전용 오브젝트는 멈추지 않음
    bool IsRealWorldObject(GameObject go)
    {
        if (go.layer == spiritLayer) return false;
        if (go.GetComponentInParent<PlayerController>() != null) return false;

        WorldObject worldObject = go.GetComponentInParent<WorldObject>();
        if (worldObject != null && worldObject.Visible == WorldObject.VisibleIn.SpiritOnly) return false;

        return true;
    }
}