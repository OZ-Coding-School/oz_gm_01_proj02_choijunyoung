using UnityEngine;

public class TestPlayerMovement_Monobehaviour : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float rotationSpeed = 120f;  
    [SerializeField] private float jumpHeight = 2f;       
    [SerializeField] private float gravityScale = 2f;   
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 1f;  
    [SerializeField] private float runMultiplier = 1.5f;

    [Header("Rotation Smoothing")]
    [SerializeField] private float rotationSmoothTime = 0.1f;

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
        Debug.Log("isGrounded"+isGrounded);
        
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
    }

    private void FixedUpdate()
    {
        // if (!IsOwner) return;

        CheckGround();

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

    private void CheckGround()
    {
        Vector3 rayStart = transform.position + Vector3.up * 0.2f;
        isGrounded = Physics.Raycast(rayStart, Vector3.down, groundCheckDistance, groundLayer);
    }

    private void Move(float forward, float right, float currentSpeed)
    {
        if(Mathf.Abs(forward) <= 0 && Mathf.Abs(right) <= 0)
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
        
        Vector3 direction = (transform.forward * forward + transform.right * right).normalized;
        Vector3 velocity = direction * currentSpeed;

        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;
    }

    private void Rotate(float mouseX)
    {
        float targetAngularY = mouseX;
        currentAngularY = Mathf.Lerp(currentAngularY, targetAngularY, Time.fixedDeltaTime / rotationSmoothTime);
        rb.angularVelocity = new Vector3(0f, currentAngularY, 0f);
    }

    private void Jump()
    {
        Debug.Log("점프");
        float jumpVelocity = Mathf.Sqrt(2f * jumpHeight * -Physics.gravity.y);
        Debug.Log("점프 밸로시티" + jumpVelocity);
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpVelocity, rb.linearVelocity.z);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 start = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawRay(start, Vector3.down * groundCheckDistance);
    }
}