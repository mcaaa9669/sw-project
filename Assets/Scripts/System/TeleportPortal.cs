using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// 플레이어(살아 있거나 영혼)가 포탈 범위 안에서 F키를 누르면 지정한 위치로 순간이동
// - destination에 빈 오브젝트(위치만)나 다른 TeleportPortal을 넣으면 됨
// - 포탈 두 개가 서로를 destination으로 가리키면 양방향 포탈
// - E키는 영혼세계 조기 복귀에 쓰이므로 포탈은 F키 사용
[RequireComponent(typeof(Collider2D))]
public class TeleportPortal : MonoBehaviour
{
    public enum VelocityMode
    {
        Keep,   // 누를 때 속도 그대로
        Zero,   // 멈춘 상태로 도착
    }

    [Tooltip("순간이동할 위치. 다른 TeleportPortal을 넣으면 양방향 포탈처럼 사용 가능")]
    [SerializeField] Transform destination;
    [Tooltip("도착 위치 보정값 (도착지 기준)")]
    [SerializeField] Vector2 arriveOffset = Vector2.zero;
    [SerializeField] VelocityMode velocityMode = VelocityMode.Zero;

    [Tooltip("영혼 상태에서 사용 가능")]
    [SerializeField] bool allowSoul = true;
    [Tooltip("살아 있는(현실) 상태에서 사용 가능")]
    [SerializeField] bool allowAlive = true;
    [Tooltip("F키를 누를 때 플레이어 상호작용 애니메이션 재생")]
    [SerializeField] bool playInteractionAnimation = true;
    [Tooltip("연달아 이동하지 않게 하는 최소 간격(초)")]
    [SerializeField, Min(0f)] float cooldown = 0.2f;

    [Tooltip("플레이어가 순간이동한 직후 호출 (중력 반전, 효과음, VFX 등을 연결)")]
    public UnityEvent<PlayerController> onPlayerTeleported;

    // 모든 포탈 공용: 한 번의 F 입력으로 여러 포탈이 연쇄 작동하지 않게 함
    // (도착한 포탈이 같은 프레임에 Update되면서 다시 돌려보내는 문제 방지)
    static int lastTeleportFrame = -1;
    static float lastTeleportTime = float.NegativeInfinity;

    Collider2D interactionArea;
    readonly System.Collections.Generic.List<Collider2D> overlaps = new();

    public Transform Destination => destination;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    // 포탈 상호작용 키 (GravityFlipper가 같은 키인지 비교할 때 사용)
    public const Key InteractKey = Key.F;

    void Awake()
    {
        interactionArea = GetComponent<Collider2D>();
        interactionArea.isTrigger = true;
    }

    void Update()
    {
        if (Time.timeScale <= 0f) return;
        Keyboard kb = Keyboard.current;
        if (kb == null || !kb[InteractKey].wasPressedThisFrame) return;
        if (lastTeleportFrame == Time.frameCount) return;
        if (Time.time - lastTeleportTime < cooldown) return;

        PlayerController player = FindPlayerInside();
        if (player == null) return;

        // 도착지가 비어 있으면 아무 일도 안 일어나므로 콘솔에 알려줌
        if (destination == null)
        {
            Debug.LogWarning($"[포탈] {name}: Destination이 비어 있어서 이동할 수 없음", this);
            return;
        }

        if (playInteractionAnimation)
            player.PlayInteraction(transform.position);

        Teleport(player);
    }

    // 지금 포탈 범위 안에 있는, 사용 가능한 플레이어
    PlayerController FindPlayerInside()
    {
        var filter = new ContactFilter2D { useTriggers = true };
        interactionArea.Overlap(filter, overlaps);

        foreach (Collider2D other in overlaps)
        {
            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (CanUse(player))
                return player;
        }
        return null;
    }

    bool CanUse(PlayerController player)
    {
        if (player == null) return false;
        if (player.IsSoul) return allowSoul;
        return allowAlive && !player.IsDead;
    }

    // 버튼·레버 이벤트 등에서 직접 호출할 수도 있음
    public void Teleport(PlayerController player)
    {
        if (player == null || destination == null) return;

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        Vector2 target = (Vector2)destination.position + arriveOffset;

        if (body != null)
        {
            body.position = target;
            if (velocityMode == VelocityMode.Zero)
                body.linearVelocity = Vector2.zero;
        }
        player.transform.position = new Vector3(target.x, target.y, player.transform.position.z);

        lastTeleportFrame = Time.frameCount;
        lastTeleportTime = Time.time;
        Debug.Log($"[포탈] {player.name} → {destination.name} ({target})", this);

        // 같은 오브젝트에 포탈 키(F)로 설정된 GravityFlipper가 있으면, 이동에 성공했을 때만 반전
        if (TryGetComponent(out GravityFlipper flipper))
            flipper.OnPortalTeleported(player);

        onPlayerTeleported?.Invoke(player);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 1f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        if (destination == null) return;

        Vector3 target = destination.position + (Vector3)arriveOffset;
        Gizmos.DrawLine(transform.position, target);
        Gizmos.DrawWireSphere(target, 0.15f);
    }
}
