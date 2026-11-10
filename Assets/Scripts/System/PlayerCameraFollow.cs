using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PlayerCameraFollow : MonoBehaviour
{
    private const float ViewportShiftSpeed = 0.6f;

    [SerializeField] private Transform target;

    private Camera viewCamera;
    private float viewportX = 0.5f;
    private float desiredViewportX = 0.5f;

    private void Awake()
    {
        viewCamera = GetComponent<Camera>();
    }

    public void SetHorizontalFraming(Transform player, float playerViewportX)
    {
        if (player != target) return;

        desiredViewportX = Mathf.Clamp01(playerViewportX);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        viewportX = Mathf.MoveTowards(viewportX, desiredViewportX, ViewportShiftSpeed * Time.deltaTime);
        float viewWidth = 2f * viewCamera.orthographicSize * viewCamera.aspect;

        Vector3 position = target.position;
        position.x += (0.5f - viewportX) * viewWidth;
        position.z = transform.position.z;
        transform.position = position;
    }
}
