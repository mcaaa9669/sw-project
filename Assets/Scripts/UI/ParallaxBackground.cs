using UnityEngine;

[DefaultExecutionOrder(100)]
public class ParallaxBackground : MonoBehaviour
{
    [SerializeField] private Camera cam;

    [Tooltip("1 = 카메라에 완전히 붙음, 0 = 월드에 고정")]
    [SerializeField, Range(0f, 1f)] private float factorX = 0.9f;
    [SerializeField, Range(0f, 1f)] private float factorY = 1f;

    [Tooltip("화면 전체를 덮는 배경만 켜기. 작은 장식은 끄기")]
    [SerializeField] private bool clampToEdges = true;

    private Vector3 camStart, bgStart;
    private SpriteRenderer sr;

    private void Start()
    {
        if (cam == null) cam = Camera.main;
        sr = GetComponent<SpriteRenderer>();
        camStart = cam.transform.position;
        bgStart = transform.position;
    }

    private void LateUpdate()
    {
        Vector3 c = cam.transform.position;

        float x = bgStart.x + (c.x - camStart.x) * factorX;
        float y = bgStart.y + (c.y - camStart.y) * factorY;

        if (clampToEdges && sr != null)
        {
            float viewHalfW = cam.orthographicSize * cam.aspect;
            float limit = Mathf.Max(sr.bounds.extents.x - viewHalfW, 0f);
            x = Mathf.Clamp(x, c.x - limit, c.x + limit);
        }

        transform.position = new Vector3(x, y, transform.position.z);
    }
}