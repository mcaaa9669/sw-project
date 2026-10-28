
using UnityEngine;
using UnityEngine.InputSystem;

public class SoulWorldTestController : MonoBehaviour
{
    [SerializeField] private SoulWorldController soulWorld;

    private void OnEnable()
    {
        if (soulWorld != null)
            soulWorld.OnReturnRequested += HandleReturn;
    }

    private void OnDisable()
    {
        if (soulWorld != null)
            soulWorld.OnReturnRequested -= HandleReturn;
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.yKey.wasPressedThisFrame)
        {
            soulWorld.EnterSoulWorld();
        }
    }

    private void HandleReturn()
    {
        Debug.Log("현실세계 복귀 요청 발생!");
    }
}
