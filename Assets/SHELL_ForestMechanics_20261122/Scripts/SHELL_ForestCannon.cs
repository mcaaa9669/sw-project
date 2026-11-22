using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SHELL forest cannon V3: full, reliable parabolic flight from launch to descent.
/// Temporarily owns the player's Rigidbody2D trajectory; restores native gravity afterwards.
/// </summary>
[DefaultExecutionOrder(20000)]
[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class SHELL_ForestCannon : MonoBehaviour
{
    [Header("Launch (automatic when touched)")]
    [Range(2f, 30f)] public float launchSpeed = 9.2f;
    [Range(10f, 85f)] public float launchAngle = 48f;
    public bool fireToRight = true;
    [Min(0.1f)] public float reentryCooldown = 1.4f;

    [Header("Full arc (independent of player gravity)")]
    [Tooltip("Effective gravity during the cannon flight. Larger values make a shorter arc.")]
    [Range(4f, 20f)] public float arcGravity = 9.81f;

    [Header("Compatibility with SHELL player")]
    [Tooltip("OFF recommended: launches any dynamic Rigidbody2D. No Player tag needed.")]
    public bool onlyPlayer = false;
    public string playerTag = "Player";
    [Tooltip("Minimum duration for flight control. A complete up-and-down arc is always enforced even if this value is smaller.")]
    [Range(0.1f, 1.5f)] public float launchAssistSeconds = 0.72f;
    [Tooltip("Safety scan also works when Unity layer collision rules prevent trigger callbacks.")]
    public bool useProximityBackup = true;

    [Header("Optional sound")]
    public AudioSource audioSource;
    public AudioClip launchSound;

    [HideInInspector] public int setupVersion = 0;

    private sealed class Flight
    {
        public Rigidbody2D body;
        public Vector2 initialVelocity;
        public float started;
        public float ends;
        public float gravity;
        public float originalGravityScale;
        public bool kinematic;
    }

    private readonly Dictionary<int, float> nextAllowedTime = new Dictionary<int, float>();
    private readonly Dictionary<int, Flight> flights = new Dictionary<int, Flight>();
    private readonly List<int> finished = new List<int>();
    private CircleCollider2D detection;
    private float nextScan;

    private void Awake()
    {
        detection = GetComponent<CircleCollider2D>();
        detection.isTrigger = true;
        // The sprite is about 2 units wide; avoid an invisible dead zone at the rim.
        detection.radius = Mathf.Max(detection.radius, 1.04f);
        var body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.simulated = true;
        body.gravityScale = 0f;
    }

    private void Reset()
    {
        var zone = GetComponent<CircleCollider2D>();
        zone.isTrigger = true;
        zone.radius = 1.04f;
        zone.offset = new Vector2(-0.02f, -0.06f);
        var body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
    }

    private void OnTriggerEnter2D(Collider2D other) { TryLaunch(other); }
    private void OnTriggerStay2D(Collider2D other) { TryLaunch(other); }
    private void OnCollisionEnter2D(Collision2D contact)
    {
        if (contact != null) TryLaunch(contact.collider);
    }
    private void OnCollisionStay2D(Collision2D contact)
    {
        if (contact != null) TryLaunch(contact.collider);
    }

    private static bool IsNamedPlayer(Transform t)
    {
        if (t == null) return false;
        string s = t.name.ToLowerInvariant();
        return s.Contains("player") || s.Contains("cat") || s.Contains("hero") || s.Contains("character");
    }

    private bool IsKnownPlayer(Collider2D other, Rigidbody2D rb)
    {
        Transform t = other.transform;
        while (t != null)
        {
            if (!string.IsNullOrEmpty(playerTag) && t.gameObject.tag == playerTag) return true;
            if (IsNamedPlayer(t)) return true;
            t = t.parent;
        }
        if (IsNamedPlayer(rb.transform)) return true;
        var scripts = rb.GetComponentsInChildren<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script == null) continue;
            string name = script.GetType().Name.ToLowerInvariant();
            if (name.Contains("playercontroller") || name.Contains("playermovement") ||
                name.Contains("catcontroller") || name.Contains("charactermotor")) return true;
        }
        return false;
    }

    private void TryLaunch(Collider2D other)
    {
        if (!isActiveAndEnabled || other == null || !other.enabled) return;
        Rigidbody2D rb = other.attachedRigidbody;
        if (rb == null) rb = other.GetComponentInParent<Rigidbody2D>();
        if (rb == null || rb.gameObject == gameObject || !rb.simulated ||
            rb.bodyType == RigidbodyType2D.Static) return;

        bool player = IsKnownPlayer(other, rb);
        if (onlyPlayer && !player) return;
        // Never launch unrelated moving platforms if onlyPlayer is OFF.
        if (rb.bodyType == RigidbodyType2D.Kinematic && !player) return;

        int id = rb.GetInstanceID();
        if (flights.ContainsKey(id)) return;
        float readyAt;
        if (nextAllowedTime.TryGetValue(id, out readyAt) && Time.time < readyAt) return;
        float radians = launchAngle * Mathf.Deg2Rad;
        float direction = fireToRight ? 1f : -1f;
        Vector2 velocity = new Vector2(direction * Mathf.Cos(radians), Mathf.Sin(radians)) * launchSpeed;
        float effectiveGravity = arcGravity > 0.01f ? arcGravity : 9.81f;
        // Flight duration from v_y(t) = v_y(0) - g*t. Uses both rising and falling halves.
        float completeArcSeconds = 2f * velocity.y / effectiveGravity;
        float duration = Mathf.Max(completeArcSeconds, launchAssistSeconds);
        nextAllowedTime[id] = Time.time + Mathf.Max(reentryCooldown, duration + 0.25f);
        flights[id] = new Flight
        {
            body = rb,
            initialVelocity = velocity,
            started = Time.fixedTime,
            ends = Time.fixedTime + duration,
            gravity = effectiveGravity,
            originalGravityScale = rb.gravityScale,
            kinematic = rb.bodyType == RigidbodyType2D.Kinematic
        };

        if (rb.bodyType == RigidbodyType2D.Dynamic)
        {
            // Temporarily take over trajectory so the player's gravity and movement
            // scripts do not collapse the arc halfway through.
            rb.gravityScale = 0f;
            SetVelocity(rb, velocity);
            rb.WakeUp();
        }
        if (audioSource != null && launchSound != null)
            audioSource.PlayOneShot(launchSound);
        Debug.Log("SHELL Cannon V3 full arc: " + rb.name +
                  " | flight " + duration.ToString("F2") + " s | range " +
                  (Mathf.Abs(velocity.x) * duration).ToString("F2") + " units", this);
    }

    private static void SetVelocity(Rigidbody2D body, Vector2 v)
    {
#if UNITY_6000_0_OR_NEWER
        body.linearVelocity = v;
#else
        body.velocity = v;
#endif
    }
    private static void RestoreFlight(Flight flight)
    {
        if (flight == null || flight.body == null) return;
        if (!flight.kinematic && flight.body.bodyType == RigidbodyType2D.Dynamic)
            flight.body.gravityScale = flight.originalGravityScale;
    }

    private void FixedUpdate()
    {
        // Backup scan: handles player sub-colliders and trigger collision-layer mismatches.
        if (useProximityBackup && Time.time >= nextScan)
        {
            nextScan = Time.time + 0.08f;
            if (detection == null) detection = GetComponent<CircleCollider2D>();
            Vector3 center3 = transform.TransformPoint(detection.offset);
            Vector3 scale = transform.lossyScale;
            float radius = detection.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            var nearby = Physics2D.OverlapCircleAll(new Vector2(center3.x, center3.y), radius);
            foreach (var hit in nearby) TryLaunch(hit);
        }

        finished.Clear();
        foreach (var entry in flights)
        {
            Flight flight = entry.Value;
            if (flight.body == null || !flight.body.simulated ||
                Time.fixedTime >= flight.ends)
            {
                finished.Add(entry.Key);
                continue;
            }

            float elapsed = Mathf.Max(0f, Time.fixedTime - flight.started);
            // Crucial V2 bug fix: vertical velocity MUST turn negative after the apex.
            // Using Mathf.Max(currentY, expectedY) incorrectly suppressed descent.
            float verticalSpeed = flight.initialVelocity.y - flight.gravity * elapsed;
            Vector2 idealVelocity = new Vector2(flight.initialVelocity.x, verticalSpeed);

            if (flight.kinematic)
            {
                flight.body.MovePosition(flight.body.position + idealVelocity * Time.fixedDeltaTime);
            }
            else if (flight.body.bodyType == RigidbodyType2D.Dynamic)
            {
                // Applied after ordinary PlayerController.FixedUpdate through the
                // execution order attribute. Restore normal gravity on completion.
                flight.body.gravityScale = 0f;
                SetVelocity(flight.body, idealVelocity);
            }
            else
            {
                finished.Add(entry.Key);
            }
        }
        foreach (int id in finished)
        {
            Flight flight;
            if (flights.TryGetValue(id, out flight)) RestoreFlight(flight);
            flights.Remove(id);
        }
    }

    private void OnDisable()
    {
        // Never leave the player's gravity disabled if this cannon is removed mid-flight.
        foreach (var entry in flights) RestoreFlight(entry.Value);
        flights.Clear();
    }

    private void OnDestroy()
    {
        foreach (var entry in flights) RestoreFlight(entry.Value);
        flights.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, .85f, .23f, .9f);
        float angle = launchAngle * Mathf.Deg2Rad;
        Vector3 dir = new Vector3((fireToRight ? 1f : -1f) * Mathf.Cos(angle), Mathf.Sin(angle), 0f);
        float g = arcGravity > .01f ? arcGravity : 9.81f;
        float speed = launchSpeed;
        float flightTime = 2f * speed * Mathf.Sin(angle) / g;
        Vector3 previous = transform.position;
        for (int i = 1; i <= 32; i++)
        {
            float t = flightTime * i / 32f;
            Vector3 next = transform.position +
                new Vector3(dir.x * speed * t, speed * Mathf.Sin(angle) * t - .5f * g * t * t, 0f);
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
        Gizmos.DrawSphere(previous, .065f);
    }
}
