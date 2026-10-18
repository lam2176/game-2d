using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Điều khiển platformer: A/D hoặc ←/→ đi, giữ Shift chạy, Space nhảy (thả sớm thì nhảy thấp).
/// Input đọc trong Update, vật lý xử lý trong FixedUpdate. Chỉ nhảy khi đang tiếp đất
/// (cộng coyote time ngắn sau khi rời mép và jump buffer khi bấm sớm trước lúc chạm đất).
/// Mọi thông số chỉnh trực tiếp ở Inspector, kể cả khi đang Play.
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

    [Tooltip("Ảnh và collider thân lấy từ asset này khi vào game.")]
    public ZodiacCharacter character;

    [Header("Di chuyển")]
    [Min(0f)] public float walkSpeed = 5f;
    [Tooltip("Tốc độ khi giữ Shift.")]
    [Min(0f)] public float runSpeed = 8.5f;
    [Tooltip("Tăng tốc trên mặt đất (unit/s²).")]
    [Min(0f)] public float groundAcceleration = 60f;
    [Tooltip("Giảm tốc trên mặt đất khi thả phím hoặc đổi hướng (unit/s²).")]
    [Min(0f)] public float groundDeceleration = 80f;
    [Tooltip("Tăng/giảm tốc khi đang trên không (unit/s²).")]
    [Min(0f)] public float airAcceleration = 35f;

    [Header("Nhảy")]
    [Tooltip("Độ cao đỉnh cú nhảy (unit) khi giữ Space. Lực nhảy được tính từ giá trị này và trọng lực.")]
    [Min(0f)] public float jumpHeight = 3.2f;
    [Tooltip("Thả Space khi đang bay lên thì vận tốc lên nhân hệ số này (nhảy thấp).")]
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;
    [Tooltip("Vẫn nhảy được trong khoảng này sau khi bước khỏi mép (giây). 0 = tắt.")]
    [Min(0f)] public float coyoteTime = 0.1f;
    [Tooltip("Bấm Space sớm trong khoảng này trước khi chạm đất vẫn tính (giây). 0 = tắt.")]
    [Min(0f)] public float jumpBufferTime = 0.12f;

    [Header("Trọng lực")]
    [Tooltip("Ghi đè Gravity Scale của Rigidbody2D (Physics2D.gravity = -9.81).")]
    [Min(0f)] public float gravityScale = 3f;
    [Tooltip("Rơi nhanh hơn lúc bay lên cho cú nhảy gọn.")]
    [Min(1f)] public float fallGravityMultiplier = 1.6f;
    [Tooltip("Tốc độ rơi tối đa (unit/s) — tránh rơi quá nhanh xuyên nền mỏng.")]
    [Min(1f)] public float maxFallSpeed = 20f;

    [Header("Kiểm tra tiếp đất")]
    public LayerMask groundLayer;
    [Min(0.01f)] public float groundCheckDepth = 0.08f;

    [Header("Hiển thị")]
    [Tooltip("Object con được lật trái/phải; Animator nằm ở object con của nó.")]
    public Transform facing;
    public SpriteRenderer spriteRenderer;
    public Animator animator;

    Rigidbody2D rb;
    CapsuleCollider2D body;
    InputAction moveAction, jumpAction, sprintAction;
    readonly Collider2D[] groundHits = new Collider2D[4];

    float inputX;
    bool sprintHeld, jumpCutRequested;
    float coyoteCounter, jumpBufferCounter;
    bool jumping;
    bool controlEnabled = true;

    public bool IsGrounded { get; private set; }
    public bool IsRunning { get; private set; }

    /// <summary>Vận tốc nhảy ban đầu để đạt đúng jumpHeight với bước vật lý hiện tại.</summary>
    public float JumpVelocity
    {
        get
        {
            float g = Mathf.Abs(Physics2D.gravity.y) * gravityScale;
            float half = g * Time.fixedDeltaTime * 0.5f;   // bù sai số tích phân từng bước
            return half + Mathf.Sqrt(half * half + 2f * g * jumpHeight);
        }
    }

    public static ZodiacPlayerController FromCollider(Collider2D other) =>
        other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<ZodiacPlayerController>() : null;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        body = GetComponent<CapsuleCollider2D>();
        rb.gravityScale = gravityScale;
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
        body.size = character.colliderSize;
        body.offset = character.colliderOffset;
        if (spriteRenderer != null && character.sprite != null) spriteRenderer.sprite = character.sprite;
    }

    /// <summary>Khoá/mở điều khiển. Khi khoá, nhân vật đứng yên tại chỗ (dùng lúc thắng/thua).</summary>
    public void SetControlEnabled(bool value)
    {
        controlEnabled = value;
        inputX = 0f;
        sprintHeld = jumpCutRequested = false;
        jumpBufferCounter = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = value;
    }

    void Update()
    {
        if (controlEnabled)
        {
            inputX = moveAction.ReadValue<Vector2>().x;
            if (Mathf.Abs(inputX) < 0.1f) inputX = 0f;
            sprintHeld = sprintAction.IsPressed();
            if (jumpAction.WasPressedThisFrame()) jumpBufferCounter = Mathf.Max(jumpBufferTime, Time.fixedDeltaTime);
            if (jumpAction.WasReleasedThisFrame()) jumpCutRequested = true;

            if (inputX > 0f) SetFacing(1f);
            else if (inputX < 0f) SetFacing(-1f);
        }
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        Vector2 v = rb.linearVelocity;

        IsGrounded = CheckGround(v.y);
        if (IsGrounded)
        {
            coyoteCounter = coyoteTime;
            jumping = false;
        }
        else
        {
            coyoteCounter -= dt;
        }
        if (!controlEnabled) return;

        // Ngang: tăng tốc tới tốc độ mục tiêu, giảm tốc riêng khi thả phím hoặc đổi hướng.
        IsRunning = inputX != 0f && sprintHeld;
        float targetSpeed = inputX * (IsRunning ? runSpeed : walkSpeed);
        bool slowingDown = Mathf.Abs(targetSpeed) < Mathf.Abs(v.x) || targetSpeed * v.x < 0f;
        float accel = !IsGrounded ? airAcceleration : slowingDown ? groundDeceleration : groundAcceleration;
        v.x = Mathf.MoveTowards(v.x, targetSpeed, accel * dt);

        // Nhảy: chỉ khi đang đất (hoặc vừa rời mép trong coyote time) và chưa nhảy.
        if (jumpBufferCounter > 0f && coyoteCounter > 0f && !jumping)
        {
            v.y = JumpVelocity;
            jumping = true;
            IsGrounded = false;   // báo ngay cho camera/Animator, không đợi bước vật lý sau
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            jumpCutRequested = false;
        }
        else
        {
            jumpBufferCounter -= dt;
        }

        if (jumpCutRequested)
        {
            if (jumping && v.y > 0f) v.y *= jumpCutMultiplier;
            jumpCutRequested = false;
        }

        v.y = Mathf.Max(v.y, -maxFallSpeed);
        rb.linearVelocity = v;
        rb.gravityScale = v.y < 0f ? gravityScale * fallGravityMultiplier : gravityScale;
    }

    bool CheckGround(float verticalVelocity)
    {
        // Đang bay lên thì không tính là đứng trên đất (tránh nhảy liên tiếp khi vừa rời sàn).
        if (verticalVelocity > 0.1f) return false;
        GetGroundBox(out Vector2 center, out Vector2 size);
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(groundLayer);
        return Physics2D.OverlapBox(center, size, 0f, filter, groundHits) > 0;
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

    void OnValidate()
    {
        runSpeed = Mathf.Max(runSpeed, walkSpeed);
        if (rb != null && Application.isPlaying) rb.gravityScale = gravityScale;
    }

    void OnDrawGizmosSelected()
    {
        if (body == null) body = GetComponent<CapsuleCollider2D>();
        GetGroundBox(out Vector2 center, out Vector2 size);
        Gizmos.color = Application.isPlaying && IsGrounded ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(center, size);
    }
}
