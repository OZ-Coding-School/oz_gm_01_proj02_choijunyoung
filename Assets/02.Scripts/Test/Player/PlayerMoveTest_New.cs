using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Player))]
public class PlayerMoveTest_New : MonoBehaviour
{
    protected Player player;
    public Vector3 direction { get; private set; }
    protected const float CONVERT_UNIT_VALUE = 0.01f;
    Rigidbody rigidBody;
    private const float RAY_DISTANCE = 2f;
    private RaycastHit slopeHit;
    private int groundLayer = 1 << LayerMask.NameToLayer("Ground");  // 땅(Ground) 레이어만 체크
    private float maxSlopeAngle = 40f;

    [SerializeField] Transform groundCheck;

    
    protected Animator animator;
    protected const float DEFAULT_CONVERT_MOVESPEED = 3f;
    protected const float DEFAULT_ANIMATION_PLAYSPEED = 0.9f;

    private bool isJumping;
    [SerializeField] private float jumpForce = 10f;

    float moveSpeed;

    private void Start()
    {
        player  = GetComponent<Player>();
        rigidBody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        isJumping = false;
    }
    private void FixedUpdate()
    {
        moveSpeed = player.MoveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            moveSpeed *= 1.5f;
        }

        Move(moveSpeed);

    }

    public void OnMoveInput(InputAction.CallbackContext context)
    {
        Debug.Log("이동");
        Vector2 input = context.ReadValue<Vector2>();
        Debug.Log(input);
        direction = new Vector3(input.x, 0f, input.y);
    }

    public void OnJumpInput(InputAction.CallbackContext context)
    {
        // 땅에 있고 점프 중이 아닐 때
        if (context.phase == InputActionPhase.Performed && IsGrounded() && !isJumping)
        {
            Debug.Log("점프");
            isJumping = true;

            Vector3 vel = rigidBody.linearVelocity;
            vel.y = jumpForce; 
            rigidBody.linearVelocity = vel;
        }
    }

    protected void Move(float speed)
    {
        float currentMoveSpeed = speed * CONVERT_UNIT_VALUE;
        float animationPlaySpeed = DEFAULT_ANIMATION_PLAYSPEED + GetAnimationSyncWithMovement(currentMoveSpeed);

        bool isOnSlope = IsOnSlope();
        bool isGrounded = IsGrounded();

        if (isGrounded && rigidBody.linearVelocity.y < 0f) isJumping = false;
        Vector3 velocity = direction;
        Vector3 gravity = Vector3.up * Mathf.Abs(rigidBody.linearVelocity.y);


        if (isOnSlope && isGrounded && !isJumping)
        {
            velocity = AdjustDirectionToSlope(direction);
            gravity = Vector3.zero;
            rigidBody.useGravity = false;
        }
        else
        {
            rigidBody.useGravity = true;
            if (rigidBody.linearVelocity.y < 0f) gravity += Vector3.down * 0.5f;
        }

        LookAt();
        rigidBody.linearVelocity = velocity * currentMoveSpeed + gravity;
        Debug.Log(rigidBody.linearVelocity.y);
        animator.SetFloat("Velocity", animationPlaySpeed);
    }

    protected void LookAt() 
    {
        if (direction != Vector3.zero)
        {
            Quaternion targetAngle = Quaternion.LookRotation(direction);
            rigidBody.rotation = targetAngle;
        }
    }

    public bool IsOnSlope()
    {
        Ray ray = new Ray(transform.position, Vector3.down);
        if (Physics.Raycast(ray, out slopeHit, RAY_DISTANCE, groundLayer))
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

    public bool IsGrounded()
    {
        Vector3 boxSize = new Vector3(transform.lossyScale.x, 0.4f, transform.lossyScale.z);
        return Physics.CheckBox(groundCheck.position, boxSize, Quaternion.identity, groundLayer);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 boxSize = new Vector3(transform.lossyScale.x, 0.4f, transform.lossyScale.z);
        Gizmos.DrawWireCube(groundCheck.position, boxSize);
    }

    protected float GetAnimationSyncWithMovement(float changedMoveSpeed)
    {
        if(direction == Vector3.zero)
        {
            return -DEFAULT_ANIMATION_PLAYSPEED;
        }

        return (changedMoveSpeed - DEFAULT_CONVERT_MOVESPEED) * 0.1f;
    }

}
