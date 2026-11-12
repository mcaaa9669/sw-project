using UnityEngine;

[ExecuteAlways]
public class BackgroundPlayerFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 offset;

    private void Update()
    {
        if (!Application.IsPlaying(gameObject))
            FollowTarget();
    }

    private void LateUpdate()
    {
        if (Application.IsPlaying(gameObject))
            FollowTarget();
    }

    private void FollowTarget()
    {
        if (target == null)
            return;

        Vector2 position = (Vector2)target.position + offset;
        Vector3 nextPosition = new Vector3(position.x, position.y, transform.position.z);
        if (transform.position != nextPosition)
            transform.position = nextPosition;
    }
}
