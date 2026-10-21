using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Điều khiển beat 'em up 2.5D: ← → đi dọc con đường (trục X), ↑ ↓ đổi làn (trục Z, chậm hơn như Streets of Rage),
/// giữ Shift chạy, Space nhảy (thả sớm thì nhảy thấp). Input đọc trong Update, vật lý (Rigidbody 3D) xử lý trong FixedUpdate.
/// Nhảy khi đang tiếp đất (cộng coyote time và jump buffer), nhảy thêm trên không nếu nhân vật có airJumps,
/// giữ Space khi rơi để lượn nếu có glideFallSpeed. Trọng lực tự tính (rơi nhanh hơn lúc lên, lơ lửng nhẹ ở đỉnh).
///
/// Nhân vật lấy từ <see cref="roster"/> theo lựa chọn ở màn hình chính (dự phòng: <see cref="character"/>);
/// chỉ số tốc độ/nhảy của asset nhân vật ghi đè các ô bên dưới khi vào game. Máu, Khí, công đi qua <see cref="CharacterStats"/>.
/// Chiêu thức (PlayerSkill) và né (PlayerDodge) tạm chiếm quyền điều khiển vận tốc bằng <see cref="SetOverride"/>.
/// Trúng đòn quật ngã thì ngã nằm đất (bất tử) rồi bật dậy — <see cref="IsKnockedDown"/>.
/// Animation do <see cref="CharacterAnimator"/> tính bằng code từ trạng thái và các sự kiện <see cref="Jumped"/>, <see cref="Landed"/>.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
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
    [Tooltip("Tốc độ đổi làn (trục Z) so với tốc độ đi ngang.")]
    [Range(0.2f, 1f)] public float depthSpeedFactor = 0.7f;
    [Tooltip("Tăng tốc trên mặt đất (unit/s²).")]
    [Min(0f)] public float groundAcceleration = 60f;
    [Tooltip("Giảm tốc trên mặt đất khi thả phím hoặc đổi hướng (unit/s²).")]
    [Min(0f)] public float groundDeceleration = 80f;
    [Tooltip("Tăng/giảm tốc khi đang trên không (unit/s²).")]
    [Min(0f)] public float airAcceleration = 30f;

    [Header("Nhảy")]
    [Tooltip("Độ cao đỉnh cú nhảy (unit) khi giữ Space.")]
    [Min(0f)] public float jumpHeight = 3.2f;
    [Tooltip("Nhân với độ cao nhảy của nhân vật (bảng số liệu viết cho platformer — beat 'em up nhảy thấp hơn).")]
    [Min(0f)] public float jumpHeightScale = 0.6f;
    [Tooltip("Thả Space khi đang bay lên thì vận tốc lên nhân hệ số này (nhảy thấp).")]
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;
    [Min(0f)] public float coyoteTime = 0.08f;
    [Min(0f)] public float jumpBufferTime = 0.12f;
    [Tooltip("Số lần nhảy thêm trên không.")]
    [Min(0)] public int airJumps = 0;
    [Tooltip("Giữ Space khi rơi thì tốc độ rơi tối đa bằng giá trị này. 0 = không lượn.")]
    [Min(0f)] public float glideFallSpeed = 0f;

    [Header("Trọng lực")]
    [Tooltip("Nhân với trọng lực Trái Đất (9.81).")]
    [Min(0f)] public float gravityScale = 3f;
    [Min(1f)] public float fallGravityMultiplier = 1.6f;
    [Min(1f)] public float maxFallSpeed = 20f;
    [Min(0f)] public float apexThreshold = 2f;
    [Range(0.1f, 1f)] public float apexGravityMultiplier = 0.55f;

    [Header("Bị đánh (cần component Health)")]
    [Tooltip("Vận tốc bị đẩy ra xa nguồn sát thương (x: ngang, y: bật lên).")]
    public Vector2 hurtKnockback = new Vector2(5f, 3f);
    [Min(0f)] public float hurtStunTime = 0.25f;
    [Tooltip("Thời gian nằm đất khi bị quật ngã (giây) — bất tử trong lúc nằm và lúc đứng dậy.")]
    [Min(0f)] public float knockdownTime = 0.8f;
    [Range(0f, 1f)] public float knockbackResistance = 0f;

    [Header("Kiểm tra tiếp đất")]
    public LayerMask groundLayer;
    [Min(0.01f)] public float groundCheckDepth = 0.08f;

    [Header("Hiển thị")]
    [Tooltip("Object con được lật trái/phải; ảnh nằm ở object con của nó.")]
    public Transform facing;
    public SpriteRenderer spriteRenderer;

    /// <summary>Người chơi duy nhất trong màn (quái, đồng xu, camera dùng).</summary>
    public static ZodiacPlayerController Current { get; private set; }

    const float EarthGravity = 9.81f;

    Rigidbody rb;
    CapsuleCollider body;
    Health health;
    InputAction moveAction, jumpAction, sprintAction;

    Vector2 move;
    bool sprintHeld, jumpHeld, jumpCutRequested;
    float coyoteCounter, jumpBufferCounter;
    bool jumping;
    int airJumpsLeft;
    bool controlEnabled = true;
    float stunCounter;
    bool overridden;
    float overrideGravity;
    float knockdownUntil;

    /// <summary>Vừa bật nhảy (true = nhảy trên không).</summary>
    public event Action<bool> Jumped;
    /// <summary>Vừa chạm đất, kèm tốc độ rơi lúc chạm (unit/s, dương).</summary>
    public event Action<float> Landed;

    public bool ControlEnabled => controlEnabled;
    public bool IsGrounded { get; private set; }
    public bool IsRunning { get; private set; }
    public bool IsGliding { get; private set; }
    /// <summary>Đang nằm đất sau khi bị quật ngã.</summary>
    public bool IsKnockedDown => Time.time < knockdownUntil;
    /// <summary>Đang tàng hình — quái không phát hiện.</summary>
    public bool IsHidden { get; set; }
    /// <summary>1 = nhìn phải, -1 = nhìn trái.</summary>
    public float FacingDirection { get; private set; } = 1f;
    public ZodiacCharacter Character => character;
    public Rigidbody Body => rb;
    public CapsuleCollider BodyCollider => body;
    public Vector3 Velocity => rb.linearVelocity;
    public bool IsOverridden => overridden;
    /// <summary>Phím di chuyển (x: ← →, y: ↑ ↓ = vào sâu / ra ngoài).</summary>
    public Vector2 MoveInput => move;
    /// <summary>Tâm thân người (để quái nhắm, hiệu ứng).</summary>
    public Vector3 BodyCenter => body.bounds.center;

    /// <summary>Vận tốc nhảy ban đầu để đạt đúng độ cao nhảy với bước vật lý hiện tại.</summary>
    public float JumpVelocity
    {
        get
        {
            float g = EarthGravity * gravityScale;
            float half = g * Time.fixedDeltaTime * 0.5f;   // bù sai số tích phân từng bước
            return half + Mathf.Sqrt(half * half + 2f * g * jumpHeight * jumpHeightScale);
        }
    }

    public static ZodiacPlayerController FromCollider(Collider other) =>
        other != null && other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<ZodiacPlayerController>() : null;

    void Awake()
    {
        Current = this;
        rb = GetComponent<Rigidbody>();
        body = GetComponent<CapsuleCollider>();
        health = GetComponent<Health>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        if (roster != null && roster.Count > 0) character = roster.Get(ZodiacSelection.Index);
        ApplyCharacter();
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
        body.direction = 1;
        body.radius = Mathf.Max(0.2f, character.colliderSize.x * 0.5f);
        body.height = Mathf.Max(body.radius * 2f, character.colliderSize.y);
        body.center = new Vector3(0f, body.height * 0.5f, 0f);
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
    public void ApplyKnockback(Vector3 velocity, float stunTime)
    {
        if (!controlEnabled || overridden) return;
        rb.linearVelocity = velocity;
        stunCounter = Mathf.Max(stunCounter, stunTime);
        jumping = false;
        jumpBufferCounter = 0f;
        if (velocity.y > 0.1f) IsGrounded = false;
    }

    /// <summary>Bật nảy lên (đòn trên không trúng quái). Giữ Space thì nảy hết cỡ, thả ra thì bị cắt như cú nhảy thường.</summary>
    public void Bounce(float velocity)
    {
        if (!controlEnabled) return;
        var v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(v.x, velocity, v.z);
        jumping = true;
        IsGrounded = false;
        coyoteCounter = 0f;
        jumpBufferCounter = 0f;
    }

    /// <summary>
    /// Chiêu thức chiếm quyền điều khiển: controller không đổi vận tốc, chỉ áp trọng lực theo hệ số gravity.
    /// Gọi lại với active = false để trả quyền.
    /// </summary>
    public void SetOverride(bool active, float gravity = 0f)
    {
        overridden = active;
        overrideGravity = gravity;
        stunCounter = 0f;
    }

    /// <summary>Dịch chuyển tức thời (hồi sinh tại đèn lồng).</summary>
    public void Teleport(Vector3 position)
    {
        rb.position = position;
        transform.position = position;
        if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
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
        if (hit.knockback <= 0f || health.IsDead) return;
        Vector3 away = transform.position - hit.source;
        away.y = 0f;
        away = away.sqrMagnitude > 0.0001f ? away.normalized : Vector3.right * -FacingDirection;
        float k = hit.knockback * (1f - knockbackResistance);
        if (hit.knockdown)
        {
            // Quật ngã: văng ra, nằm đất, bất tử cho tới khi đứng dậy hẳn.
            knockdownUntil = Time.time + knockdownTime;
            health.SetInvulnerable(knockdownTime + 0.5f);
            ApplyKnockback(away * hurtKnockback.x * Mathf.Max(k, 0.6f) * 1.3f + Vector3.up * 7f, knockdownTime);
            return;
        }
        ApplyKnockback(away * hurtKnockback.x * k + Vector3.up * hurtKnockback.y * Mathf.Min(k, 1f), hurtStunTime);
    }

    /// <summary>Khoá/mở điều khiển. Khi khoá, nhân vật đứng yên tại chỗ (dùng lúc thắng/thua, chuyển cảnh).</summary>
    public void SetControlEnabled(bool value)
    {
        controlEnabled = value;
        move = Vector2.zero;
        sprintHeld = jumpHeld = jumpCutRequested = false;
        jumpBufferCounter = 0f;
        if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
        rb.isKinematic = !value;
    }

    void Update()
    {
        if (!controlEnabled) return;
        Vector2 raw = moveAction.ReadValue<Vector2>();
        move = new Vector2(Mathf.Abs(raw.x) < 0.1f ? 0f : raw.x, Mathf.Abs(raw.y) < 0.1f ? 0f : raw.y);
        if (move.sqrMagnitude > 1f) move.Normalize();
        sprintHeld = sprintAction.IsPressed();
        jumpHeld = jumpAction.IsPressed();
        if (jumpAction.WasPressedThisFrame() && !IsKnockedDown) jumpBufferCounter = Mathf.Max(jumpBufferTime, Time.fixedDeltaTime);
        if (jumpAction.WasReleasedThisFrame()) jumpCutRequested = true;

        if (!overridden && stunCounter <= 0f)
        {
            if (move.x > 0f) SetFacing(1f);
            else if (move.x < 0f) SetFacing(-1f);
        }
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        Vector3 v = rb.linearVelocity;
        bool wasGrounded = IsGrounded;
        float fallSpeed = -v.y;

        IsGrounded = v.y <= 0.1f && CheckGround();
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
        IsRunning = false;
        if (!controlEnabled) return;

        if (overridden)
        {
            ApplyGravity(ref v, overrideGravity, dt);
            rb.linearVelocity = v;
            return;
        }

        if (stunCounter > 0f || IsKnockedDown)
        {
            // Bị đẩy lùi / nằm đất: vận tốc ngang tắt dần trên đất, chỉ áp trọng lực.
            stunCounter -= dt;
            if (IsGrounded)
            {
                Vector2 flat = Vector2.MoveTowards(new Vector2(v.x, v.z), Vector2.zero, groundDeceleration * 0.35f * dt);
                v.x = flat.x;
                v.z = flat.y;
            }
            ApplyGravity(ref v, v.y < 0f ? gravityScale * fallGravityMultiplier : gravityScale, dt);
            rb.linearVelocity = v;
            return;
        }

        // Mặt phẳng XZ: tăng tốc tới vận tốc mục tiêu, giảm tốc riêng khi thả phím hoặc đổi hướng.
        IsRunning = move.x != 0f && sprintHeld;
        float speed = IsRunning ? runSpeed : walkSpeed;
        Vector2 target = new Vector2(move.x * speed, move.y * walkSpeed * depthSpeedFactor);
        Vector2 current = new Vector2(v.x, v.z);
        bool slowingDown = target.sqrMagnitude < current.sqrMagnitude || Vector2.Dot(target, current) < 0f;
        float accel = !IsGrounded ? airAcceleration : slowingDown ? groundDeceleration : groundAcceleration;
        current = Vector2.MoveTowards(current, target, accel * dt);
        v.x = current.x;
        v.z = current.y;

        if (jumpBufferCounter > 0f && coyoteCounter > 0f && !jumping)
        {
            v.y = JumpVelocity;
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

        float g = v.y < 0f ? gravityScale * fallGravityMultiplier : gravityScale;
        if (!IsGrounded && jumping && jumpHeld && Mathf.Abs(v.y) < apexThreshold) g = gravityScale * apexGravityMultiplier;
        ApplyGravity(ref v, g, dt);

        float fallLimit = maxFallSpeed;
        if (glideFallSpeed > 0f && jumpHeld && v.y < 0f && !IsGrounded)
        {
            IsGliding = true;
            fallLimit = glideFallSpeed;
        }
        v.y = Mathf.Max(v.y, -fallLimit);
        rb.linearVelocity = v;
    }

    void ApplyGravity(ref Vector3 v, float scale, float dt)
    {
        if (IsGrounded && v.y <= 0f) v.y = -1f;   // ép nhẹ xuống đất để không nảy lên khi đi qua mép nối
        else v.y -= EarthGravity * scale * dt;
    }

    void StartJump()
    {
        jumping = true;
        IsGrounded = false;   // báo ngay cho camera/Animator, không đợi bước vật lý sau
        jumpBufferCounter = 0f;
        coyoteCounter = 0f;
        jumpCutRequested = false;
    }

    bool CheckGround()
    {
        float r = body.radius * 0.9f;
        Vector3 origin = transform.position + Vector3.up * (r + 0.05f);
        return Physics.SphereCast(origin, r, Vector3.down, out _, 0.05f + groundCheckDepth, groundLayer, QueryTriggerInteraction.Ignore);
    }

    public void SetFacing(float direction)
    {
        FacingDirection = direction >= 0f ? 1f : -1f;
        if (facing != null) facing.localScale = new Vector3(FacingDirection, 1f, 1f);
    }

    void OnValidate() => runSpeed = Mathf.Max(runSpeed, walkSpeed);

    void OnDrawGizmosSelected()
    {
        if (body == null) body = GetComponent<CapsuleCollider>();
        Gizmos.color = Application.isPlaying && IsGrounded ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * body.radius * 0.9f, body.radius * 0.9f);
    }
}
