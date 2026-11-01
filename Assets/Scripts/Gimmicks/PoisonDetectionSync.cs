using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(BoxCollider2D))]
public class PoisonDetectionSync : MonoBehaviour
{
    [SerializeField] private BoxCollider2D waterCollider;

    private BoxCollider2D detectionCollider;

    private void OnEnable()
    {
        SyncCollider();
    }

    private void LateUpdate()
    {
        SyncCollider();
    }

    private void SyncCollider()
    {
        if (detectionCollider == null)
            detectionCollider = GetComponent<BoxCollider2D>();

        if (waterCollider == null ||
            waterCollider == detectionCollider)
            return;

        if (detectionCollider.size != waterCollider.size)
            detectionCollider.size = waterCollider.size;

        if (detectionCollider.offset != waterCollider.offset)
            detectionCollider.offset = waterCollider.offset;
    }
}