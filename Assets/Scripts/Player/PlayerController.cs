using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour,IKillable
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float jumpForce = 5f;
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundRadius = 0.1f;
    [SerializeField] LayerMask groundLayer;

    [Header("Death")]
    [SerializeField] Shell shellPrefab;
    [SerializeField] float poisonDelay = 3f;   


    [Header("Respawn")]
    [SerializeField] RespawnPoint respawnPoint;                // ���� ��Ȱ ����. ���� ���� ��ġ���� ��Ȱ
    [SerializeField] float respawnDelay = 1f;                  // ��� �� ��Ȱ���� �ɸ��� �ð�

    [Header("Soul World (���� ��� �� ��)")]
    [SerializeField] bool useSoulWorld = false;                // �Ѹ� ��� �� ��ȥ����� ��ȯ
    [SerializeField] SoulWorldController soulWorldController;  // ��� ��ȣ�� ���� ��

    Rigidbody2D rb;
    float inputX;
    bool jumpRequested;
    SpriteRenderer sr;
    Collider2D col;

    bool isDead;
    Coroutine poisonRoutine;
    Vector3 startPosition;

    public bool IsDead => isDead;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        startPosition = transform.position;

        if (useSoulWorld&&soulWorldController == null)
            soulWorldController = FindFirstObjectByType<SoulWorldController>();
    }

    void OnEnable()
    {
        if (useSoulWorld && soulWorldController != null)
            soulWorldController.OnReturnRequested += HandleReturn;
    }

    void OnDisable()
    {
        if (soulWorldController != null)
            soulWorldController.OnReturnRequested -= HandleReturn;
    }

    void Update()
    {
        if (isDead) return;
        if (Time.timeScale == 0f) return;
        var kb = Keyboard.current;
        if (kb == null) return;
        inputX = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
        if (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) jumpRequested = true;
    }

    void FixedUpdate()
    {
        if (isDead) return;

        bool grounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
        rb.linearVelocity = new Vector2(inputX * moveSpeed, rb.linearVelocity.y);

        if (jumpRequested && grounded)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpRequested = false;
    }

    public void Kill(DeathType deathType)
    {
        if (isDead) return;

        switch (deathType)
        {
            case DeathType.Spike:
                Die();
                SpawnShell().OnSpikeDeath();
                break;

            case DeathType.laser:
                Die();
                SpawnShell().OnLaserDeath();
                break;

            case DeathType.poison:
                if (poisonRoutine == null)
                    poisonRoutine = StartCoroutine(PoisonRoutine());
                break;

            case DeathType.Fall:
                Die();
                break;

            default:
                Debug.LogWarning($"[PlayerController] ���� ó������ ���� ��� ����: {deathType}");
                break;
        }
    }


    IEnumerator PoisonRoutine()
    {
        yield return new WaitForSeconds(poisonDelay);
        poisonRoutine = null;

        if (isDead) yield break;
        Die();
        SpawnShell().OnPoisonDeath();
    }

    void Die()
    {
        isDead = true;
        if (poisonRoutine != null)
        {
            StopCoroutine(poisonRoutine);
            poisonRoutine = null;
        }

        inputX = 0f;
        jumpRequested = false;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        if (col) col.enabled = false;
        if (sr) sr.enabled = false;

        if (useSoulWorld && soulWorldController != null)
        {
            soulWorldController.HandlePlayerDeath();
        }
        else
        {
            StartCoroutine(RespawnAfterDelay());
        }
    }


    void HandleReturn()
    {
        if (!isDead) return;
        Revive(respawnPoint != null ? respawnPoint.transform.position : startPosition);
    }

    
    public bool SetRespawnPoint(RespawnPoint point)
    {
        if (point == null || point == respawnPoint) return false;
        if (respawnPoint != null && point.Order < respawnPoint.Order) return false;

        respawnPoint = point;
        return true;
    }

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        HandleReturn();
    }

    Shell SpawnShell()
    {
        return Instantiate(shellPrefab, transform.position, Quaternion.identity);
    }


    public void Revive(Vector3 position)
    {
        transform.position = position;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = true;
        if (col) col.enabled = true;
        if (sr) sr.enabled = true;
        isDead = false;
    }
}