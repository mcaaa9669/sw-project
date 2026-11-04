using UnityEngine;
using UnityEngine.Rendering;

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

    void Show()
    {
        volume.weight = 1f;
    }

    void Hide()
    {
        volume.weight = 0f;
    }
}