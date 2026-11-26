using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(SHELLVFXFramePlayer))]
public sealed class SHELLLandingGrass : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] blades = System.Array.Empty<SpriteRenderer>();

    private SHELLVFXFramePlayer animationPlayer;
    private Vector3[] origins;
    private Color[] colors;

    private void Start()
    {
        animationPlayer = GetComponent<SHELLVFXFramePlayer>();
        origins = new Vector3[blades.Length];
        colors = new Color[blades.Length];
        for (int i = 0; i < blades.Length; i++)
        {
            origins[i] = blades[i].transform.localPosition;
            colors[i] = blades[i].color;
        }
    }

    private void LateUpdate()
    {
        float progress = animationPlayer.Progress;
        for (int i = 0; i < blades.Length; i++)
        {
            Vector3 origin = origins[i];
            blades[i].transform.localPosition = origin + new Vector3(
                origin.x * progress * 2.5f, 0.24f * Mathf.Sin(progress * Mathf.PI), 0f);
            blades[i].transform.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Sign(origin.x) * (18f + progress * 65f));
            Color color = colors[i];
            color.a *= Mathf.Clamp01((1f - progress) * 2.2f);
            blades[i].color = color;
        }
    }
}
