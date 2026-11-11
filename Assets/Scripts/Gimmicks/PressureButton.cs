
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

    private readonly HashSet<Collider2D> pressingColliders = new();

    void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInParent<SpriteRenderer>();

        if (targetRenderer != null && defaultSprite != null)
            targetRenderer.sprite = defaultSprite;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActiveAndEnabled ||
            (other.GetComponentInParent<PlayerController>() == null &&
             other.GetComponentInParent<Shell>() == null))
            return;

        if (!pressingColliders.Add(other))
            return;

        if (pressingColliders.Count == 1)
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
        if (!isActiveAndEnabled || !pressingColliders.Remove(other))
            return;

        if (pressingColliders.Count == 0)
        {
            // 원래 버튼 이미지로 복구
            if (targetRenderer != null && defaultSprite != null)
                targetRenderer.sprite = defaultSprite;

            Debug.Log("버튼 복구!");
            onReleased?.Invoke();
        }
    }
}
