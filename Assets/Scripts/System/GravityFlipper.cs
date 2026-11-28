using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 플레이어의 중력 방향을 바꾸는 기믹
// - activationKey = None : 트리거에 닿는 즉시 반전
// - activationKey = 지정 키 : 트리거 안에서 그 키를 눌러야 반전
// - 또는 다른 기믹(예: TeleportPortal.onPlayerTeleported)에서 Apply(PlayerController)를 직접 호출
// 실제 반전 처리(바닥 판정, 점프 방향, 몸 뒤집기)는 PlayerController.SetGravityFlipped가 담당
//
// 포탈과 같이 쓰기: 같은 오브젝트에 TeleportPortal(F키)과 이 스크립트를 붙이고 activationKey를 F로 지정
//   → F를 눌러 순간이동에 "성공했을 때만" 반전된 채로 도착 (포탈이 직접 OnPortalTeleported 호출)
//   → 도착지가 비어 있는 등 이동이 안 되면 반전도 안 됨
//   이 경우 onPlayerTeleported에 Apply를 연결하지 말 것 (중복 호출은 cooldown이 막지만 의도가 헷갈림)
[RequireComponent(typeof(Collider2D))]
public class GravityFlipper : MonoBehaviour
{
    public enum FlipMode
    {
        Toggle,     // 작동할 때마다 반대로
        Flip,       // 항상 반전 상태로 (천장으로 떨어짐)
        Normal,     // 항상 원래 상태로
    }

    [SerializeField] FlipMode mode = FlipMode.Toggle;
    [Tooltip("이 오브젝트의 트리거로 작동. 끄면 Apply() 호출(이벤트 연결)로만 작동")]
    [SerializeField] bool applyOnTrigger = true;
    [Tooltip("None: 닿는 즉시 반전 / 키 지정: 범위 안에서 그 키를 눌러야 반전")]
    [SerializeField] Key activationKey = Key.None;
    [Tooltip("같은 플레이어가 연달아 뒤집히지 않게 하는 간격(초)")]
    [SerializeField, Min(0f)] float cooldown = 0.3f;

    readonly Dictionary<PlayerController, float> lastApplyTime = new();
    // 트리거 안에 있는 플레이어 (플레이어 콜라이더가 여러 개일 수 있어 개수로 셈)
    readonly Dictionary<PlayerController, int> playersInside = new();
    readonly List<PlayerController> applyBuffer = new();

    TeleportPortal portal;   // 같은 오브젝트의 포탈

    bool UsesKey => activationKey != Key.None;
    // 포탈과 같은 키면 포탈이 이동에 성공했을 때만 반전 (직접 키 입력은 처리하지 않음)
    bool LinkedToPortal => portal != null && portal.enabled && activationKey == TeleportPortal.InteractKey;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Awake()
    {
        portal = GetComponent<TeleportPortal>();
    }

    void OnDisable()
    {
        playersInside.Clear();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        playersInside.TryGetValue(player, out int count);
        playersInside[player] = count + 1;

        // 키 설정이 없으면 닿는 즉시 반전
        if (applyOnTrigger && !UsesKey && count == 0)
            Apply(player);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || !playersInside.TryGetValue(player, out int count)) return;

        if (count <= 1) playersInside.Remove(player);
        else playersInside[player] = count - 1;
    }

    void Update()
    {
        if (!applyOnTrigger || !UsesKey || LinkedToPortal || playersInside.Count == 0) return;
        if (Time.timeScale <= 0f) return;

        Keyboard kb = Keyboard.current;
        if (kb == null || !kb[activationKey].wasPressedThisFrame) return;

        applyBuffer.Clear();
        applyBuffer.AddRange(playersInside.Keys);
        foreach (PlayerController player in applyBuffer)
            Apply(player);
    }

    // 같은 오브젝트의 TeleportPortal이 순간이동에 성공한 직후 호출
    public void OnPortalTeleported(PlayerController player)
    {
        if (applyOnTrigger && LinkedToPortal)
            Apply(player);
    }

    // UnityEvent<PlayerController>에 연결해서 호출 가능 (Dynamic 항목으로 선택)
    public void Apply(PlayerController player)
    {
        if (player == null || !isActiveAndEnabled) return;
        if (player.IsDead && !player.IsSoul) return;

        if (lastApplyTime.TryGetValue(player, out float last) && Time.time - last < cooldown)
            return;
        lastApplyTime[player] = Time.time;

        bool flipped = mode switch
        {
            FlipMode.Flip => true,
            FlipMode.Normal => false,
            _ => !player.IsGravityFlipped,
        };

        player.SetGravityFlipped(flipped);
        Debug.Log($"[중력 반전] {player.name} 반전={flipped} (모드={mode}, 키={activationKey})", this);
    }
}
