
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraTestInput : MonoBehaviour
{
    [SerializeField]
    private StageCameraController stageCamera;

    private void Update()
    {
        if (Keyboard.current == null) return;

        // c 키를 누르면 다음 구역으로 이동
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            stageCamera.MoveToNextArea();
        }
    }
}
