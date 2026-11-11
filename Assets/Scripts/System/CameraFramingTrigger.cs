using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraFramingTrigger : MonoBehaviour
{
    [SerializeField] private PlayerCameraFollow followCamera;
    [SerializeField] private RespawnPoint requiredCheckpoint;
    [SerializeField, Range(0f, 1f)] private float playerViewportX = 0.2f;

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || followCamera == null || !player.IsCurrentRespawnPoint(requiredCheckpoint))
            return;

        followCamera.SetHorizontalFraming(player.transform, playerViewportX);
    }
}
