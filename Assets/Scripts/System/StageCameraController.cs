using UnityEngine;

[RequireComponent(typeof(Camera))]
public class StageCameraController : MonoBehaviour
{
    [Header("카메라 위치 목록 — 0번은 시작 위치")]
    [SerializeField] private Transform[] areaPositions;

    [Header("카메라 이동 시간")]
    [SerializeField] private float moveDuration = 1.5f;

    private Vector3 startPosition;
    private Vector3 targetPosition;

    private float elapsedTime;
    private bool isMoving;
    private int currentArea;

    public int CurrentArea => currentArea;
    public bool IsMoving => isMoving;

    private void Awake()
    {
        currentArea = 0;

        if (areaPositions != null &&
            areaPositions.Length > 0 &&
            areaPositions[0] != null)
        {
            Vector3 position = areaPositions[0].position;

            transform.position = new Vector3(
                position.x,
                position.y,
                transform.position.z
            );
        }

        startPosition = transform.position;
        targetPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (!isMoving) return;

        elapsedTime += Time.deltaTime;

        float t = Mathf.Clamp01(
            elapsedTime / Mathf.Max(moveDuration, 0.01f)
        );

        float smoothT = Mathf.SmoothStep(0f, 1f, t);

        transform.position = Vector3.Lerp(
            startPosition,
            targetPosition,
            smoothT
        );

        if (t >= 1f)
        {
            transform.position = targetPosition;
            isMoving = false;
        }
    }

    // 외부에서 카메라 이동 신호를 받을 때 호출
    public void MoveToNextArea()
    {
        MoveToArea(currentArea + 1);
    }

    // 목록에 등록한 위치로 이동 — 0번부터 시작
    public void MoveToArea(int areaIndex)
    {
        if (isMoving) return;

        if (areaPositions == null ||
            areaIndex < 0 ||
            areaIndex >= areaPositions.Length)
            return;

        Transform nextPosition = areaPositions[areaIndex];

        if (nextPosition == null)
        {
            Debug.LogWarning(
                $"[StageCameraController] {areaIndex}번 위치가 비어 있습니다.",
                this
            );
            return;
        }

        currentArea = areaIndex;
        MoveToStageCenter(nextPosition.position);
    }

    // 특정 좌표로 직접 이동 — 기존 트리거와의 호환용
    // 이 함수를 직접 호출하면 목록의 순번은 바뀌지 않음
    public void MoveToStageCenter(Vector3 center)
    {
        if (isMoving) return;

        startPosition = transform.position;

        targetPosition = new Vector3(
            center.x,
            center.y,
            transform.position.z
        );

        elapsedTime = 0f;
        isMoving = true;
    }
}