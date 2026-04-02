using UnityEngine.InputSystem;
using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    private PlayerAction playerAction;
    private InputAction moveAction;
    private InputAction dashAction;
    private InputAction jumpAction;
    private InputAction AttackAction;
    private InputAction SecondaryAttackAction;
    [Header("direction")]
    public bool right;
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
    private ComboComponent comboComponent;
    private AttackComponent attackComponent;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    [Header("context")]
    public bool grounded;
    public bool onWall;
    void Awake()
    {
        playerAction = new PlayerAction();
    }

    private void Start()
    {

        rb = GetComponent<Rigidbody2D>();
        comboComponent = GetComponent<ComboComponent>();
        attackComponent = GetComponent<AttackComponent>();

    }

    private void OnEnable()
    {
        playerAction.Player.Enable();
        moveAction = playerAction.Player.Move;
        dashAction = playerAction.Player.Dash;
        jumpAction = playerAction.Player.Jump;
        AttackAction=playerAction.Player.Attack;
        SecondaryAttackAction=playerAction.Player.SecondaryAttack;

        jumpAction.performed += OnJump;
        dashAction.performed += OnDash;
        AttackAction.performed += OnAttack;
        SecondaryAttackAction.performed += OnSecondaryAttack;
    }

    private void OnDisable()
    {
        playerAction.Player.Disable();
        jumpAction.performed -= OnJump;
        dashAction.performed -= OnDash;
        AttackAction.performed -= OnAttack;
        SecondaryAttackAction.performed -= OnSecondaryAttack;
    }
    private bool isMovementLocked = false;

    public void SetMovementLocked(bool locked)
    {
        isMovementLocked = locked;
        if (locked)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        else
        {

            wasRight = false;
            wasLeft = false;
        }
    }

    private bool wasRight;
    private bool wasLeft;

    void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();

        bool isPressingRight = moveInput.x > 0;
        bool isPressingLeft = moveInput.x < 0;

        if (!isMovementLocked)
        {
            if (isPressingRight && !wasRight)
            {
                attackComponent?.RegisterDirectInput(3);
                right = true;
                transform.localScale = new Vector3(1f, transform.localScale.y, transform.localScale.z);
            }
            else if (isPressingLeft && !wasLeft)
            {
                attackComponent?.RegisterDirectInput(2);
                right = false;
                transform.localScale = new Vector3(-1f, transform.localScale.y, transform.localScale.z);
            }
        }

        wasRight = isPressingRight;
        wasLeft = isPressingLeft;
    }


    void FixedUpdate()
    {
        if (!isDashing)
        {
            Vector2 velocity = rb.linearVelocity;
            velocity.x = isMovementLocked ? 0f : moveInput.x * moveSpeed; 
            rb.linearVelocity = velocity;
        }

        if (!IsGrounded() && !isWallSliding && !isDashing && IsTouchingWall(out _))
        {
            StartCoroutine(WallSlideCoroutine());
        }
    }

    public bool IsGrounded()
    {

        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    public bool IsOnWall()
    {
        return IsTouchingWall(out _);
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
    public void OnAttack(InputAction.CallbackContext ctx)
    {
        attackComponent?.RegisterDirectInput(0);
        comboComponent?.RegisterInput(0);

    }

    public void OnSecondaryAttack(InputAction.CallbackContext ctx)
    {
        attackComponent?.RegisterDirectInput(1);
        comboComponent?.RegisterInput(1);
    }
    IEnumerator DashCoroutine()
    {
        isDashing = true;

        Vector2 input = moveInput.normalized;

        Vector2 dir = input != Vector2.zero ? input : new Vector2(right ? 1f : -1f, 0f); 

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