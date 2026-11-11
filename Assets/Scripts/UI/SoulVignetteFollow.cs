using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// 영혼세계 비네팅의 중심을 플레이어 위치로 따라가게 함
// SoulWorldVignette 오브젝트(Volume이 있는 곳)에 같이 붙여서 사용
[DefaultExecutionOrder(120)]
[RequireComponent(typeof(Volume))]
public class SoulVignetteFollow : MonoBehaviour
{
    [SerializeField] Transform target;      // 비우면 PlayerController를 자동으로 찾음
    [SerializeField] Camera cam;            // 비우면 Main Camera 사용
    [SerializeField] Vector2 offset = Vector2.zero; // 중심을 살짝 옮기고 싶을 때 (월드 단위)

    Vignette vignette;

    void Start()
    {
        if (cam == null) cam = Camera.main;

        if (target == null)
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null) target = player.transform;
        }

        // volume.profile은 이 씬 전용 복사본을 만들어서 원본 Profile 에셋은 안 바뀜
        Volume volume = GetComponent<Volume>();
        if (volume.profile.TryGet(out vignette))
            vignette.center.overrideState = true;
        else
            Debug.LogWarning("[SoulVignetteFollow] Profile에 Vignette가 없음", this);
    }

    void LateUpdate()
    {
        if (vignette == null || target == null || cam == null) return;

        // 플레이어 월드 좌표 → 화면 비율 좌표(0~1)
        Vector3 vp = cam.WorldToViewportPoint((Vector2)target.position + offset);
        vignette.center.value = new Vector2(vp.x, vp.y);
    }
}