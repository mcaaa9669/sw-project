using System;
using System.Collections.Generic;
using UnityEngine;

// ���� ���� ����
public enum WorldType { Real, Spirit }

// ��ȥ���� ���� ����� �ϴ� ��ũ��Ʈ�� ���� (��: ��� ȸ��, �����̴� ����)
public interface IWorldPausable
{
    void SetWorldPaused(bool paused);
}

// SFR-004 ���Ǽ��� �� ��ȥ���� ��ȯ ����
// - SoulWorldController�� ����/���� �̺�Ʈ�� �޾� ���踦 �ٲ�
// - ��ȥ���� ���� ���Ǽ����� �������ִϸ��̼ǡ���ũ��Ʈ�� �Ͻ������ϰ�, �����ϸ� �簳
// - ���谡 �ٲ�� OnWorldChanged �̺�Ʈ�� WorldObject � �˸�
public class WorldStateManager : MonoBehaviour
{
    public static WorldStateManager Instance { get; private set; }
    public static event Action<WorldType> OnWorldChanged;

    [SerializeField] SoulWorldController soulWorldController;

    [Header("ȯ�� ȿ�� (�ӽ�, ���� ����� ��ü)")]
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

    [Serializable]
    private class BackgroundLayer
    {
        public SpriteRenderer renderer;
        public Sprite spiritSprite;
        [NonSerialized] public Sprite realSprite;
    }

    [SerializeField] private BackgroundLayer[] backgroundLayers = Array.Empty<BackgroundLayer>();

    Camera cam;
    Color realBackgroundColor;
    int spiritLayer;

    void Awake()
    {
        Instance = this;
        foreach (var layer in backgroundLayers)
        {
            if (layer?.renderer != null)
                layer.realSprite = layer.renderer.sprite;
        }
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
        // ���� ���� �� ���Ǽ��� Ȱ��ȭ
        OnWorldChanged?.Invoke(CurrentWorld);
    }

    void EnterSpiritWorld()
    {
        if (IsSpirit) return;
        CurrentWorld = WorldType.Spirit;

        SwapBackgroundSprites();
        FreezeRealWorld();
        if (changeCameraBackground && cam != null) cam.backgroundColor = spiritBackgroundColor;

        OnWorldChanged?.Invoke(CurrentWorld);
    }

    void ReturnToRealWorld()
    {
        if (!IsSpirit) return;
        CurrentWorld = WorldType.Real;

        SwapBackgroundSprites();
        UnfreezeRealWorld();
        if (changeCameraBackground && cam != null) cam.backgroundColor = realBackgroundColor;

        OnWorldChanged?.Invoke(CurrentWorld);
    }

    // ===== ���Ǽ��� �Ͻ����� =====

    private void SwapBackgroundSprites()
    {
        foreach (var layer in backgroundLayers)
        {
            if (layer?.renderer == null || layer.spiritSprite == null)
                continue;

            layer.renderer.sprite = IsSpirit ? layer.spiritSprite : layer.realSprite;
        }
    }

    void FreezeRealWorld()
    {
        frozenBodies.Clear();
        pausedAnimators.Clear();
        pausedScripts.Clear();

        // 1. �����̴� ��ü(Shell, ���� ��) ����
        //    simulated�� ���� �ݶ��̴��� ������Ƿ�, Kinematic���� �ٲ㼭 ���߱⸸ ��
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

        // 2. �ִϸ��̼� ����
        foreach (var animator in FindObjectsByType<Animator>(FindObjectsSortMode.None))
        {
            if (!IsRealWorldObject(animator.gameObject)) continue;
            if (animator.TryGetComponent<Shell>(out _)) continue;   // Shell 사망 모션은 영혼세계에서도 재생
            pausedAnimators.Add((animator, animator.speed));
            animator.speed = 0f;
        }

        // 3. IWorldPausable ��ũ��Ʈ ���� (��� ȸ�� ��)
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

    // �÷��̾�(��ȥ)�� ��ȥ���� ���� ������Ʈ�� ������ ����
    bool IsRealWorldObject(GameObject go)
    {
        if (go.layer == spiritLayer) return false;
        if (go.GetComponentInParent<PlayerController>() != null) return false;

        WorldObject worldObject = go.GetComponentInParent<WorldObject>();
        if (worldObject != null && worldObject.Visible == WorldObject.VisibleIn.SpiritOnly) return false;

        return true;
    }
}