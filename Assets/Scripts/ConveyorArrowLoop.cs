using System.Collections.Generic;
using UnityEngine;

// 컨베이어 외곽 그림은 1장만 고정하고, 화살표만 타원 궤도를 따라 순환시킵니다.
// 기존 PlayerController / SurfaceEffector2D와 독립적인 '시각 효과 전용' 스크립트입니다.
[DisallowMultipleComponent]
public class ConveyorArrowLoop : MonoBehaviour
{
    [Header("고정 배경")]
    [SerializeField] private SpriteRenderer beltRenderer;

    [Header("움직이는 화살표")]
    [Min(4)] public int arrowCount = 20;
    [Min(0f)] public float visualSpeed = 0.65f;
    public bool moveRightOnTop = true;  // 체크: 윗면 → / 아랫면 ←
    [Range(0.5f, 2f)] public float arrowScale = 1f;
    [SerializeField] private Color arrowColor = new Color(1f, 0.94f, 0.82f, 1f);

    [Header("이미지에서 타원 궤도를 잡는 위치 (0~1)")]
    [Range(0f, 1f)] public float leftCenterX = 0.108f;
    [Range(0f, 1f)] public float rightCenterX = 0.870f;
    [Range(0f, 1f)] public float centerYFromTop = 0.486f;
    [Range(0.01f, 0.3f)] public float endRadiusByImageHeight = 0.128f;

    private readonly List<Transform> arrows = new List<Transform>();
    private readonly List<SpriteRenderer> arrowRenderers = new List<SpriteRenderer>();
    private Texture2D arrowTexture;
    private Sprite arrowSprite;
    private float travelled;
    private float xLeft, xRight, yCenter, radius, straightLength, loopLength;

    private void Reset()
    {
        beltRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (beltRenderer == null)
            beltRenderer = GetComponent<SpriteRenderer>();

        if (beltRenderer == null || beltRenderer.sprite == null)
        {
            Debug.LogError("ConveyorArrowLoop: 같은 오브젝트에 Sprite Renderer와 고정 배경 이미지를 지정하세요.", this);
            enabled = false;
            return;
        }

        BuildPath();
        CreateArrowSprite();

        for (int i = 0; i < arrowCount; i++)
        {
            GameObject marker = new GameObject("Arrow_" + (i + 1).ToString("00"));
            marker.transform.SetParent(transform, false);
            SpriteRenderer sr = marker.AddComponent<SpriteRenderer>();
            sr.sprite = arrowSprite;
            sr.color = arrowColor;
            sr.sortingLayerID = beltRenderer.sortingLayerID;
            sr.sortingOrder = Mathf.Min(32767, beltRenderer.sortingOrder + 1);
            marker.transform.localScale = Vector3.one * arrowScale;
            arrows.Add(marker.transform);
            arrowRenderers.Add(sr);
        }
        UpdateArrowPositions();
    }

    private void Update()
    {
        if (arrows.Count == 0) return;
        // 반복값은 좌표 계산에만 사용합니다. 궤도의 시작과 끝이 연결되어 있어
        // 6프레임 방식처럼 갑자기 거꾸로 돌아가는 현상이 없습니다.
        float direction = moveRightOnTop ? 1f : -1f;
        travelled = Mathf.Repeat(travelled + visualSpeed * direction * Time.deltaTime, loopLength);
        UpdateArrowPositions();
    }

    private void BuildPath()
    {
        Sprite sprite = beltRenderer.sprite;
        float ppu = sprite.pixelsPerUnit;
        float w = sprite.rect.width;
        float h = sprite.rect.height;
        Vector2 pivot = sprite.pivot;

        xLeft = (w * leftCenterX - pivot.x) / ppu;
        xRight = (w * rightCenterX - pivot.x) / ppu;
        yCenter = (h * (1f - centerYFromTop) - pivot.y) / ppu;
        radius = (h * endRadiusByImageHeight) / ppu;
        straightLength = Mathf.Max(0.01f, xRight - xLeft);
        loopLength = 2f * straightLength + 2f * Mathf.PI * radius;
    }

