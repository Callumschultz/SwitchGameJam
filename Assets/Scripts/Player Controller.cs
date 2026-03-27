using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float jumpForce = 5f;
    public float groundCheckDistance = 0.2f;

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public Transform groundCheck;

    private Rigidbody rb;
    private Animator anim;
    private bool isGrounded;
    private bool isRunning;

    private bool is2DMode = false;
    public bool Is2DMode => is2DMode;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        rb.freezeRotation = true;
    }

    void Update()
    {
        GroundCheck();
        HandleJump();
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleMovement()
{
    float h = Input.GetAxisRaw("Horizontal");
    float v = Input.GetAxisRaw("Vertical");

    isRunning = Input.GetKey(KeyCode.LeftShift);
    float speed = isRunning ? runSpeed : walkSpeed;

    Vector3 moveDir;

    if (is2DMode)
    {
        moveDir = new Vector3(h, 0f, 0f).normalized;
        Vector3 vel = rb.linearVelocity;
        vel.z = 0f;
        rb.linearVelocity = vel;
    }
    else
    {
        // Remapped for camera facing -X
        moveDir = new Vector3(v, 0f, -h).normalized;
    }

    Vector3 targetVelocity = moveDir * speed;
    targetVelocity.y = rb.linearVelocity.y;
    rb.linearVelocity = targetVelocity;

    if (moveDir != Vector3.zero)
    {
        Quaternion targetRot = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.fixedDeltaTime);
    }
}

    void HandleJump()
    {
        // Prevent "jump stacking" when the player is still considered grounded
        // due to door/floor contact or minor collider penetration.
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && rb.linearVelocity.y <= 0.1f)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            anim.SetBool("isJumping", true);
        }

        // Clear jump when grounded again
        if (isGrounded && rb.linearVelocity.y <= 0.1f)
        {
            anim.SetBool("isJumping", false);
        }
    }

    void GroundCheck()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckDistance, groundLayer);
    }

    void UpdateAnimator()
    {
        float horizontalSpeed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
        anim.SetFloat("Speed", horizontalSpeed);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckDistance);
        }
    }

    public void SetDimensionMode(bool twoD)
{
    is2DMode = twoD;

    if (twoD)
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y, 0f);
    }
}
}