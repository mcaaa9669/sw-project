using UnityEngine;

// 버튼이 눌려 있는 동안 땅 아래로 내려가는 벽 (1-2 풀숲)
// PressureButton의 On Pressed → Sink(), On Released → Rise() 연결
public class SinkingWall : MonoBehaviour, IWorldPausable
{
    [SerializeField] float sinkDepth = 4f;   // 내려가는 거리 (벽 높이보다 조금 크게)
    [SerializeField] float speed = 6f;       // 초당 이동 거리

    [SerializeField] bool moveInSpiritWorld;

    Vector3 upPosition;     // 원래 위치
    Vector3 target;
    bool paused;

    void Awake()
    {
        upPosition = transform.position;
        target = upPosition;
    }

    // 버튼 눌림 → 땅 아래로
    public void Sink()
    {
        target = upPosition + Vector3.down * sinkDepth;
    }

    public void RetractUp()
    {
        target = upPosition + Vector3.up * sinkDepth;
    }

    // 버튼 떼짐 → 원래 위치로
    public void Rise()
    {
        target = upPosition;
    }

    // 영혼세계 동안 멈춤 (WorldStateManager가 자동 호출)
    public void SetWorldPaused(bool p) => paused = p && !moveInSpiritWorld;

    void Update()
    {
        if (paused) return;
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }
}