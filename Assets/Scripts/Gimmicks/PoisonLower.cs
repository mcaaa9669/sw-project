using UnityEngine;
using Cainos.InteractivePixelWater;

// 버튼을 누르면 독 수위를 지정한 높이까지 천천히 내림
// - 독(Poison) 오브젝트에 붙임 (자식의 PF Pixel Water를 자동으로 찾음)
// - 버튼의 ButtonTrigger → PressureButton.onPressed → Lower() 연결
// - Target Level 오브젝트의 높이(y)까지 수면이 내려감 (한 번 내려가면 그대로 유지)
// - 영혼세계에서는 현실세계와 함께 멈춤
public class PoisonLower : MonoBehaviour, IWorldPausable
{
    [Header("수위")]
    [SerializeField] Transform targetLevel;                 // 수면이 내려갈 위치 (빈 오브젝트)
    [SerializeField, Min(0.01f)] float lowerSpeed = 1f;     // 초당 내려가는 높이
    [SerializeField] bool hideWhenEmpty = true;             // 바닥까지 다 빠지면 독 오브젝트 끄기

    PixelWater water;
    float targetFill;
    bool lowering;
    bool done;
    bool paused;

    void Awake()
    {
        water = GetComponentInChildren<PixelWater>();
        if (water == null)
        {
            Debug.LogWarning($"[PoisonLower] {name}: 자식에서 PixelWater를 찾지 못했습니다.");
            enabled = false;
        }
    }

    // 버튼에서 호출
    public void Lower()
    {
        if (done || lowering || water == null) return;

        if (targetLevel == null)
        {
            Debug.LogWarning($"[PoisonLower] {name}: Target Level이 비어 있습니다.");
            return;
        }

        // 독 바닥 ~ 목표 높이를 0~1 비율(fill)로 변환
        float height = water.size.y * water.transform.lossyScale.y;
        targetFill = Mathf.Clamp01((targetLevel.position.y - water.BottomPos) / height);
        lowering = true;
    }

    void Update()
    {
        if (!lowering || paused) return;

        float height = water.size.y * water.transform.lossyScale.y;
        water.fill = Mathf.MoveTowards(water.fill, targetFill, lowerSpeed / height * Time.deltaTime);
        water.Refresh();    // 메시·콜라이더·부력을 새 수위에 맞춤

        if (Mathf.Approximately(water.fill, targetFill))
        {
            lowering = false;
            done = true;

            if (hideWhenEmpty && targetFill <= 0f)
                gameObject.SetActive(false);
        }
    }

    public void SetWorldPaused(bool value)
    {
        paused = value;
    }

    // 씬 뷰에서 목표 수위 표시
    void OnDrawGizmosSelected()
    {
        if (targetLevel == null) return;

        var w = water != null ? water : GetComponentInChildren<PixelWater>();
        float halfWidth = w != null ? w.size.x * w.transform.lossyScale.x * 0.5f : 3f;
        float centerX = w != null ? w.transform.position.x : transform.position.x;
        float y = targetLevel.position.y;

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(new Vector3(centerX - halfWidth, y), new Vector3(centerX + halfWidth, y));
    }
}