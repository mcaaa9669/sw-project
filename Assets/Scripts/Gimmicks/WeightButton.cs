using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

// 무게 버튼 (압력 유지 방식)
// - 기존 PressureButton(닿는 순간·떠나는 순간 방식)과 별도로 사용
// - 위에 무언가 올라가 있는 동안만 눌림 (shell이 얹혀 있거나, 살아 있는 플레이어가 밟고 있음)
// - 압력이 사라지면 다시 올라옴
// - 매 물리 프레임마다 버튼 위를 직접 검사하므로, 플레이어가 그 자리에서 죽거나
//   영혼이 되거나 shell이 사라져도 상태가 정확히 바뀜
[RequireComponent(typeof(Collider2D))]
public class WeightButton : MonoBehaviour
{
    // 버튼 이미지를 표시하는 컴포넌트
    [SerializeField] private SpriteRenderer targetRenderer;

    // 누르기 전 이미지
    [SerializeField] private Sprite defaultSprite;

    // 눌렸을 때 이미지
    [SerializeField] private Sprite pressedSprite;

    [Tooltip("압력이 사라진 뒤 버튼이 올라오기까지 기다리는 시간. 점프로 잠깐 뜰 때 깜빡이지 않게 함")]
    [SerializeField] private float releaseDelay = 0.1f;
    [SerializeField] private bool holdShellOnPlate;
    [SerializeField] private bool soulOnly;
    [FormerlySerializedAs("effectsOnlyInSpiritWorld")]
    [SerializeField] private bool allowPlayerOnShellPlate;
    [SerializeField] private GameObject platformWhilePressed;
    [SerializeField] private Transform pressurePlayer;

    // 다른 장치를 작동시키는 이벤트
    public UnityEvent onPressed;
    public UnityEvent onReleased;

    public bool IsPressed { get; private set; }

    private PlayerController assignedPlayer;
    private Collider2D assignedPlayerCollider;
    private Collider2D area;                                    // 버튼 위 감지 영역 (Is Trigger)
    private readonly List<Collider2D> overlaps = new();
    private ContactFilter2D filter;
    private float lastPressureTime = float.NegativeInfinity;

    void Awake()
    {
        area = GetComponent<Collider2D>();
        if (pressurePlayer != null)
            assignedPlayer = pressurePlayer.GetComponent<PlayerController>();
        else if (allowPlayerOnShellPlate && platformWhilePressed != null)
            assignedPlayer = FindFirstObjectByType<PlayerController>();

        if (assignedPlayer != null)
            assignedPlayerCollider = assignedPlayer.GetComponent<Collider2D>();

        if (targetRenderer == null)
            targetRenderer = GetComponentInParent<SpriteRenderer>();

        // 트리거는 무시하고, 실제 몸체(플레이어·shell) 콜라이더만 검사
        filter = new ContactFilter2D();
        filter.useTriggers = false;

        UpdateSprite();
    }

    void FixedUpdate()
    {
        bool hasPressure = HasPressure();
        if (assignedPlayer != null && assignedPlayerCollider != null && area.enabled &&
            assignedPlayerCollider.enabled && (!assignedPlayer.IsDead || assignedPlayer.IsSoul))
        {
            ColliderDistance2D contact = area.Distance(assignedPlayerCollider);
            bool playerOnPlate = contact.isValid && contact.distance <= 0.03f;
            hasPressure |= playerOnPlate;
        }
        if (hasPressure)
            lastPressureTime = Time.time;

        bool shouldBePressed = Time.time - lastPressureTime <= releaseDelay;

        if (shouldBePressed && !IsPressed)
        {
            IsPressed = true;
            UpdateSprite();
            Debug.Log("버튼 눌림!");
            onPressed?.Invoke();
        }
        else if (!shouldBePressed && IsPressed)
        {
            IsPressed = false;
            UpdateSprite();
            Debug.Log("버튼 복구!");
            onReleased?.Invoke();
        }

        if (platformWhilePressed != null && platformWhilePressed.activeSelf != shouldBePressed)
            platformWhilePressed.SetActive(shouldBePressed);
    }

    // 지금 버튼 위에 압력을 주는 것이 있는지
    bool HasPressure()
    {
        if (!area.enabled) return false;

        int count = area.Overlap(filter, overlaps);

        for (int i = 0; i < count; i++)
        {
            Collider2D other = overlaps[i];
            if (other == null || !other.enabled) continue;

            // shell (시체)
            if (!soulOnly && other.GetComponentInParent<Shell>() != null)
            {
                Rigidbody2D body = other.attachedRigidbody;
                if (holdShellOnPlate && body != null && body.bodyType == RigidbodyType2D.Dynamic)
                    body.linearVelocityX = 0f;

                return true;
            }

            if (holdShellOnPlate && !soulOnly && !allowPlayerOnShellPlate) continue;

            PlayerController player = other.GetComponentInParent<PlayerController>();
            if (player != null && (player.IsSoul || (!soulOnly && !player.IsDead)))
                return true;
        }

        return false;
    }

    void UpdateSprite()
    {
        if (targetRenderer == null) return;

        Sprite sprite = IsPressed ? pressedSprite : defaultSprite;
        if (sprite != null)
            targetRenderer.sprite = sprite;
    }
}