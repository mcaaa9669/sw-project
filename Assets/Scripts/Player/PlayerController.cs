using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float jumpForce = 5f;
    [SerializeField] Transform groundCheck;      
    [SerializeField] float groundRadius = 0.1f;
    [SerializeField] LayerMask groundLayer;

    Rigidbody2D rb;
    float inputX;
    bool jumpRequested;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        inputX = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0);
        if (kb.spaceKey.wasPressedThisFrame) jumpRequested = true;
    }

    void FixedUpdate()
    {
        bool grounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
        rb.linearVelocity = new Vector2(inputX * moveSpeed, rb.linearVelocity.y);

        if (jumpRequested && grounded)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpRequested = false;
    }
}