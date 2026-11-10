using UnityEngine;

[RequireComponent(typeof(SurfaceEffector2D))]
public class ConveyorOnReturn : MonoBehaviour
{
    private SurfaceEffector2D effector;
    private bool isRunning;

    private void Awake()
    {
        effector = GetComponent<SurfaceEffector2D>();
    }

    private void OnEnable()
    {
        effector.enabled = isRunning;
    }

    private void OnDisable()
    {
        if (effector != null)
            effector.enabled = false;
    }

    public void ToggleBelt()
    {
        isRunning = !isRunning;
        effector.enabled = isRunning;
    }
}
