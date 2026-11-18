using UnityEngine;

// 돌이 올려진 이동 발판 (MovingPlatform과 같은 오브젝트에 붙임)
// - 버튼(PressureButton.onPressed → Activate)으로 작동
// - 돌이 위에 있는 동안: 제자리에서 덜컥거림
// - 돌이 떨어지면: MovingPlatform 경로대로 이동 시작 (이후 계속)
// - 이동 경로·속도는 MovingPlatform에서 설정
[RequireComponent(typeof(MovingPlatform))]
public class RockPlatform : MonoBehaviour, IWorldPausable
{
    enum State { Off, Rattling, Moving }

    [Header("돌 감지")]
    [SerializeField] string rockTag = "Rock";
    [SerializeField] Transform checkPoint;                      // 돌이 놓이는 위치
    [SerializeField] Vector2 checkSize = new Vector2(2f, 0.5f);

    [Header("덜컥거림")]
    [SerializeField] float shakeAmount = 0.05f;     // 흔들림 세기
    [SerializeField] float rattleInterval = 1.2f;   // 몇 초마다 덜컥
    [SerializeField] float rattleDuration = 0.25f;  // 한 번 덜컥하는 시간

    MovingPlatform platform;
    Rigidbody2D rb;
    Vector2 startPos;
    Vector2 checkCenter;
    State state = State.Off;
    float rattleTimer;
    bool paused;

    void Awake()
    {
        platform = GetComponent<MovingPlatform>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        startPos = rb.position;
        checkCenter = checkPoint != null ? (Vector2)checkPoint.position : startPos;
        platform.StopMoving();      // 버튼 누르기 전에는 가만히
    }

    // 버튼에서 호출
    public void Activate()
    {
        if (state != State.Off) return;
        state = State.Rattling;
        rattleTimer = 0f;
    }

    void FixedUpdate()
    {
        if (state != State.Rattling || paused) return;

        // 돌이 떨어졌으면 원위치로 맞춘 뒤 이동 시작
        if (!RockOnTop())
        {
            state = State.Moving;
            rb.position = startPos;
            platform.StartMoving();
            return;
        }

        rattleTimer += Time.fixedDeltaTime;
        Vector2 offset = Vector2.zero;
        if (rattleTimer % rattleInterval < rattleDuration)
            offset = Random.insideUnitCircle * shakeAmount;
        rb.MovePosition(startPos + offset);
    }

    bool RockOnTop()
    {
        foreach (var h in Physics2D.OverlapBoxAll(checkCenter, checkSize, 0f))
            if (h.CompareTag(rockTag)) return true;
        return false;
    }

    public void SetWorldPaused(bool value) => paused = value;

    void OnDrawGizmosSelected()
    {
        Vector2 center = Application.isPlaying ? checkCenter
                       : (checkPoint != null ? (Vector2)checkPoint.position : (Vector2)transform.position);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, checkSize);
    }
}