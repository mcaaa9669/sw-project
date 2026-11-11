using UnityEngine;

// 화면 프레임 장식: 화면에 붙어 있다가, 카메라가 움직이는 동안에만 살짝 뒤처짐
[DefaultExecutionOrder(100)]
public class ScreenFrameLayer : MonoBehaviour
{
    [SerializeField] private Camera cam;

    [Tooltip("카메라 이동 속도 대비 뒤처지는 정도. 앞쪽 레이어일수록 크게")]
    [SerializeField] private float lagAmount = 0.06f;

    [Tooltip("최대로 밀리는 거리(유닛). 이미지 여유보다 작게")]
    [SerializeField] private float maxOffset = 0.5f;

    [Tooltip("제자리로 돌아오는 속도")]
    [SerializeField] private float returnSpeed = 5f;

    private Vector3 baseOffset;   // 에디터에서 놓은 '카메라 기준' 위치
    private Vector3 prevCamPos;
    private Vector2 lag;

    private void Start()
    {
        if (cam == null) cam = Camera.main;
        baseOffset = transform.position - cam.transform.position;
        prevCamPos = cam.transform.position;
    }

    private void LateUpdate()
    {
        Vector3 c = cam.transform.position;
        Vector2 velocity = (c - prevCamPos) / Mathf.Max(Time.deltaTime, 0.0001f);
        prevCamPos = c;

        // 카메라가 움직이는 반대 방향으로 밀림 (리스폰 순간이동은 maxOffset으로 제한)
        Vector2 target = Vector2.ClampMagnitude(-velocity * lagAmount, maxOffset);
        lag = Vector2.Lerp(lag, target, 1f - Mathf.Exp(-returnSpeed * Time.deltaTime));

        transform.position = c + baseOffset + (Vector3)lag;
    }
}