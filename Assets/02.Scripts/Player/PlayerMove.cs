using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerMove : NetworkBehaviour
{
    const float DEFAULT_CONVERT_MOVESPEED = 3f;
    const float DEFAULT_ANIMATION_PLAYSPEED = 0.9f;
    const float DEFAULT_MOVESPEED = 5f;

    [Header("Movement Settings")]
    [SerializeField] public float speed = DEFAULT_MOVESPEED;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravityScale = 2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 2f;
    [SerializeField] private float runMultiplier = 2.3f;
    public Vector3 direction { get; private set; }

    [Header("Rotation Smoothing")]
    [SerializeField] private float rotationSmoothTime = 0.1f;

    [Header("Slope Setting")]
    private RaycastHit slopeHit;
    [SerializeField] private float maxSlopeAngle = 50f;
    //이 외에도 위에 groundLayer, groundCheckDistance 포함

    [Header("Ground Check")]
    [SerializeField] Transform groundCheck;

    private Rigidbody rb;
    private float currentAngularY = 0f;
    //2026-01-23 추가 코드(물리회전으로 전환)
    private float targetRotationY = 0f;
    //2026-01-23 추가 코드(물리회전으로 전환)

    private bool isGrounded;
    private Animator anim;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        rb.linearDamping = 0f;
        rb.angularDamping = 5f;
        //2026-01-23 추가 코드(물리회전으로 전환)
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        //2026-01-23 추가 코드(물리회전으로 전환)
    }

    private void Start()
    {
        rb.useGravity = false;

        //2026-01-23 추가 코드(물리회전으로 전환)
        targetRotationY = transform.eulerAngles.y;
        //2026-01-23 추가 코드(물리회전으로 전환)
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

        isGrounded = IsGrounded();

        float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
        //2026-01-23 추가 코드(물리회전으로 전환)
        HandleRotation(mouseX);
        //2026-01-23 추가 코드(물리회전으로 전환)

        float moveForward = Input.GetAxisRaw("Vertical");
        float moveRight = Input.GetAxisRaw("Horizontal");
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? speed * runMultiplier : speed;

        Move(moveForward, moveRight, currentSpeed);

        if (!isGrounded)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (gravityScale - 1) * Time.fixedDeltaTime;
        }

    }

    public bool IsGrounded()
    {
        //Vector3 boxSize = new Vector3(transform.lossyScale.x, 0.4f, transform.lossyScale.z);
        Vector3 boxSize = new Vector3(0.28f, 0.4f, 0.28f);
        return Physics.CheckBox(groundCheck.position, boxSize, Quaternion.identity, groundLayer);
    }
    //2026-01-23 추가 코드(물리회전으로 전환)
    private void HandleRotation(float mouseX)
    {
        if(Mathf.Abs(mouseX) > 0.01f)
        {
            targetRotationY += mouseX * rotationSpeed * Time.fixedDeltaTime;
        }
        float smoothY = Mathf.LerpAngle(rb.rotation.eulerAngles.y, targetRotationY, Time.fixedDeltaTime / rotationSmoothTime);
        Quaternion targetRot = Quaternion.Euler(0f, smoothY, 0f);
        rb.MoveRotation(targetRot);
    }
    //2026-01-23 추가 코드(물리회전으로 전환)

    private void Move(float forward, float right, float currentSpeed)
    {
        float animPlaySpeed = DEFAULT_ANIMATION_PLAYSPEED + GetAnimationSyncWithMovement(currentSpeed);
        
        bool isOnSlope = IsOnSlope();

        direction = (transform.forward * forward + transform.right * right).normalized;
        Vector3 velocity = direction * currentSpeed; 

        float currentYVelocity = rb.linearVelocity.y;

        //if (isGrounded && isOnSlope && currentYVelocity <= 0.1f)
        if (isGrounded && isOnSlope)
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
        Vector3 boxSize = new Vector3(0.28f, 0.4f, 0.28f);
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

    public void SetMoveSpeed(float m_speed)
    {
        speed = m_speed;
    }
}