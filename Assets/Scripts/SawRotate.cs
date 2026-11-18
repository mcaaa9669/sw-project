using UnityEngine;

public class SawRotate : MonoBehaviour, IWorldPausable
{
    // 회전 속도 (초당 각도)
    public float rotationSpeed = 120f;

    // 체크하면 시계방향, 해제하면 반시계방향
    public bool clockwise = false;

    bool paused;    // 영혼세계에서 현실세계와 함께 멈춤

    void Update()
    {
        if (paused) return;

        float direction = clockwise ? -1f : 1f;

        transform.Rotate(
            0f,
            0f,
            rotationSpeed * direction * Time.deltaTime
        );
    }

    public void SetWorldPaused(bool value)
    {
        paused = value;
    }
}
