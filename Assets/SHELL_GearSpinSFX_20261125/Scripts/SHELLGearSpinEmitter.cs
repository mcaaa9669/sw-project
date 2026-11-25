// Monitors actual visual rotation; independent of the game's original gear logic.
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SHELLGearSpinEmitter : MonoBehaviour
{
    AudioSource loop;
    SpriteRenderer sprite;
    Sprite lastSprite;
    float lastWorldAngle;
    float lastVisualMotion;

    void Awake()
    {
        // Separate source: never overwrite any AudioSource owned by a teammate.
        loop = gameObject.AddComponent<AudioSource>();
        loop.clip = Resources.Load<AudioClip>("SHELL_GearSpin/13_saw_spin_LOOP");
        loop.playOnAwake = false;
        loop.loop = true;
        loop.spatialBlend = 0f; // Reliable 2D playback with the project's 2D camera.
        loop.volume = 0f;
        sprite = GetComponent<SpriteRenderer>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        lastSprite = sprite != null ? sprite.sprite : null;
        lastWorldAngle = transform.eulerAngles.z;
    }

    void OnEnable()
    {
        lastWorldAngle = transform.eulerAngles.z;
        lastVisualMotion = -100f;
    }

    void LateUpdate()
    {
        if (loop == null || loop.clip == null) return;
        float newAngle = transform.eulerAngles.z;
        // Read the real rotation AFTER gameplay scripts ran, not just whether the object is enabled.
        bool rotated = Mathf.Abs(Mathf.DeltaAngle(lastWorldAngle, newAngle)) > 0.015f;
        lastWorldAngle = newAngle;

        // Also support replacement gears animated as successive sprite frames.
        if (sprite != null && sprite.sprite != lastSprite)
        {
            lastSprite = sprite.sprite;
            lastVisualMotion = Time.unscaledTime;
        }
        bool animated = sprite != null && Time.unscaledTime - lastVisualMotion < 0.35f;
        bool spinning = isActiveAndEnabled && gameObject.activeInHierarchy &&
                        Time.timeScale > 0f && (rotated || animated);

        if (spinning && !loop.isPlaying) loop.Play();
        float volume = spinning ? 0.22f : 0f;
        loop.volume = Mathf.MoveTowards(loop.volume, volume, Time.unscaledDeltaTime * 1.2f);
        if (!spinning && loop.isPlaying && loop.volume <= 0.001f) loop.Stop();
    }

    void OnDisable()
    {
        if (loop != null) { loop.Stop(); loop.volume = 0f; }
    }
}
