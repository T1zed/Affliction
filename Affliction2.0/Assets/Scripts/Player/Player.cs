using UnityEngine.InputSystem;
using UnityEngine;
using System.Collections;

public enum PState
{
    IDLE,
    RUNNING,
    JUMPING,
    LANDING,
    DASHING,
    WALLSLIDING,
    GLIDING,
    SLIDING,
}

public enum PAttacks
{
    NEUTRAL,
    SIDE,
    DOWN,
    NEUTRALAIR,
    SIDEAIR,
    DOWNAIR,
}
public class Player : MonoBehaviour
{

    private PlayerAction playerAction;
    private InputAction moveAction;
    private InputAction dashAction;
    private InputAction jumpAction;
    private InputAction AttackAction;
    private InputAction SecondaryAttackAction;
    private InputAction DownAction;
    [Header("direction")]
    public bool right;
    public PAttacks state;
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
    [SerializeField] private GameObject wallCheck;
    public bool isWallSliding = false;

    [Header("WallJump")]
    private bool isWallJumping;
    private float wallJumpDirection;
    private float wallJumpTime=0.2f;
    private float wallJumpingCounter;
    private float wallJumpingDirection = 0.4f;
    private Vector2 wallJumpingPower = new Vector2(11f, 11f);
    [SerializeField] private float wallSlideDelay = 2f;
    [SerializeField] private float wallSlideSpeed = 0.5f;
    private ComboComponent comboComponent;
    private AttackComponent attackComponent;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    [Header("context")]
    public bool grounded;
    public bool onWall;
    private bool isPressingDown = false;

    [Header("animations")]
    [SerializeField]private Animator animator;
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
        DownAction = playerAction.Player.Down;
        SecondaryAttackAction=playerAction.Player.SecondaryAttack;

        jumpAction.started += OnJumpStarted;
        jumpAction.canceled += OnJumpCanceled;
        dashAction.performed += OnDash;
        AttackAction.performed += OnAttack;
        SecondaryAttackAction.performed += OnSecondaryAttack;
        DownAction.started += OnDown;
        DownAction.canceled += OnDown;
    }

    private void OnDisable()
    {
        playerAction.Player.Disable();
        jumpAction.started -= OnJumpStarted;
        jumpAction.canceled -= OnJumpCanceled;
        dashAction.performed -= OnDash;
        AttackAction.performed -= OnAttack;
        SecondaryAttackAction.performed -= OnSecondaryAttack;
        DownAction.started -= OnDown;
        DownAction.canceled -= OnDown;
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
    private void StateShow(bool left, bool right)
    {
        if (grounded && isPressingDown)
            state = PAttacks.DOWN;
        else if (!grounded && isPressingDown)
            state = PAttacks.DOWNAIR;
        else if (grounded && (left || right))
            state = PAttacks.SIDE;
        else if (!grounded && (left || right))
            state = PAttacks.SIDEAIR;
        else if (!grounded)
            state = PAttacks.NEUTRALAIR;
        else
            state = PAttacks.NEUTRAL;

        Debug.Log(state);
    }
    void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();

        bool isPressingRight = moveInput.x > 0;
        bool isPressingLeft = moveInput.x < 0;
        bool isMoving = isPressingRight || isPressingLeft;
        WallSlide();
        WallJump(); 
        if (!isMovementLocked)
        {
            if (isPressingRight && !wasRight)
            {
                right = true;
                transform.localScale = new Vector3(1f, transform.localScale.y, transform.localScale.z);
      
            }
            else if (isPressingLeft && !wasLeft)
            {
                right = false;
                transform.localScale = new Vector3(-1f, transform.localScale.y, transform.localScale.z);

            }


            animator.SetBool("isRunning", isMoving);
        }
        StateShow(isPressingLeft, isPressingRight);
      
        wasRight = isPressingRight;
        wasLeft = isPressingLeft;
    }
    [SerializeField] float maxGravityScale = 5f;     
    [SerializeField] float gravityAcceleration = 0.5f;
    void FixedUpdate()
    {
        grounded = IsGrounded();
        if (grounded && rb.linearVelocityY <= 0f)
            isjumping = false;
        if (!isDashing)
        {
            Vector2 velocity = rb.linearVelocity;
            velocity.x = isMovementLocked ? 0f : moveInput.x * moveSpeed;
            rb.linearVelocity = velocity;
        }
  

    }
    public bool IsWalled()
    {
        return Physics2D.OverlapCircle(wallCheck.transform.position, 0.2f, wallLayer);
    }

    private void WallSlide()
    {
        if (IsWalled() && !grounded)
        {
            isWallSliding = true;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Clamp(rb.linearVelocity.y, -wallSlideSpeed, float.MaxValue));
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void WallJump()
    {
        if (isWallSliding)
        {
            isWallJumping = false;
            wallJumpDirection = -transform.localScale.x;
            wallJumpingCounter = wallJumpTime;
            CancelInvoke(nameof(StopWallJumping));
        }
        else
        {
            wallJumpingCounter -= Time.deltaTime;
        }

        if (jumpPressed && wallJumpingCounter > 0f)
        {
            isWallJumping = true;
            jumpPressed = false; 
            rb.linearVelocity = new Vector2(wallJumpDirection * wallJumpingPower.x, wallJumpingPower.y);
            wallJumpingCounter = 0f;

            if (transform.localScale.x != wallJumpDirection)
            {
                right = !right;
                Vector3 localScale = transform.localScale;
                localScale.x *= -1;
                transform.localScale = localScale;
            }

            Invoke(nameof(StopWallJumping), wallJumpTime);
        }
    }

    private void StopWallJumping()
    {
        isWallJumping = false;
    }
    public bool IsGrounded()
    {

        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }

    private bool jumpPressed;
    private bool isjumping;

    private void OnJumpStarted(InputAction.CallbackContext ctx)
    {
        jumpPressed = true;

        if (IsGrounded())
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            isjumping = true;
        }
    }

    private void OnJumpCanceled(InputAction.CallbackContext ctx)
    {
        jumpPressed = false;
        if (rb.linearVelocityY > 0f)
        {
            rb.linearVelocityY *= 0.5f;
            isjumping = false;
        }

     
    }
    private void OnDown(InputAction.CallbackContext ctx)
    {
        if (ctx.started) isPressingDown = true;
        else if (ctx.canceled) isPressingDown = false;
    }

    private AttackDirection GetAttackDirection()
    {
        if (isPressingDown) return AttackDirection.Down;
        if (Mathf.Abs(moveInput.x) > 0.5f) return AttackDirection.Side;
        return AttackDirection.Neutral;
    }

    public void OnAttack(InputAction.CallbackContext ctx)
    {
        var dir = GetAttackDirection();
        attackComponent?.RegisterDirectInput(AttackButton.Primary, dir);
    }

    public void OnSecondaryAttack(InputAction.CallbackContext ctx)
    {
        var dir = GetAttackDirection();
        attackComponent?.RegisterDirectInput(AttackButton.Secondary, dir);
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