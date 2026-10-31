
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PressureButton : MonoBehaviour
{
    // 버튼 이미지를 표시하는 컴포넌트
    [SerializeField] private SpriteRenderer targetRenderer;

    // 누르기 전 이미지
    [SerializeField] private Sprite defaultSprite;

    // 눌렸을 때 이미지
    [SerializeField] private Sprite pressedSprite;

    // 다른 장치를 작동시키는 이벤트
    public UnityEvent onPressed;
    public UnityEvent onReleased;

    private readonly HashSet<Collider2D> touchingPlayers = new();

    void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInParent<SpriteRenderer>();

        if (targetRenderer != null && defaultSprite != null)
            targetRenderer.sprite = defaultSprite;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어만 버튼을 누를 수 있음
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        if (!touchingPlayers.Add(other))
            return;

        if (touchingPlayers.Count == 1)
        {
            // 빨간 버튼이 낮아진 이미지로 변경
            if (targetRenderer != null && pressedSprite != null)
                targetRenderer.sprite = pressedSprite;

            Debug.Log("버튼 눌림!");
            onPressed?.Invoke();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!touchingPlayers.Remove(other))
            return;

        if (touchingPlayers.Count == 0)
        {
            // 원래 버튼 이미지로 복구
            if (targetRenderer != null && defaultSprite != null)
                targetRenderer.sprite = defaultSprite;

            Debug.Log("버튼 복구!");
            onReleased?.Invoke();
        }
    }
}
