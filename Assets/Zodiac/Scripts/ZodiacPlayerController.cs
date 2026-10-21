using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Điều khiển platformer: A/D hoặc ←/→ đi, giữ Shift chạy, Space nhảy (thả sớm thì nhảy thấp).
/// Input đọc trong Update, vật lý xử lý trong FixedUpdate. Nhảy khi đang tiếp đất
/// (cộng coyote time ngắn sau khi rời mép và jump buffer khi bấm sớm trước lúc chạm đất),
/// nhảy thêm trên không nếu nhân vật có airJumps, giữ Space khi rơi để lượn nếu có glideFallSpeed.
/// Đứng trên <see cref="MovingPlatform"/> thì đi theo bục. ↓ + Space trên bục tre (một chiều) thì rơi xuyên xuống.
///
/// Nhân vật lấy từ <see cref="roster"/> theo lựa chọn ở màn hình chính (dự phòng: <see cref="character"/>);
/// chỉ số tốc độ/nhảy/máu của asset nhân vật ghi đè các ô bên dưới khi vào game — vẫn chỉnh được khi đang Play.
/// Chiêu thức (PlayerSkill) tạm chiếm quyền điều khiển vận tốc bằng <see cref="SetOverride"/>.
/// Dùng project-wide actions trong Assets/InputSystem_Actions.inputactions (Player/Move, Jump, Sprint).
/// Animation do <see cref="CharacterAnimator"/> tính bằng code từ trạng thái và các sự kiện <see cref="Jumped"/>, <see cref="Landed"/>.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class ZodiacPlayerController : MonoBehaviour
{
    [Tooltip("Danh sách 12 con giáp — lấy nhân vật đang chọn (ZodiacSelection) khi vào game.")]
    public ZodiacRoster roster;
    [Tooltip("Nhân vật dùng khi không có roster.")]
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
    [Tooltip("Số lần nhảy thêm trên không.")]
    [Min(0)] public int airJumps = 0;
    [Tooltip("Giữ Space khi rơi thì tốc độ rơi tối đa bằng giá trị này. 0 = không lượn.")]
    [Min(0f)] public float glideFallSpeed = 0f;

    [Header("Trọng lực")]
    [Tooltip("Ghi đè Gravity Scale của Rigidbody2D (Physics2D.gravity = -9.81).")]
    [Min(0f)] public float gravityScale = 3f;
    [Tooltip("Rơi nhanh hơn lúc bay lên cho cú nhảy gọn.")]
    [Min(1f)] public float fallGravityMultiplier = 1.6f;
    [Tooltip("Tốc độ rơi tối đa (unit/s) — tránh rơi quá nhanh xuyên nền mỏng.")]
    [Min(1f)] public float maxFallSpeed = 20f;
    [Tooltip("Ở gần đỉnh cú nhảy (|vận tốc dọc| nhỏ hơn giá trị này) mà còn giữ Space thì trọng lực nhẹ đi — lơ lửng một chút như Celeste.")]
    [Min(0f)] public float apexThreshold = 2.2f;
    [Range(0.1f, 1f)] public float apexGravityMultiplier = 0.55f;

    [Header("Bị đánh (cần component Health)")]
    [Tooltip("Vận tốc bị đẩy ra xa nguồn sát thương (x) và bật lên (y).")]
    public Vector2 hurtKnockback = new Vector2(6f, 7f);
    [Tooltip("Mất điều khiển trong khoảng này sau khi bị đánh (giây).")]
    [Min(0f)] public float hurtStunTime = 0.25f;
    [Range(0f, 1f)] public float knockbackResistance = 0f;

    [Header("Kiểm tra tiếp đất")]
    public LayerMask groundLayer;
    [Min(0.01f)] public float groundCheckDepth = 0.08f;
    [Tooltip("Thời gian bỏ qua va chạm với bục một chiều khi ↓ + Space (giây).")]
    [Min(0f)] public float dropThroughTime = 0.3f;

    [Header("Hiển thị")]
    [Tooltip("Object con được lật trái/phải; Visual (ảnh) nằm ở object con của nó.")]
    public Transform facing;
    public SpriteRenderer spriteRenderer;

    /// <summary>Người chơi duy nhất trong màn (quái, đồng xu, camera dùng).</summary>
    public static ZodiacPlayerController Current { get; private set; }

    Rigidbody2D rb;
    CapsuleCollider2D body;
    Health health;
    InputAction moveAction, jumpAction, sprintAction;
    readonly Collider2D[] groundHits = new Collider2D[4];

    float inputX, inputY;
    bool sprintHeld, jumpHeld, jumpCutRequested;
    float coyoteCounter, jumpBufferCounter;
    bool jumping;
    int airJumpsLeft;
    bool controlEnabled = true;
    float stunCounter;
    bool overridden;
    float overrideGravity;
    Collider2D droppedPlatform;
    float dropUntil;

    /// <summary>Vừa bật nhảy (true = nhảy trên không).</summary>
    public event Action<bool> Jumped;
    /// <summary>Vừa chạm đất, kèm tốc độ rơi lúc chạm (unit/s, dương).</summary>
    public event Action<float> Landed;

    public bool ControlEnabled => controlEnabled;
    public bool IsGrounded { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsGliding { get; private set; }
    /// <summary>Đang tàng hình — quái không phát hiện.</summary>
    public bool IsHidden { get; set; }
    /// <summary>1 = nhìn phải, -1 = nhìn trái.</summary>
    public float FacingDirection { get; private set; } = 1f;
    public ZodiacCharacter Character => character;
    public Rigidbody2D Body => rb;
    public CapsuleCollider2D BodyCollider => body;
    public Vector2 Velocity => rb.linearVelocity;
    /// <summary>Collider đang đứng lên (null khi trên không).</summary>
    public Collider2D GroundCollider { get; private set; }
    public bool IsOverridden => overridden;
    /// <summary>Trục dọc của phím di chuyển (↑ = 1, ↓ = -1) — dùng cho đánh lên/xuống.</summary>
    public float InputY => inputY;

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
        Current = this;
        rb = GetComponent<Rigidbody2D>();
        body = GetComponent<CapsuleCollider2D>();
        health = GetComponent<Health>();
        if (roster != null && roster.Count > 0) character = roster.Get(ZodiacSelection.Index);
        ApplyCharacter();
        rb.gravityScale = gravityScale;
        if (health != null) health.Damaged += OnDamaged;

        moveAction = InputSystem.actions.FindAction("Player/Move");
        jumpAction = InputSystem.actions.FindAction("Player/Jump");
        sprintAction = InputSystem.actions.FindAction("Player/Sprint");
        if (moveAction == null || jumpAction == null || sprintAction == null)
        {
            Debug.LogError("Thiếu action Player/Move, Player/Jump hoặc Player/Sprint trong InputSystem_Actions.", this);
            enabled = false;
        }
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
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

        walkSpeed = character.walkSpeed;
        runSpeed = Mathf.Max(character.runSpeed, character.walkSpeed);
        jumpHeight = character.jumpHeight;
        airJumps = character.airJumps;
        glideFallSpeed = character.glideFallSpeed;
        knockbackResistance = character.knockbackResistance;
        // Máu, Khí, công, giáp đi qua CharacterStats để trang bị / nâng cấp cộng thêm được
        var stats = GetComponent<CharacterStats>();
        if (stats == null) stats = gameObject.AddComponent<CharacterStats>();
        stats.InitFrom(character);
        if (health != null) health.lastStandCharges = character.lastStandCharges;
    }

    /// <summary>Đẩy nhân vật đi với vận tốc cho trước và tạm mất điều khiển trong stunTime giây.</summary>
    public void ApplyKnockback(Vector2 velocity, float stunTime)
    {
        if (!controlEnabled || overridden) return;
        rb.linearVelocity = velocity;
        stunCounter = stunTime;
        jumping = false;
        jumpBufferCounter = 0f;
        IsGrounded = false;
    }

    /// <summary>Bật nảy lên (đạp đầu quái). Giữ Space thì nảy hết cỡ, thả ra thì bị cắt như cú nhảy thường.</summary>
    public void Bounce(float velocity)
    {
        if (!controlEnabled) return;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, velocity);
        jumping = true;
        IsGrounded = false;
        coyoteCounter = 0f;
        jumpBufferCounter = 0f;
        airJumpsLeft = airJumps;   // nảy trên đầu quái thì được nhảy đôi lại (như pogo của Hollow Knight)
    }

    /// <summary>
    /// Chiêu thức chiếm quyền điều khiển: controller không đổi vận tốc, chỉ đặt trọng lực = gravity.
    /// Gọi lại với active = false để trả quyền.
    /// </summary>
    public void SetOverride(bool active, float gravity = 0f)
    {
        overridden = active;
        overrideGravity = gravity;
        stunCounter = 0f;
        if (!active) rb.gravityScale = gravityScale;
    }

    /// <summary>Dịch chuyển tức thời (hồi sinh tại đèn lồng).</summary>
    public void Teleport(Vector2 position)
    {
        rb.position = position;
        transform.position = position;
        rb.linearVelocity = Vector2.zero;
        stunCounter = 0f;
        jumping = false;
        IsGrounded = false;
    }

    void OnDamaged(Hit hit)
    {
        ScreenFader.Flash(new Color(0.8f, 0.05f, 0.05f, 0.35f), 0.35f);
        GameFeel.HitStop(0.07f);
        GameFeel.Shake(0.3f);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.hurt, 0.9f);
        if (hit.knockback <= 0f) return;
        float away = transform.position.x >= hit.source.x ? 1f : -1f;
        float k = hit.knockback * (1f - knockbackResistance);
        ApplyKnockback(new Vector2(away * hurtKnockback.x * k, hurtKnockback.y * Mathf.Max(k, 0.5f)), hurtStunTime);
    }

    /// <summary>Khoá/mở điều khiển. Khi khoá, nhân vật đứng yên tại chỗ (dùng lúc thắng/thua).</summary>
    public void SetControlEnabled(bool value)
    {
        controlEnabled = value;
        inputX = inputY = 0f;
        sprintHeld = jumpHeld = jumpCutRequested = false;
        jumpBufferCounter = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = value;
    }

    void Update()
    {
        if (controlEnabled)
        {
            Vector2 move = moveAction.ReadValue<Vector2>();
            inputX = Mathf.Abs(move.x) < 0.1f ? 0f : move.x;
            inputY = move.y;
            sprintHeld = sprintAction.IsPressed();
            jumpHeld = jumpAction.IsPressed();
            if (jumpAction.WasPressedThisFrame()) jumpBufferCounter = Mathf.Max(jumpBufferTime, Time.fixedDeltaTime);
            if (jumpAction.WasReleasedThisFrame()) jumpCutRequested = true;

            if (!overridden)
            {
                if (inputX > 0f) SetFacing(1f);
                else if (inputX < 0f) SetFacing(-1f);
            }
        }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        Vector2 v = rb.linearVelocity;
        bool wasGrounded = IsGrounded;
        float fallSpeed = -v.y;

        if (droppedPlatform != null && Time.time >= dropUntil)
        {
            Physics2D.IgnoreCollision(body, droppedPlatform, false);
            droppedPlatform = null;
        }

        Collider2D ground = FindGround();
        Vector2 platformVelocity = Vector2.zero;
        if (ground != null && ground.TryGetComponent(out MovingPlatform platform)) platformVelocity = platform.Velocity;
        // Đang bay lên (so với bục dưới chân) thì không tính là đứng trên đất.
        IsGrounded = ground != null && v.y - platformVelocity.y <= 0.1f;
        GroundCollider = IsGrounded ? ground : null;
        if (IsGrounded && !wasGrounded && controlEnabled) Landed?.Invoke(Mathf.Max(0f, fallSpeed));
        if (IsGrounded)
        {
            coyoteCounter = coyoteTime;
            jumping = false;
            airJumpsLeft = airJumps;
        }
        else
        {
            coyoteCounter -= dt;
        }
        IsGliding = false;
        if (!controlEnabled) return;

        if (overridden)
        {
            rb.gravityScale = overrideGravity;
            return;
        }

        if (stunCounter > 0f)
        {
            // Đang bị đẩy lùi: để vận tốc tự nhiên, chỉ áp trọng lực và giới hạn tốc độ rơi.
            stunCounter -= dt;
            v.y = Mathf.Max(v.y, -maxFallSpeed);
            rb.linearVelocity = v;
            rb.gravityScale = v.y < 0f ? gravityScale * fallGravityMultiplier : gravityScale;
            return;
        }

        // Ngang: tăng tốc tới tốc độ mục tiêu (tính so với bục đang đứng), giảm tốc riêng khi thả phím hoặc đổi hướng.
        IsRunning = inputX != 0f && sprintHeld;
        float targetSpeed = inputX * (IsRunning ? runSpeed : walkSpeed);
        float relativeX = v.x - (IsGrounded ? platformVelocity.x : 0f);
        bool slowingDown = Mathf.Abs(targetSpeed) < Mathf.Abs(relativeX) || targetSpeed * relativeX < 0f;
        float accel = !IsGrounded ? airAcceleration : slowingDown ? groundDeceleration : groundAcceleration;
        relativeX = Mathf.MoveTowards(relativeX, targetSpeed, accel * dt);
        v.x = relativeX + (IsGrounded ? platformVelocity.x : 0f);
        if (IsGrounded && !jumping && platformVelocity.y < 0f) v.y = platformVelocity.y;   // bục đi xuống: bám theo

        if (jumpBufferCounter > 0f && IsGrounded && inputY < -0.5f && TryDropThrough(ground))
        {
            jumpBufferCounter = 0f;
        }
        else if (jumpBufferCounter > 0f && coyoteCounter > 0f && !jumping)
        {
            // Nhảy thường: đang đất (hoặc vừa rời mép trong coyote time) và chưa nhảy.
            v.y = JumpVelocity + Mathf.Max(0f, platformVelocity.y);
            StartJump();
            Jumped?.Invoke(false);
        }
        else if (jumpBufferCounter > 0f && coyoteCounter <= 0f && airJumpsLeft > 0)
        {
            v.y = JumpVelocity;
            airJumpsLeft--;
            StartJump();
            Jumped?.Invoke(true);
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

        float fallLimit = maxFallSpeed;
        if (glideFallSpeed > 0f && jumpHeld && v.y < 0f && !IsGrounded)
        {
            IsGliding = true;
            fallLimit = glideFallSpeed;
        }
        v.y = Mathf.Max(v.y, -fallLimit);
        rb.linearVelocity = v;
        float g = v.y < 0f ? gravityScale * fallGravityMultiplier : gravityScale;
        if (!IsGrounded && jumping && jumpHeld && Mathf.Abs(v.y) < apexThreshold) g = gravityScale * apexGravityMultiplier;
        rb.gravityScale = g;
    }

    void StartJump()
    {
        jumping = true;
        IsGrounded = false;   // báo ngay cho camera/Animator, không đợi bước vật lý sau
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
        jumpCutRequested = false;
    }

    bool TryDropThrough(Collider2D ground)
    {
        if (ground == null || !ground.TryGetComponent(out PlatformEffector2D effector) || !effector.useOneWay) return false;
        Physics2D.IgnoreCollision(body, ground, true);
        droppedPlatform = ground;
        dropUntil = Time.time + dropThroughTime;
        IsGrounded = false;
        coyoteCounter = 0f;
        return true;
    }

    Collider2D FindGround()
    {
        GetGroundBox(out Vector2 center, out Vector2 size);
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(groundLayer);
        int count = Physics2D.OverlapBox(center, size, 0f, filter, groundHits);
        for (int i = 0; i < count; i++)
            if (groundHits[i] != droppedPlatform) return groundHits[i];
        return null;
    }

    void GetGroundBox(out Vector2 center, out Vector2 size)
    {
        Bounds b = body.bounds;
        center = new Vector2(b.center.x, b.min.y - groundCheckDepth / 2f);
        size = new Vector2(b.size.x * 0.9f, groundCheckDepth);
    }

    public void SetFacing(float direction)
    {
        FacingDirection = direction >= 0f ? 1f : -1f;
        if (facing != null) facing.localScale = new Vector3(FacingDirection, 1f, 1f);
    }

    void OnValidate()
    {
        runSpeed = Mathf.Max(runSpeed, walkSpeed);
        if (rb != null && Application.isPlaying && !overridden) rb.gravityScale = gravityScale;
    }

    void OnDrawGizmosSelected()
    {
        if (body == null) body = GetComponent<CapsuleCollider2D>();
        GetGroundBox(out Vector2 center, out Vector2 size);
        Gizmos.color = Application.isPlaying && IsGrounded ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(center, size);
    }
}
