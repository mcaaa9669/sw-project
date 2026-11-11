using UnityEngine;

public class DrainPoisonOnReturn : MonoBehaviour
{
    private bool drainRequested;

    private void OnEnable()
    {
        WorldStateManager.OnWorldChanged += HandleWorldChanged;
    }

    private void OnDisable()
    {
        WorldStateManager.OnWorldChanged -= HandleWorldChanged;
    }

    public void RequestDrain()
    {
        drainRequested = true;
    }

    private void HandleWorldChanged(WorldType world)
    {
        if (world == WorldType.Real && drainRequested)
            gameObject.SetActive(false);
    }
}
