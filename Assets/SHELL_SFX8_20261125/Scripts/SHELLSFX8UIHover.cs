// Add to real UI Button GameObjects. Works for pointer and keyboard/controller selection.
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class SHELLSFX8UIHover : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    private static float lastPlay = -10f;
    public void OnPointerEnter(PointerEventData eventData) { Play(); }
    public void OnSelect(BaseEventData eventData) { Play(); }
    private static void Play()
    {
        if (Time.unscaledTime - lastPlay < 0.10f) return;
        lastPlay = Time.unscaledTime;
        SHELLSFX8.PlayMenuHover();
    }
}
