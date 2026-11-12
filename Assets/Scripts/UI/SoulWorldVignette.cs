using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Volume))]
public class SoulWorldVignette : MonoBehaviour
{
    [SerializeField] SoulWorldController soulWorldController;

    Volume volume;

    void Awake()
    {
        volume = GetComponent<Volume>();
    }

    void OnEnable()
    {
        if (soulWorldController == null)
            soulWorldController =
                FindFirstObjectByType<SoulWorldController>();

        if (soulWorldController == null)
        {
            volume.weight = 0f;
            Debug.LogWarning(
                "[영혼 비네팅] SoulWorldController 연결 필요",
                this
            );
            return;
        }

        soulWorldController.OnSoulWorldEntered += Show;
        soulWorldController.OnReturnRequested += Hide;

        volume.weight = soulWorldController.IsSoulWorld ? 1f : 0f;
    }

    void OnDisable()
    {
        if (soulWorldController != null)
        {
            soulWorldController.OnSoulWorldEntered -= Show;
            soulWorldController.OnReturnRequested -= Hide;
        }

        if (volume != null)
            volume.weight = 0f;
    }

    public void SetMaximumStrength(bool maximum)
    {
        if (volume.sharedProfile == null || !volume.sharedProfile.TryGet(out Vignette defaults) ||
            !volume.profile.TryGet(out Vignette vignette))
            return;

        vignette.intensity.value = maximum ? 1f : defaults.intensity.value;
        vignette.smoothness.value = maximum ? 1f : defaults.smoothness.value;
    }

    void Show()
    {
        volume.weight = 1f;
    }

    void Hide()
    {
        volume.weight = 0f;
    }
}