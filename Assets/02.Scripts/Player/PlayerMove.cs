using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerMove : NetworkBehaviour
{
    const float DEFAULT_CONVERT_MOVESPEED = 3f;
    const float DEFAULT_ANIMATION_PLAYSPEED = 0.9f;

    [Header("Movement Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravityScale = 2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 2f;
    [SerializeField] private float runMultiplier = 2f;
    public Vector3 direction { get; private set; }

    [Header("Rotation Smoothing")]
    [SerializeField] private float rotationSmoothTime = 0.1f;

    [Header("Slope Setting")]
    private RaycastHit slopeHit;
    [SerializeField] private float maxSlopeAngle = 40f;
    //이 외에도 위에 groundLayer, groundCheckDistance 포함

    [Header("Ground Check")]
    [SerializeField] Transform groundCheck;

    private Rigidbody rb;
    private float currentAngularY = 0f;
    private bool isGrounded;
    private Animator anim;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        rb.linearDamping = 0f;
        rb.angularDamping = 5f;
    }

    private void Start()
    {
        rb.useGravity = false;
    }

    private void Update()
    {
        if (!IsOwner) return;
        Debug.Log("isGrounded" + isGrounded);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        //CheckGround();
        isGrounded = IsGrounded();

        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        Rotate(mouseX);

        float moveForward = Input.GetAxisRaw("Vertical");
        float moveRight = Input.GetAxisRaw("Horizontal");
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? speed * runMultiplier : speed;

        Move(moveForward, moveRight, currentSpeed);

        if (!isGrounded)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (gravityScale - 1) * Time.fixedDeltaTime;
        }

    }

    //private void CheckGround()
    //{
    //    Vector3 rayStart = transform.position + Vector3.up * 0.2f;
    //    isGrounded = Physics.Raycast(rayStart, Vector3.down, groundCheckDistance, groundLayer);
    //}

    public bool IsGrounded()
    {
        Vector3 boxSize = new Vector3(transform.lossyScale.x, 0.4f, transform.lossyScale.z);
        return Physics.CheckBox(groundCheck.position, boxSize, Quaternion.identity, groundLayer);
    }

    private void Move(float forward, float right, float currentSpeed)
    {
        float animPlaySpeed = DEFAULT_ANIMATION_PLAYSPEED + GetAnimationSyncWithMovement(currentSpeed);
        if (Mathf.Abs(forward) <= 0 && Mathf.Abs(right) <= 0)
        {
            anim.SetBool("Walk", false);
            anim.SetBool("Run", false);
        }
        else
        {
            bool isRunning = currentSpeed > speed;

            anim.SetBool("Run", isRunning);
            anim.SetBool("Walk", !isRunning);
        }

        bool isOnSlope = IsOnSlope();

        direction = (transform.forward * forward + transform.right * right).normalized;
        Vector3 velocity = direction * currentSpeed; 

        float currentYVelocity = rb.linearVelocity.y;

        if (isGrounded && isOnSlope && currentYVelocity <= 0.1f) 
        {
            velocity = AdjustDirectionToSlope(direction) * currentSpeed;
            rb.useGravity = false;
        }
        else
        {
            rb.useGravity = true;
            velocity.y = currentYVelocity;
        }

        rb.linearVelocity = velocity; // 최종 적용
        anim.SetFloat("Velocity", animPlaySpeed);
    }

    private void Rotate(float mouseX)
    {
        float targetAngularY = mouseX;
        currentAngularY = Mathf.Lerp(currentAngularY, targetAngularY, Time.fixedDeltaTime / rotationSmoothTime);
        rb.angularVelocity = new Vector3(0f, currentAngularY, 0f);
        transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
    }

    private void Jump()
    {
        Debug.Log("점프");
        float jumpVelocity = Mathf.Sqrt(2f * jumpHeight * -Physics.gravity.y);
        Debug.Log("점프 밸로시티" + jumpVelocity);
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpVelocity, rb.linearVelocity.z);
    }

    public bool IsOnSlope()
    {
        Ray ray = new Ray(transform.position, Vector3.down);
        if(Physics.Raycast(ray, out slopeHit, groundCheckDistance, groundLayer))
        {
            var angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle != 0f && angle < maxSlopeAngle;
        }
        return false;
    }

    protected Vector3 AdjustDirectionToSlope(Vector3 direction)
    {
        return Vector3.ProjectOnPlane(direction, slopeHit.normal).normalized;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 boxSize = new Vector3(transform.lossyScale.x, 0.4f, transform.lossyScale.z);
        Gizmos.DrawWireCube(groundCheck.position, boxSize);
    }

    public float GetAnimationSyncWithMovement(float changedMoveSpeed)
    {
        if (direction == Vector3.zero)
        {
            return -DEFAULT_ANIMATION_PLAYSPEED;
        }

        return (changedMoveSpeed - DEFAULT_CONVERT_MOVESPEED) * 0.1f;
    }
}