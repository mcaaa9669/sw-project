using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class Lever : MonoBehaviour
{
    static readonly HashSet<Lever> toggleLevers = new HashSet<Lever>();
    static int lastToggleFrame = -1;

    [SerializeField] SpriteRenderer targetRenderer;
    [SerializeField] Sprite offSprite;
    [SerializeField] Sprite onSprite;
    [SerializeField] bool soulOnly;
    [SerializeField] bool toggleWithT;

    public UnityEvent onActivated;
    public UnityEvent onDeactivated;

    readonly List<Collider2D> nearbyColliders = new List<Collider2D>();
    Collider2D interactionArea;

    public bool IsPulled { get; private set; }

    void Awake()
    {
        interactionArea = GetComponent<Collider2D>();
        interactionArea.isTrigger = true;

        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<SpriteRenderer>();

        UpdateSprite();
    }

    void OnEnable()
    {
        if (toggleWithT)
            toggleLevers.Add(this);
    }

    void OnDisable()
    {
        toggleLevers.Remove(this);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (toggleWithT || IsPulled)
            return;

        if (CanInteract(other.GetComponentInParent<PlayerController>()))
            SetPulled(true);
    }

    void Update()
    {
        if (toggleWithT && Keyboard.current?.tKey.wasPressedThisFrame == true)
            TryToggleNearby();
    }

    static bool TryToggleNearby()
    {
        if (Time.timeScale <= 0f)
            return false;
        if (lastToggleFrame == Time.frameCount)
            return true;

        Lever nearest = null;
        float nearestDistance = float.PositiveInfinity;
        var filter = new ContactFilter2D { useTriggers = true };

        foreach (Lever lever in toggleLevers)
        {
            if (!lever.interactionArea.enabled)
                continue;

            lever.interactionArea.Overlap(filter, lever.nearbyColliders);
            foreach (Collider2D other in lever.nearbyColliders)
            {
                var player = other.GetComponentInParent<PlayerController>();
                if (!lever.CanInteract(player))
                    continue;

                float distance = (player.transform.position - lever.transform.position).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = lever;
                    nearestDistance = distance;
                }
            }
        }

        if (nearest == null)
            return false;

        lastToggleFrame = Time.frameCount;
        nearest.SetPulled(!nearest.IsPulled);
        return true;
    }

    bool CanInteract(PlayerController player)
    {
        return player != null &&
            (soulOnly ? player.IsSoul : (!player.IsDead || player.IsSoul));
    }

    void SetPulled(bool pulled)
    {
        IsPulled = pulled;
        UpdateSprite();
        if (pulled)
            onActivated?.Invoke();
        else
            onDeactivated?.Invoke();
    }

    void UpdateSprite()
    {
        if (targetRenderer == null)
            return;

        Sprite sprite = IsPulled ? onSprite : offSprite;
        if (sprite != null)
            targetRenderer.sprite = sprite;
    }
}
