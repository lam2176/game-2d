using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Điều khiển platformer: A/D hoặc ←/→ đi, giữ Shift chạy, Space nhảy (thả sớm thì nhảy thấp).
/// Có coyote time và jump buffer. Kiểm tra tiếp đất bằng một hộp mỏng ngay dưới collider.
/// Dùng project-wide actions trong Assets/InputSystem_Actions.inputactions (Player/Move, Jump, Sprint)
/// và đẩy tham số Speed / Running / Grounded / VerticalSpeed sang Animator của Visual.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class ZodiacPlayerController : MonoBehaviour
{
    static readonly int SpeedParam = Animator.StringToHash("Speed");
    static readonly int RunningParam = Animator.StringToHash("Running");
    static readonly int GroundedParam = Animator.StringToHash("Grounded");
    static readonly int VerticalSpeedParam = Animator.StringToHash("VerticalSpeed");

    [Tooltip("Nếu gán, ảnh/collider/tốc độ lấy từ asset này khi vào game.")]
    public ZodiacCharacter character;

    [Header("Di chuyển")]
    public float walkSpeed = 5f;
    public float runSpeed = 8.5f;
    public float groundAcceleration = 60f;
    public float airAcceleration = 35f;

    [Header("Nhảy")]
    public float jumpHeight = 3.2f;
    [Tooltip("Rơi nhanh hơn lúc bay lên cho cú nhảy gọn.")]
    public float fallGravityMultiplier = 1.6f;
    [Tooltip("Thả Space khi đang bay lên thì vận tốc lên nhân hệ số này.")]
    public float jumpCutMultiplier = 0.5f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.12f;

    [Header("Kiểm tra tiếp đất")]
    public LayerMask groundLayer;
    public float groundCheckDepth = 0.08f;

    [Header("Hiển thị")]
    [Tooltip("Object con được lật trái/phải; Animator nằm ở object con của nó.")]
    public Transform facing;
    public SpriteRenderer spriteRenderer;
    public Animator animator;

    Rigidbody2D rb;
    CapsuleCollider2D body;
    InputAction moveAction, jumpAction, sprintAction;
    float baseGravity;
    float lastGroundedTime = -10f, lastJumpPressedTime = -10f;
    bool jumping;
    bool controlEnabled = true;

    public bool IsGrounded { get; private set; }
    public bool IsRunning { get; private set; }

    public static ZodiacPlayerController FromCollider(Collider2D other) =>
        other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<ZodiacPlayerController>() : null;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        body = GetComponent<CapsuleCollider2D>();
        baseGravity = rb.gravityScale;
        ApplyCharacter();

        moveAction = InputSystem.actions.FindAction("Player/Move");
        jumpAction = InputSystem.actions.FindAction("Player/Jump");
        sprintAction = InputSystem.actions.FindAction("Player/Sprint");
        if (moveAction == null || jumpAction == null || sprintAction == null)
        {
            Debug.LogError("Thiếu action Player/Move, Player/Jump hoặc Player/Sprint trong InputSystem_Actions.", this);
            enabled = false;
        }
    }

    void OnEnable()
    {
        moveAction?.Enable();
        jumpAction?.Enable();
        sprintAction?.Enable();
    }

    void ApplyCharacter()
    {
        if (character == null) return;
        walkSpeed = character.walkSpeed;
        runSpeed = character.runSpeed;
        jumpHeight = character.jumpHeight;
        body.size = character.colliderSize;
        body.offset = character.colliderOffset;
        if (spriteRenderer != null && character.sprite != null) spriteRenderer.sprite = character.sprite;
    }

    /// <summary>Khoá/mở điều khiển. Khi khoá, nhân vật đứng yên tại chỗ (dùng lúc thắng/thua).</summary>
    public void SetControlEnabled(bool value)
    {
        controlEnabled = value;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = value;
    }

    void Update()
    {
        if (controlEnabled)
        {
            if (jumpAction.WasPressedThisFrame()) lastJumpPressedTime = Time.time;
            if (jumpAction.WasReleasedThisFrame() && jumping && rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
                jumping = false;
            }
        }
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        IsGrounded = CheckGround();
        if (IsGrounded)
        {
            lastGroundedTime = Time.time;
            if (rb.linearVelocity.y <= 0.01f) jumping = false;
        }
        if (!controlEnabled) return;

        float inputX = moveAction.ReadValue<Vector2>().x;
        if (Mathf.Abs(inputX) < 0.1f) inputX = 0f;
        IsRunning = inputX != 0f && sprintAction.IsPressed();

        float targetSpeed = inputX * (IsRunning ? runSpeed : walkSpeed);
        float accel = IsGrounded ? groundAcceleration : airAcceleration;
        float vx = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accel * Time.fixedDeltaTime);
        float vy = rb.linearVelocity.y;

        bool jumpBuffered = Time.time - lastJumpPressedTime <= jumpBufferTime;
        bool canJump = Time.time - lastGroundedTime <= coyoteTime && !jumping;
        if (jumpBuffered && canJump)
        {
            vy = Mathf.Sqrt(2f * jumpHeight * Mathf.Abs(Physics2D.gravity.y * baseGravity));
            jumping = true;
            lastJumpPressedTime = -10f;
            lastGroundedTime = -10f;
        }

        rb.linearVelocity = new Vector2(vx, vy);
        rb.gravityScale = vy < 0f ? baseGravity * fallGravityMultiplier : baseGravity;

        if (inputX > 0f) SetFacing(1f);
        else if (inputX < 0f) SetFacing(-1f);
    }

    bool CheckGround()
    {
        GetGroundBox(out Vector2 center, out Vector2 size);
        return Physics2D.OverlapBox(center, size, 0f, groundLayer) != null;
    }

    void GetGroundBox(out Vector2 center, out Vector2 size)
    {
        Bounds b = body.bounds;
        center = new Vector2(b.center.x, b.min.y - groundCheckDepth / 2f);
        size = new Vector2(b.size.x * 0.9f, groundCheckDepth);
    }

    void SetFacing(float direction)
    {
        if (facing != null) facing.localScale = new Vector3(direction, 1f, 1f);
    }

    void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat(SpeedParam, Mathf.Abs(rb.linearVelocity.x));
        animator.SetBool(RunningParam, IsRunning);
        animator.SetBool(GroundedParam, IsGrounded);
        animator.SetFloat(VerticalSpeedParam, rb.linearVelocity.y);
    }

    void OnDrawGizmosSelected()
    {
        if (body == null) body = GetComponent<CapsuleCollider2D>();
        GetGroundBox(out Vector2 center, out Vector2 size);
        Gizmos.color = Application.isPlaying && IsGrounded ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(center, size);
    }
}
