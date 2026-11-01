
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class StageCameraController : MonoBehaviour
{
    [Header("Stage Settings")]
    [SerializeField] private float areaWidth = 20f;

    [Header("Camera Movement")]
    [SerializeField] private float moveDuration = 1.5f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Vector3 firstAreaPosition;

    private float elapsedTime;
    private bool isMoving = false;
    private int currentArea = 0;

    public int CurrentArea => currentArea;
    public bool IsMoving => isMoving;

    private void Awake()
    {
        firstAreaPosition = transform.position;
        targetPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (!isMoving) return;

        elapsedTime += Time.deltaTime;

        float t = Mathf.Clamp01(
            elapsedTime / Mathf.Max(moveDuration, 0.01f)
        );

        // 자연스러운 가속 및 감속
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

    // 다음 구역으로 이동
    public void MoveToNextArea()
    {
        if (isMoving) return;

        MoveToArea(currentArea + 1);
    }

    // 지정한 구역으로 이동 (0부터 시작)
    public void MoveToArea(int areaIndex)
    {
        if (isMoving || areaIndex < 0) return;

        currentArea = areaIndex;

        Vector3 center = firstAreaPosition +
            new Vector3(areaWidth * currentArea, 0f, 0f);

        MoveToStageCenter(center);
    }

    // 외부에서 전달한 중심 좌표로 이동
    public void MoveToStageCenter(Vector3 center)
    {
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
