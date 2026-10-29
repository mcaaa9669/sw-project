
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraTransitionTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StageCameraController stageCamera;
    [SerializeField] private Transform cameraTarget;

    [Header("Transition State")]
    [SerializeField] private bool unlocked = false;

    private bool used = false;
    private bool playerInside = false;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    // 퍼즐 클리어 시 외부에서 호출
    public void UnlockTransition()
    {
        unlocked = true;
        TryTransition();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        playerInside = true;
        TryTransition();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInside = false;
    }

    private void TryTransition()
    {
        if (!unlocked || used || !playerInside)
            return;

        if (stageCamera == null || cameraTarget == null)
            return;

        used = true;
        stageCamera.MoveToStageCenter(cameraTarget.position);
    }
}
