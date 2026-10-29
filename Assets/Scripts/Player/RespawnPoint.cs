using UnityEngine;

// 플레이어가 지나가면 부활 위치를 이 지점으로 갱신하는 체크포인트
[RequireComponent(typeof(Collider2D))]
public class RespawnPoint : MonoBehaviour
{
    [Tooltip("맵 진행 순서. 숫자가 클수록 뒤쪽 지점. 뒤로 돌아가도 앞 지점으로 되돌아가지 않음")]
    [SerializeField] int order = 0;

    [Header("표시 (선택)")]
    [SerializeField] SpriteRenderer marker;                 // 비우면 색 변경 안 함
    [SerializeField] Color inactiveColor = Color.gray;
    [SerializeField] Color activeColor = Color.yellow;

    public int Order => order;

    void Reset()
    {
        // 컴포넌트를 처음 붙일 때 콜라이더를 트리거로 설정
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Start()
    {
        if (marker) marker.color = inactiveColor;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        if (player.SetRespawnPoint(this))
        {
            if (marker) marker.color = activeColor;
        }
    }
}