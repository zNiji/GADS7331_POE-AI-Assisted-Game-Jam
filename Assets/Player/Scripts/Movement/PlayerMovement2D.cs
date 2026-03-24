using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerMovement2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float deceleration = 16f;
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private float moveInputRaw;
    private bool jumpPressed;
    private bool isGrounded;
    private float currentHorizontalSpeed;

    public float HorizontalInput => moveInputRaw;
    public bool IsGrounded => isGrounded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void Update()
    {
        moveInputRaw = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(moveInputRaw) < 0.01f)
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                moveInputRaw = -1f;
            }
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                moveInputRaw = 1f;
            }
        }

        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space))
        {
            jumpPressed = true;
        }

        HandleSpriteFlip();
    }

    private void FixedUpdate()
    {
        CheckGrounded();
        HandleHorizontalMovement();
        HandleJump();
        jumpPressed = false;
    }

    private void HandleHorizontalMovement()
    {
        float targetSpeed = moveInputRaw * moveSpeed;
        float speedChangeRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;

        currentHorizontalSpeed = Mathf.MoveTowards(
            rb.linearVelocity.x,
            targetSpeed,
            speedChangeRate * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector2(currentHorizontalSpeed, rb.linearVelocity.y);
    }

    private void HandleJump()
    {
        if (!jumpPressed || !isGrounded)
        {
            return;
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    private void HandleSpriteFlip()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (moveInputRaw > 0.01f)
        {
            spriteRenderer.flipX = false;
        }
        else if (moveInputRaw < -0.01f)
        {
            spriteRenderer.flipX = true;
        }
    }

    private void CheckGrounded()
    {
        if (groundCheck == null)
        {
            isGrounded = false;
            return;
        }

        if (groundLayer.value == 0)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius) != null;
            return;
        }

        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}
