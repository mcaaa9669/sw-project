
using UnityEngine;

public class SawRotate : MonoBehaviour
{
    // 회전 속도 (초당 각도)
    public float rotationSpeed = 120f;

    // 체크하면 시계방향, 해제하면 반시계방향
    public bool clockwise = false;

    void Update()
    {
        float direction = clockwise ? -1f : 1f;

        transform.Rotate(
            0f,
            0f,
            rotationSpeed * direction * Time.deltaTime
        );
    }
}
