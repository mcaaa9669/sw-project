using UnityEngine;
using UnityEngine.Events;

// 닿으면 당겨지는 레버 (E키는 영혼세계 복귀에 쓰이므로 접촉으로 작동)
// - soulOnly: 영혼만 당길 수 있음
// - onActivated에 LaserSignal.StartDevices / StopDevices 등을 연결
[RequireComponent(typeof(Collider2D))]
public class Lever : MonoBehaviour
{
    [SerializeField] SpriteRenderer targetRenderer;
    [SerializeField] Sprite offSprite;
    [SerializeField] Sprite onSprite;

    [SerializeField] bool soulOnly = false;

    public UnityEvent onActivated;

    public bool IsPulled { get; private set; }

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<SpriteRenderer>();

        UpdateSprite();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (IsPulled) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        // 영혼 전용이면 영혼만, 아니면 살아 있는 플레이어나 영혼
        bool canPull = soulOnly ? player.IsSoul : (!player.IsDead || player.IsSoul);
        if (!canPull) return;

        IsPulled = true;
        UpdateSprite();
        onActivated?.Invoke();
    }

    void UpdateSprite()
    {
        if (targetRenderer == null) return;

        Sprite sprite = IsPulled ? onSprite : offSprite;
        if (sprite != null)
            targetRenderer.sprite = sprite;
    }
}
