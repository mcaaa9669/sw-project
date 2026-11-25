// Attach only to lever, moving platform and checkpoint objects.
// Call from genuine gameplay state changes, not generic collision/click events.
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SHELLSFX8EventBridge : MonoBehaviour
{
    public void LeverSwitchedOn() => SHELLSFX8.PlayLeverOn();
    public void LeverSwitchedOff() => SHELLSFX8.PlayLeverOff();
    public void PlatformStartedMoving() => SHELLSFX8.PlayPlatformMove();
    public void CheckpointReached() => SHELLSFX8.PlayCheckpoint();
    public void MenuSelectionChanged() => SHELLSFX8.PlayMenuHover();

    // Preserved for existing serialized UnityEvents; intentionally silent.
    public void LaserActivated() { }
}
