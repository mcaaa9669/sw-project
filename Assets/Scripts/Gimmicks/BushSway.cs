using UnityEngine;

// Attach to the LeavesPivot child object, not the BushWall root.
public class BushSway : MonoBehaviour
{
    [Header("Leaf sway (editable per wall)")]
    [Min(0f)] public float swayAngle = 1.5f;     // Max degrees left/right
    [Min(0f)] public float swaySpeed = 0.5f;     // Full sway cycles per second

    private Quaternion startRotation;
    private float phase;

    void Awake()
    {
        startRotation = transform.localRotation;
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        float degrees = Mathf.Sin(Time.time * swaySpeed * Mathf.PI * 2f + phase) * swayAngle;
        transform.localRotation = startRotation * Quaternion.Euler(0f, 0f, degrees);
    }
}
