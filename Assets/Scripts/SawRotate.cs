
using UnityEngine;

public class SawRotate : MonoBehaviour
{
    // 톱니바퀴 회전 속도 (초당 120도)
    public float rotationSpeed = 120f;

    void Update()
    {
        // Z축을 기준으로 계속 회전
        transform.Rotate(
            0f,
            0f,
            rotationSpeed * Time.deltaTime
        );
    }
}
