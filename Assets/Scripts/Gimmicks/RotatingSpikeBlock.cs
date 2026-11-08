using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RotatingSpikeBlock : MonoBehaviour, IWorldPausable
{
    [SerializeField, Min(1f)] float degreesPerSecond = 180f;

    Rigidbody2D body;
    float currentAngle;
    float targetAngle;
    bool paused;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        currentAngle = body.rotation;
        targetAngle = currentAngle;
    }

    public void TurnClockwise()
    {
        targetAngle -= 90f;
    }

    void FixedUpdate()
    {
        if (paused || currentAngle == targetAngle)
            return;

        currentAngle = Mathf.MoveTowards(
            currentAngle, targetAngle, degreesPerSecond * Time.fixedDeltaTime);

        body.MoveRotation(currentAngle);
    }

    public void SetWorldPaused(bool value)
    {
        paused = value;
    }
}