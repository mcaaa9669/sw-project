using System.Collections.Generic;
using UnityEngine;

// SHELL: 3~12 grid cells wide, fixed-height conveyor.
// This is independent of ConveyorArrowLoop.cs, which remains unchanged for existing prefabs.
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class SHELLResizableConveyorBelt : MonoBehaviour
{
    [Header("길이 (가로만 변경 / 세로 고정)")]
    [Range(3, 12)] public int lengthInTiles = 8;
    [Min(0.01f)] public float tileWorldSize = 1f;

    [Header("움직이는 화살표")]
    [Min(4)] public int arrowsAtEightTiles = 20;
    [Min(0f)] public float visualSpeed = 0.65f;
    public bool moveRightOnTop = true;
    [Range(0.5f, 2f)] public float arrowScale = 1f;
    public Color arrowColor = new Color(1f, 0.94f, 0.82f, 1f);

    [Header("플레이어를 이동시키는 벨트")]
    [Min(0f)] public float playerMoveSpeed = 2f;
    [Range(0f, 1f)] public float forceScale = 0.5f;

    [HideInInspector] public SpriteRenderer leftEnd;
    [HideInInspector] public SpriteRenderer centerBelt;
    [HideInInspector] public SpriteRenderer rightEnd;
    [HideInInspector] public SpriteRenderer[] braceRenderers;
    [HideInInspector] public BoxCollider2D surfaceCollider;
    [HideInInspector] public SurfaceEffector2D surfaceEffector;

    private const float PixelsPerUnit = 400f;
    private const float ArtworkHeight = 682f;
    // Precisely measured from the four sliced sprites (not from the old single-image ratios).
    // Coordinates are relative to the OUTER edge of each fixed endcap (in source pixels).
    // Left sprite is 340 px wide; right sprite is 368 px wide.
    // Cog centers are approximately 150 px from the left edge and 156 px from the right edge.
    private const float LeftCogPixelsFromEdge = 150f;
    private const float RightCogPixelsFromEdge = 156f;

    // Brown moving-belt strips occupy y ~= 247 px (top) and 435 px (bottom)
    // in the original 682-px-high sprites, measured from the TOP of the texture.
    // The sprite pivot is centered, so the local track center is approximately 0.
    // The turn radius is half the distance between those strip centerlines.
    private const float BeltTopYFromTop = 247f;
    private const float BeltBottomYFromTop = 435f;
    private const float EndRadius = (BeltBottomYFromTop - BeltTopYFromTop) / (2f * PixelsPerUnit);
    private const float CenterY = (ArtworkHeight * 0.5f - (BeltTopYFromTop + BeltBottomYFromTop) * 0.5f) / PixelsPerUnit;

    private bool layoutDirty = true;
    private int lastTiles = -1;
    private float lastTileWidth = -1f;
    private float lastWorldScaleX = -1f;
    private float lastSpeed = -1f;
    private float lastForceScale = -1f;
    private bool lastDirection;
    private float totalLocalWidth;
    private float xLeft, xRight, straightLength, loopLength;
    private float travelled;
    private readonly List<Transform> arrows = new List<Transform>();
    private readonly List<SpriteRenderer> arrowRenderers = new List<SpriteRenderer>();
    private Texture2D arrowTexture;
    private Sprite arrowSprite;

    private void OnEnable() { layoutDirty = true; }

    // Used by the menu builder so saved prefab geometry is correct before first Play.
    [ContextMenu("Refresh Conveyor Layout")]
    public void RefreshNow()
    {
        if (leftEnd == null || centerBelt == null || rightEnd == null) return;
        float worldScaleX = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        RefreshLayout(worldScaleX);
    }

    private void OnValidate()
    {
        lengthInTiles = Mathf.Clamp(lengthInTiles, 3, 12);
        tileWorldSize = Mathf.Max(0.01f, tileWorldSize);
        arrowsAtEightTiles = Mathf.Max(4, arrowsAtEightTiles);
        layoutDirty = true;
    }

    private void Update()
    {
        if (leftEnd == null || centerBelt == null || rightEnd == null) return;

        float scaleX = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
        if (layoutDirty || lastTiles != lengthInTiles ||
            !Mathf.Approximately(lastTileWidth, tileWorldSize) ||
            !Mathf.Approximately(lastWorldScaleX, scaleX) ||
            !Mathf.Approximately(lastSpeed, playerMoveSpeed) ||
            !Mathf.Approximately(lastForceScale, forceScale) ||
            lastDirection != moveRightOnTop)
        {
            RefreshLayout(scaleX);
        }

        if (!Application.isPlaying) return;
        if (arrowSprite == null) MakeArrowSprite();
        EnsureArrowCount();
        if (loopLength <= 0f) return;
        travelled = Mathf.Repeat(travelled + visualSpeed * (moveRightOnTop ? 1f : -1f) * Time.deltaTime, loopLength);
        UpdateArrowPositions();
    }

    private void RefreshLayout(float worldScaleX)
    {
        // All image components keep local Y scale 1: no vertical distortion.
        totalLocalWidth = lengthInTiles * tileWorldSize / worldScaleX;
        float leftWidth = leftEnd.sprite.rect.width / PixelsPerUnit;
        float rightWidth = rightEnd.sprite.rect.width / PixelsPerUnit;
        float middleWidth = Mathf.Max(0.02f, totalLocalWidth - leftWidth - rightWidth);

        leftEnd.transform.localPosition = new Vector3(-totalLocalWidth * 0.5f + leftWidth * 0.5f, 0f, 0f);
        rightEnd.transform.localPosition = new Vector3(totalLocalWidth * 0.5f - rightWidth * 0.5f, 0f, 0f);
        centerBelt.transform.localPosition = new Vector3((-totalLocalWidth + 2f * leftWidth + middleWidth) * 0.5f, 0f, 0f);
        centerBelt.drawMode = SpriteDrawMode.Tiled;
        centerBelt.tileMode = SpriteTileMode.Continuous;
        centerBelt.size = new Vector2(middleWidth, ArtworkHeight / PixelsPerUnit);

        if (surfaceCollider != null)
        {
            surfaceCollider.offset = new Vector2(0f, 0.5f);
            surfaceCollider.size = new Vector2(Mathf.Max(0.05f, totalLocalWidth - 0.62f), 0.18f);
            surfaceCollider.isTrigger = false;
            surfaceCollider.usedByEffector = true;
        }
        if (surfaceEffector != null)
        {
            surfaceEffector.speed = (moveRightOnTop ? 1f : -1f) * playerMoveSpeed;
            surfaceEffector.forceScale = forceScale;
        }

        if (braceRenderers != null)
        {
            // One decorative brace roughly every two world-space tiles in the middle.
            int visibleCount = Mathf.Clamp(
                Mathf.FloorToInt(middleWidth * worldScaleX / (2f * tileWorldSize)),
                0, braceRenderers.Length);
            float middleLeft = -totalLocalWidth * 0.5f + leftWidth;
            for (int i = 0; i < braceRenderers.Length; i++)
            {
                SpriteRenderer brace = braceRenderers[i];
                if (brace == null) continue;
                bool show = i < visibleCount;
                if (brace.gameObject.activeSelf != show) brace.gameObject.SetActive(show);
                if (show)
                {
                    float u = (i + 1f) / (visibleCount + 1f);
                    brace.transform.localPosition = new Vector3(middleLeft + middleWidth * u, 0f, -0.0002f);
                }
            }
        }

        // Original ConveyorArrowLoop's cog positions, now anchored to the fixed endcaps.
        xLeft = -totalLocalWidth * 0.5f + LeftCogPixelsFromEdge / PixelsPerUnit;
        xRight = totalLocalWidth * 0.5f - RightCogPixelsFromEdge / PixelsPerUnit;
        straightLength = Mathf.Max(0.01f, xRight - xLeft);
        loopLength = 2f * straightLength + 2f * Mathf.PI * EndRadius;

        lastTiles = lengthInTiles;
        lastTileWidth = tileWorldSize;
        lastWorldScaleX = worldScaleX;
        lastSpeed = playerMoveSpeed;
        lastForceScale = forceScale;
        lastDirection = moveRightOnTop;
        layoutDirty = false;
    }

    private void EnsureArrowCount()
    {
        int desired = Mathf.Max(4, Mathf.RoundToInt(arrowsAtEightTiles * lengthInTiles / 8f));
        while (arrows.Count > desired)
        {
            int last = arrows.Count - 1;
            Transform t = arrows[last];
            arrows.RemoveAt(last);
            arrowRenderers.RemoveAt(last);
            if (t != null) Destroy(t.gameObject);
        }
        while (arrows.Count < desired)
        {
            GameObject go = new GameObject("Arrow_" + (arrows.Count + 1).ToString("00"));
            go.transform.SetParent(transform, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = arrowSprite;
            sr.sortingLayerID = centerBelt.sortingLayerID;
            sr.sortingOrder = Mathf.Min(32767, centerBelt.sortingOrder + 3);
            arrows.Add(go.transform);
            arrowRenderers.Add(sr);
        }
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

            float bottomStart = straightLength + Mathf.PI * EndRadius;
            float bottomEnd = bottomStart + straightLength;
            float fadeDistance = Mathf.Max(0.001f, EndRadius * 0.35f);
            float visibility = 0f;
            if (distance <= straightLength)
                visibility = Mathf.Clamp01(Mathf.Min(distance, straightLength - distance) / fadeDistance);
            else if (distance >= bottomStart && distance <= bottomEnd)
                visibility = Mathf.Clamp01(Mathf.Min(distance - bottomStart, bottomEnd - distance) / fadeDistance);
            Color c = arrowColor;
            c.a *= visibility;
            arrowRenderers[i].color = c;
        }
    }

    private Vector2 PositionOnLoop(float d, out Vector2 tangent)
    {
        if (d < straightLength)
        {
            tangent = Vector2.right;
            return new Vector2(xLeft + d, CenterY + EndRadius);
        }
        d -= straightLength;
        float halfCircle = Mathf.PI * EndRadius;
        if (d < halfCircle)
        {
            float a = Mathf.PI * 0.5f - d / EndRadius;
            tangent = new Vector2(Mathf.Sin(a), -Mathf.Cos(a));
            return new Vector2(xRight + EndRadius * Mathf.Cos(a), CenterY + EndRadius * Mathf.Sin(a));
        }
        d -= halfCircle;
        if (d < straightLength)
        {
            tangent = Vector2.left;
            return new Vector2(xRight - d, CenterY - EndRadius);
        }
        d -= straightLength;
        float angle = -Mathf.PI * 0.5f - d / EndRadius;
        tangent = new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle));
        return new Vector2(xLeft + EndRadius * Mathf.Cos(angle), CenterY + EndRadius * Mathf.Sin(angle));
    }

    private void MakeArrowSprite()
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
                float alpha = Mathf.Clamp01((4.5f - dist) / 1.5f);
                pixels[y * size + x] = new Color(1f, 0.95f, 0.82f, alpha);
            }
        }
        arrowTexture.SetPixels(pixels);
        arrowTexture.Apply(false, false);
        arrowSprite = Sprite.Create(arrowTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), PixelsPerUnit);
    }

    private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float denom = Mathf.Max(0.000001f, ab.sqrMagnitude);
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / denom);
        return Vector2.Distance(p, a + ab * t);
    }

    private void OnDestroy()
    {
        if (arrowSprite != null) DestroyUnityObject(arrowSprite);
        if (arrowTexture != null) DestroyUnityObject(arrowTexture);
    }

    private static void DestroyUnityObject(Object obj)
    {
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }
}
