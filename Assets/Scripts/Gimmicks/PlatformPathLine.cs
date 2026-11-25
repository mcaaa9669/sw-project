using UnityEngine;

// 이동 발판의 경로를 게임 화면에 점선으로 표시
// - MovingPlatform이 붙은 오브젝트에 같이 붙임 (설정 없이 바로 동작)
// - 경로는 MovingPlatform의 Points를 그대로 사용 (Loop면 닫힌 선, PingPong이면 열린 선)
// - 선은 발판과 따로 떨어진 오브젝트로 만들어서, 발판이 움직여도 제자리에 남음
[RequireComponent(typeof(MovingPlatform))]
public class PlatformPathLine : MonoBehaviour
{
    [Header("선")]
    [SerializeField] Color color = new Color(0.55f, 0.9f, 1f, 0.6f);
    [SerializeField, Min(0.01f)] float width = 0.06f;
    [SerializeField] bool dashed = true;
    [SerializeField, Min(0.05f)] float dashLength = 0.2f;   // 점선 한 칸 길이

    [Header("위치 조정 (실제 이동 경로는 그대로, 보이는 선만 옮김)")]
    [SerializeField] Vector2 offset = Vector2.zero;
    [SerializeField] Transform visualStart;
    [SerializeField] Transform visualEnd;

    [Header("지점 표시")]
    [SerializeField] bool showPoints = true;
    [SerializeField, Min(0.01f)] float pointSize = 0.18f;

    [Header("그리는 순서 (해제하면 발판 바로 뒤에 그림)")]
    [SerializeField] bool overrideSorting = false;
    [SerializeField] string sortingLayerName = "Default";
    [SerializeField] int sortingOrder = 0;

    [Header("머티리얼 (비우면 자동 생성. 빌드에서 선이 안 보이면 Sprite-Unlit-Default 넣기)")]
    [SerializeField] Material material;

    GameObject pathRoot;
    MovingPlatform platform;
    LineRenderer line;
    Vector2 displayedOffset;

    void Start()
    {
        platform = GetComponent<MovingPlatform>();
        Vector2[] path = GetDisplayPath(platform);
        if (path.Length < 2) return;
        for (int i = 0; i < path.Length; i++) path[i] += offset;

        // 그리는 순서: 발판 렌더러 바로 뒤
        int layerId = SortingLayer.NameToID(sortingLayerName);
        int order = sortingOrder;
        if (!overrideSorting)
        {
            var r = GetComponentInChildren<Renderer>();
            if (r != null)
            {
                layerId = r.sortingLayerID;
                order = r.sortingOrder - 1;
            }
        }

        pathRoot = new GameObject($"{name}_Path");

        // 선
        var lr = line = pathRoot.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = !HasVisualPath && platform.IsLoop;
        lr.positionCount = path.Length;
        for (int i = 0; i < path.Length; i++)
            lr.SetPosition(i, new Vector3(path[i].x, path[i].y, 0f));

        lr.startWidth = lr.endWidth = width;
        lr.startColor = lr.endColor = color;
        lr.numCapVertices = 0;
        lr.numCornerVertices = 0;
        lr.sortingLayerID = layerId;
        lr.sortingOrder = order;

        Material mat = CreateMaterial();
        if (dashed)
        {
            mat.mainTexture = MakeTexture(new[] { (Color32)Color.white, new Color32(255, 255, 255, 0) });
            lr.textureMode = LineTextureMode.Tile;
            lr.textureScale = new Vector2(1f / (dashLength * 2f), 1f);
        }
        lr.material = mat;

        // 지점 표시 (작은 네모)
        if (showPoints)
        {
            var dotTex = MakeTexture(new[] { (Color32)Color.white });
            var dotSprite = Sprite.Create(dotTex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

            foreach (var p in path)
            {
                var dot = new GameObject("Point");
                dot.transform.SetParent(pathRoot.transform, false);
                dot.transform.position = new Vector3(p.x, p.y, 0f);
                dot.transform.localScale = Vector3.one * pointSize;

                var sr = dot.AddComponent<SpriteRenderer>();
                sr.sprite = dotSprite;
                sr.color = color;
                sr.sortingLayerID = layerId;
                sr.sortingOrder = order;
            }
        }

        displayedOffset = offset;
        platform.PathChanged += RefreshPath;
    }

    void LateUpdate()
    {
        if (line == null) return;

        if (displayedOffset != offset ||
            (HasVisualPath && ((Vector2)line.GetPosition(0) != (Vector2)visualStart.position + offset ||
                               (Vector2)line.GetPosition(1) != (Vector2)visualEnd.position + offset)))
            RefreshPath();
    }

    bool HasVisualPath => visualStart != null && visualEnd != null;

    Vector2[] GetDisplayPath(MovingPlatform source)
    {
        if (HasVisualPath)
            return new Vector2[] { visualStart.position, visualEnd.position };

        return source.GetPathPoints();
    }

    void RefreshPath()
    {
        Vector2[] path = GetDisplayPath(platform);
        for (int i = 0; i < path.Length; i++)
        {
            Vector2 position = path[i] + offset;
            line.SetPosition(i, position);
            if (i < pathRoot.transform.childCount)
                pathRoot.transform.GetChild(i).position = position;
        }
        displayedOffset = offset;
    }

    Material CreateMaterial()
    {
        if (material != null) return new Material(material);

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        return new Material(shader);
    }

    static Texture2D MakeTexture(Color32[] pixels)
    {
        var tex = new Texture2D(pixels.Length, 1, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    // 씬 뷰 미리보기: 발판을 선택하면 실제로 그려질 선 위치를 노란색으로 표시
    void OnDrawGizmosSelected()
    {
        var platform = GetComponent<MovingPlatform>();
        if (platform == null) return;

        Vector2[] path = GetDisplayPath(platform);
        if (path.Length < 2) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < path.Length - 1; i++)
            Gizmos.DrawLine(path[i] + offset, path[i + 1] + offset);
        if (!HasVisualPath && platform.IsLoop)
            Gizmos.DrawLine(path[^1] + offset, path[0] + offset);
    }

    void OnEnable()
    {
        if (pathRoot != null) pathRoot.SetActive(true);
    }

    void OnDisable()
    {
        if (pathRoot != null) pathRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if (platform != null) platform.PathChanged -= RefreshPath;
        if (pathRoot != null) Destroy(pathRoot);
    }
}