    private void UpdateArrowPositions()
    {
        for (int i = 0; i < arrows.Count; i++)
        {
            float distance = Mathf.Repeat(travelled + i * loopLength / arrows.Count, loopLength);
            Vector2 tangent;
            Vector2 xy = PositionOnLoop(distance, out tangent);
            if (!moveRightOnTop) tangent = -tangent;

            Transform t = arrows[i];
            t.localPosition = new Vector3(xy.x, xy.y, -0.001f);
            t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg);
            t.localScale = Vector3.one * arrowScale;
            // 톱니바퀴 주변에서는 화살표가 뒤로 들어간 것처럼 부드럽게 감춥니다.
            float remainingInTop = Mathf.Min(distance, straightLength - distance);
            float bottomStart = straightLength + Mathf.PI * radius;
            float bottomEnd = bottomStart + straightLength;
            float remainingInBottom = Mathf.Min(distance - bottomStart, bottomEnd - distance);
            float fadeDistance = Mathf.Max(0.001f, radius * 0.35f);
            float visibility = 0f;
            if (distance <= straightLength)
                visibility = Mathf.Clamp01(remainingInTop / fadeDistance);
            else if (distance >= bottomStart && distance <= bottomEnd)
                visibility = Mathf.Clamp01(remainingInBottom / fadeDistance);
            Color c = arrowColor;
            c.a *= visibility;
            arrowRenderers[i].color = c;
        }
    }

    private Vector2 PositionOnLoop(float d, out Vector2 tangent)
    {
        // 위쪽 직선: 오른쪽 이동
        if (d < straightLength)
        {
            tangent = Vector2.right;
            return new Vector2(xLeft + d, yCenter + radius);
        }
        d -= straightLength;

        float halfCircle = Mathf.PI * radius;
        // 오른쪽 U자 회전: 위에서 아래로 연속 이동
        if (d < halfCircle)
        {
            float a = Mathf.PI * 0.5f - d / radius;
            tangent = new Vector2(Mathf.Sin(a), -Mathf.Cos(a));
            return new Vector2(xRight + radius * Mathf.Cos(a), yCenter + radius * Mathf.Sin(a));
        }
        d -= halfCircle;

        // 아래쪽 직선: 왼쪽 이동
        if (d < straightLength)
        {
            tangent = Vector2.left;
            return new Vector2(xRight - d, yCenter - radius);
        }
        d -= straightLength;

        // 왼쪽 U자 회전: 아래에서 위로 연속 이동
        float angle = -Mathf.PI * 0.5f - d / radius;
        tangent = new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle));
        return new Vector2(xLeft + radius * Mathf.Cos(angle), yCenter + radius * Mathf.Sin(angle));
    }

    // 화살표 작은 그림은 실행 시 1번만 생성. PNG 6장도 별도 애니메이션도 필요 없습니다.
    private void CreateArrowSprite()
    {
        const int size = 64;
        arrowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        arrowTexture.wrapMode = TextureWrapMode.Clamp;
        arrowTexture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];

        Vector2 a = new Vector2(21f, 17f);
        Vector2 b = new Vector2(37f, 32f);
        Vector2 c = new Vector2(21f, 47f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float dist = Mathf.Min(DistanceToSegment(p, a, b), DistanceToSegment(p, b, c));
                // 바깥은 은은하게, 안쪽은 밝은 크림색
                float alpha = Mathf.Clamp01((4.5f - dist) / 1.5f);
                pixels[y * size + x] = new Color(1f, 0.95f, 0.82f, alpha);
            }
        }
        arrowTexture.SetPixels(pixels);
        arrowTexture.Apply(false, false);
        arrowSprite = Sprite.Create(arrowTexture, new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f), beltRenderer.sprite.pixelsPerUnit);
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return Vector2.Distance(p, a + ab * t);
    }

    private void OnDestroy()
    {
        if (arrowSprite != null) Destroy(arrowSprite);
        if (arrowTexture != null) Destroy(arrowTexture);
    }
}
