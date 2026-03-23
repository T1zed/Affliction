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

    [Header("dash")]
    public float dashCooldown = 0.3f;
    [Header("Dash")]
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


    private Rigidbody rb;
    private Vector2 moveInput;

    void Awake()
    {
        playerAction = new PlayerAction();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
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
            Vector3 velocity = rb.linearVelocity;
            velocity.x = moveInput.x * moveSpeed;
            rb.linearVelocity = velocity;
        }

        if (!IsGrounded() && !isWallSliding && !isDashing && IsTouchingWall(out _))
        {
            StartCoroutine(WallSlideCoroutine());
        }
    }

    private bool IsGrounded()
    {
        return Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
    }
    private bool IsTouchingWall(out Vector3 wallNormal)
    {
        RaycastHit hit;

        if (Physics.Raycast(transform.position, Vector3.right, out hit, wallCheckDistance, wallLayer))
        {
            wallNormal = hit.normal; 
            return true;
        }
        if (Physics.Raycast(transform.position, Vector3.left, out hit, wallCheckDistance, wallLayer))
        {
            wallNormal = hit.normal; 
            return true;
        }

        wallNormal = Vector3.zero;
        return false;
    }
    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (!IsGrounded()) return;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void OnDash(InputAction.CallbackContext ctx)
    {
        if (!isDashing)
            StartCoroutine(DashCoroutine());
    }
    IEnumerator DashCoroutine()
    {
        isDashing = true;

        Vector2 input = moveInput.normalized;
        Vector3 dir = input != Vector2.zero
            ? new Vector3(input.x, input.y, 0f)
            : new Vector3(transform.localScale.x > 0 ? 1f : -1f, 0f, 0f);

        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.useGravity = false;
        rb.linearVelocity = dir * dashSpeed; 

        float timer = 0f;
        while (timer < dashDuration)
        {
            if (isDashing == false) yield break; 

            rb.linearVelocity = dir * dashSpeed;
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        rb.linearVelocity = Vector3.zero;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        isDashing = false;
    }
    IEnumerator WallSlideCoroutine()
    {
        isWallSliding = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;

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

        rb.useGravity = false;
        while (IsTouchingWall(out _) && !IsGrounded())
        {
            if (isDashing || !IsTouchingWall(out _))
            {
                ExitWallSlide();
                yield break;
            }
            rb.linearVelocity = Vector3.down * wallSlideSpeed;
            yield return new WaitForFixedUpdate();
        }

        ExitWallSlide();
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (isDashing)
        {
            rb.linearVelocity = Vector3.zero;
            rb.useGravity = true;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            isDashing = false;
            StopCoroutine(DashCoroutine());
        }
    }
    private void ExitWallSlide()
    {
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;
        isWallSliding = false;
    }
}