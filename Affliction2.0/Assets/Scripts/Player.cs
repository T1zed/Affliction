using UnityEngine.InputSystem;
using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    private PlayerAction playerAction;
    private InputAction moveAction;
    private InputAction dashAction;
    private InputAction jumpAction;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Dash")]
    public float dashCooldown = 0.3f;
    public float dashLength = 5f;
    public float dashDuration = 0.2f;
    private float dashSpeed => dashLength / dashDuration;
    private bool isDashing = false;
    private float lastDashTime;

    [Header("Wall")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private float wallCheckDistance = 0.6f;

    private bool isWallSliding = false;
    [SerializeField] private float wallSlideDelay = 2f;
    [SerializeField] private float wallSlideSpeed = 2f;

    private Rigidbody2D rb;
    private Vector2 moveInput;

    void Awake()
    {
        playerAction = new PlayerAction();
    }

    private void Start()
    {

        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        playerAction.Player.Enable();
        moveAction = playerAction.Player.Move;
        dashAction = playerAction.Player.Dash;
        jumpAction = playerAction.Player.Jump;

        jumpAction.performed += OnJump;
        dashAction.performed += OnDash;
    }

    private void OnDisable()
    {
        playerAction.Player.Disable();
        jumpAction.performed -= OnJump;
        dashAction.performed -= OnDash;
    }

    void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        Debug.Log(IsGrounded());
    }

    void FixedUpdate()
    {
        if (!isDashing)
        {

            Vector2 velocity =  rb.linearVelocity ;
            velocity.x = moveInput.x * moveSpeed;
             rb.linearVelocity  = velocity;
        }

        if (!IsGrounded() && !isWallSliding && !isDashing && IsTouchingWall(out _))
        {
            StartCoroutine(WallSlideCoroutine());
        }
    }

    private bool IsGrounded()
    {

        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private bool IsTouchingWall(out Vector2 wallNormal)
    {

        RaycastHit2D hitRight = Physics2D.Raycast(transform.position, Vector2.right, wallCheckDistance, wallLayer);
        if (hitRight.collider != null)
        {
            wallNormal = hitRight.normal;
            return true;
        }

        RaycastHit2D hitLeft = Physics2D.Raycast(transform.position, Vector2.left, wallCheckDistance, wallLayer);
        if (hitLeft.collider != null)
        {
            wallNormal = hitLeft.normal;
            return true;
        }

        wallNormal = Vector2.zero;
        return false;
    }
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (!IsGrounded()) return;

     
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
  
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }
    IEnumerator DashCoroutine()
    {
        isDashing = true;

        Vector2 input = moveInput.normalized;

        Vector2 dir = input != Vector2.zero
            ? input
            : new Vector2(transform.localScale.x > 0 ? 1f : -1f, 0f);

   
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    
        rb.gravityScale = 0f;
         rb.linearVelocity  = dir * dashSpeed;

        float timer = 0f;
        while (timer < dashDuration)
        {
            if (!isDashing) yield break;

             rb.linearVelocity  = dir * dashSpeed;
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

         rb.linearVelocity  = Vector2.zero;
        rb.gravityScale = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
        isDashing = false;
    }

    IEnumerator WallSlideCoroutine()
    {
        isWallSliding = true;
        rb.gravityScale = 0f;
         rb.linearVelocity  = Vector2.zero;

        float timer = 0f;
        while (timer < wallSlideDelay)
        {
            if (isDashing || IsGrounded() || !IsTouchingWall(out _))
            {
                ExitWallSlide();
                yield break;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        rb.gravityScale = 0f;
        while (IsTouchingWall(out _) && !IsGrounded())
        {
            if (isDashing || !IsTouchingWall(out _))
            {
                ExitWallSlide();
                yield break;
            }

             rb.linearVelocity  = Vector2.down * wallSlideSpeed;
            yield return new WaitForFixedUpdate();
        }

        ExitWallSlide();
    }


    private Coroutine dashCoroutine;

    private void OnDash(InputAction.CallbackContext ctx)
    {
        if (!isDashing)
            dashCoroutine = StartCoroutine(DashCoroutine());
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDashing && dashCoroutine != null)
        {
            StopCoroutine(dashCoroutine); 
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 1f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
            isDashing = false;
        }
    }

    private void ExitWallSlide()
    {
        rb.gravityScale = 1f;
         rb.linearVelocity  = Vector2.zero;
        isWallSliding = false;
    }
}