using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RockPlatform : MonoBehaviour
{
    enum State { Off, Rattling, Moving }

    [Header("돌 감지")]
    public string rockTag = "Rock";
    public Transform checkPoint;                 // 발판 위에 둔 빈 오브젝트
    public Vector2 checkSize = new Vector2(2f, 0.5f);

    [Header("덜컥거림")]
    public float shakeAmount = 0.05f;
    public float rattleInterval = 1.2f;
    public float rattleDuration = 0.25f;

    [Header("위아래 이동")]
    public Vector2 moveDirection = Vector2.up;
    public float moveDistance = 3f;
    public float moveSpeed = 1.5f;

    Rigidbody2D rb;
    Vector2 startPos;
    Vector2 checkCenter;   // 시작할 때 감지 위치를 고정
    State state = State.Off;
    float moveTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        startPos = rb.position;
        checkCenter = checkPoint != null ? (Vector2)checkPoint.position : startPos;
    }

    public void Activate()
    {
        if (state == State.Off) state = State.Rattling;
    }

    void FixedUpdate()
    {
        switch (state)
        {
            case State.Rattling:
                if (!RockOnTop())
                {
                    state = State.Moving;
                    moveTimer = 0f;
                    rb.MovePosition(startPos);
                    break;
                }
                Vector2 offset = Vector2.zero;
                if (Time.time % rattleInterval < rattleDuration)
                    offset = Random.insideUnitCircle * shakeAmount;
                rb.MovePosition(startPos + offset);
                break;

            case State.Moving:
                moveTimer += Time.fixedDeltaTime * moveSpeed;
                float t = (1f - Mathf.Cos(moveTimer)) * 0.5f;
                rb.MovePosition(startPos + moveDirection.normalized * moveDistance * t);
                break;
        }
    }

    bool RockOnTop()
    {
        var hits = Physics2D.OverlapBoxAll(checkCenter, checkSize, 0f);
        foreach (var h in hits)
            if (h.CompareTag(rockTag)) return true;
        return false;
    }

    void OnDrawGizmosSelected()
    {
        Vector2 center = Application.isPlaying ? checkCenter
                       : (checkPoint != null ? (Vector2)checkPoint.position : (Vector2)transform.position);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(center, checkSize);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(center, center + moveDirection.normalized * moveDistance);
    }
}