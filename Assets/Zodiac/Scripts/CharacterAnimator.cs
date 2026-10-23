using UnityEngine;

/// <summary>
/// Animation bằng code cho nhân vật chỉ có một ảnh tĩnh (chưa có khung hình vẽ tay):
/// đặt trên object Visual (pivot ở chân), mỗi frame tính vị trí/góc/tỉ lệ từ trạng thái thật của nhân vật.
///   • Nhịp bước khớp quãng đường đi được (không "trượt" khi chạy nhanh/chậm), nghiêng người theo gia tốc.
///   • Lò xo co giãn: bật nhảy kéo dài người, chạm đất bẹp xuống theo tốc độ rơi rồi nảy lại (squash &amp; stretch).
///   • Vung người theo đòn đánh (đòn kết / đòn lao mạnh hơn, đá bay chúi người), giật lùi khi bị thương,
///     ngã nằm khi bị quật ngã, lộn vòng khi nhảy đôi, chao lượn khi lượn, kéo dài người khi lướt/dậm đất.
///   • Tiếng bước chân theo mặt đất + bụi chân, tiếng/bụi khi nhảy và đáp.
/// Nguyên tắc 12 của Disney (squash &amp; stretch, anticipation, follow-through) áp dụng cho một tấm ảnh duy nhất.
/// </summary>
public class CharacterAnimator : MonoBehaviour
{
    [Header("Bước")]
    [Tooltip("Quãng đường một bước chân (unit).")]
    public float strideLength = 1.1f;
    public float walkBob = 0.06f;
    public float runBob = 0.12f;
    public float stepTilt = 3f;

    [Header("Nghiêng")]
    [Tooltip("Góc nghiêng về trước tối đa khi chạy hết tốc (độ).")]
    public float runLean = 9f;
    public float leanSmoothing = 12f;

    [Header("Lò xo co giãn")]
    public float springStiffness = 320f;
    [Range(0.05f, 1f)] public float springDamping = 0.35f;

    ZodiacPlayerController player;
    PlayerMeleeAttack attack;
    PlayerSkill skill;
    PlayerDodge dodge;
    Health health;
    Transform t;

    float stridePhase;
    int lastStep;
    float lean;
    Vector2 squash, squashVelocity;   // độ lệch tỉ lệ (x, y) quanh 1
    float kickAngle, kickVelocity;     // góc vung người (lò xo)
    float spin, spinSpeed;             // lộn vòng khi nhảy đôi
    float lastVx;
    float down;                        // 0 = đứng, 1 = nằm hẳn (bị quật ngã)

    void Awake()
    {
        t = transform;
        player = GetComponentInParent<ZodiacPlayerController>();
        attack = GetComponentInParent<PlayerMeleeAttack>();
        skill = GetComponentInParent<PlayerSkill>();
        dodge = GetComponentInParent<PlayerDodge>();
        health = GetComponentInParent<Health>();
    }

    void OnEnable()
    {
        if (player != null) { player.Jumped += OnJumped; player.Landed += OnLanded; }
        if (attack != null) attack.Swung += OnSwung;
        if (skill != null) skill.Used += OnSkill;
        if (dodge != null) dodge.Dodged += OnDodged;
        if (health != null) health.Damaged += OnDamaged;
    }

    void OnDisable()
    {
        if (player != null) { player.Jumped -= OnJumped; player.Landed -= OnLanded; }
        if (attack != null) attack.Swung -= OnSwung;
        if (skill != null) skill.Used -= OnSkill;
        if (dodge != null) dodge.Dodged -= OnDodged;
        if (health != null) health.Damaged -= OnDamaged;
    }

    // ───────── Sự kiện ─────────

    void OnJumped(bool airJump)
    {
        Impulse(new Vector2(-2.5f, 3.5f));
        var r = ZodiacResources.Get;
        if (r != null) ZodiacAudio.Play(r.jump, 0.5f, 0.12f);
        if (airJump)
        {
            spinSpeed = -1080f;   // lộn một vòng (360°) trong ~0.33s
            spin = 0f;
            ZodiacParticles.Sparkle(Feet, player.Character != null ? player.Character.themeColor : Color.white, 8, 0.3f);
        }
        else ZodiacParticles.Dust(Feet, Vector3.zero, 6);
    }

    void OnLanded(float speed)
    {
        if (speed < 2f) return;
        float k = Mathf.Clamp01(speed / 20f);
        Impulse(new Vector2(3f, -4.5f) * (0.4f + k));
        ZodiacParticles.Dust(Feet, Vector3.left, Mathf.RoundToInt(3 + k * 6));
        ZodiacParticles.Dust(Feet, Vector3.right, Mathf.RoundToInt(3 + k * 6));
        var r = ZodiacResources.Get;
        if (r != null) ZodiacAudio.Play(r.land, 0.35f + 0.5f * k, 0.1f);
        if (speed > 17f) GameFeel.Shake(0.12f);
    }

    void OnSwung(int step, PlayerMeleeAttack.Kind kind)
    {
        if (kind == PlayerMeleeAttack.Kind.Air) Kick(-30f);
        else
        {
            Kick(step == 2 ? -26f : -15f);
            Impulse(new Vector2(step == 2 ? 2.8f : 1.6f, -1.2f));
        }
    }

    void OnSkill(SkillType type)
    {
        switch (type)
        {
            case SkillType.Dash: Impulse(new Vector2(5f, -3.5f)); break;
            case SkillType.Projectile: Kick(10f); Impulse(new Vector2(-1.5f, 1.5f)); break;
            case SkillType.Burst: Impulse(new Vector2(4f, 3f)); break;
            case SkillType.Heal: Impulse(new Vector2(-1.5f, 2f)); break;
            case SkillType.GroundPound: Impulse(new Vector2(-2f, 3f)); break;
        }
    }

    /// <summary>Lăn: lộn một vòng về phía trước, người thu nhỏ lại.</summary>
    void OnDodged(float dir)
    {
        spin = 0f;
        spinSpeed = -360f / Mathf.Max(0.1f, dodge.duration);
        Impulse(new Vector2(2f, -3f));
    }

    void OnDamaged(Hit hit)
    {
        Kick(18f);
        Impulse(new Vector2(-2f, 2.5f));
    }

    void Impulse(Vector2 v) => squashVelocity += v;
    void Kick(float degreesPerSecondScaled) => kickVelocity += degreesPerSecondScaled * 18f;

    Vector3 Feet => player != null ? player.transform.position + Vector3.up * 0.05f : t.position;

    // ───────── Mỗi frame ─────────

    void LateUpdate()
    {
        if (player == null) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        Vector3 v = player.Velocity;
        float speed = new Vector2(v.x, v.z).magnitude;   // tốc độ trên mặt đường (đổi làn cũng tính bước chân)
        var c = player.Character;
        float runSpeed = c != null ? Mathf.Max(1f, c.runSpeed) : 8.5f;

        // Tư thế mục tiêu theo trạng thái
        Vector2 targetSquash = Vector2.zero;
        float targetLean = 0f, bob = 0f, wobble = 0f;

        if (dodge != null && dodge.IsDodging)
        {
            targetSquash = new Vector2(-0.12f, -0.22f);   // cuộn tròn người
        }
        else if (player.IsOverridden && skill != null && skill.IsBusy)
        {
            // Đang lướt / dậm đất: kéo dài theo hướng chuyển động
            if (Mathf.Abs(v.y) > Mathf.Abs(v.x)) targetSquash = new Vector2(-0.18f, 0.25f);
            else if (speed > 4f) { targetSquash = new Vector2(0.3f, -0.2f); targetLean = -14f; }
        }
        else if (player.IsGrounded)
        {
            if (speed > 0.3f)
            {
                stridePhase += speed * dt / strideLength;
                float run = Mathf.InverseLerp(c != null ? c.walkSpeed : 5f, runSpeed, speed);
                bob = Mathf.Abs(Mathf.Sin(stridePhase * Mathf.PI)) * Mathf.Lerp(walkBob, runBob, run);
                wobble = Mathf.Sin(stridePhase * Mathf.PI) * stepTilt * (1f + run);
                targetLean = -runLean * Mathf.Clamp01(speed / runSpeed);
                targetSquash = new Vector2(0f, -0.04f * run);
                int step = Mathf.FloorToInt(stridePhase);
                if (step != lastStep) { lastStep = step; Footstep(run); }
            }
            else
            {
                // Thở khi đứng yên
                float breath = Mathf.Sin(Time.time * 2.4f);
                targetSquash = new Vector2(-0.012f * breath, 0.022f * breath);
            }
            // Đổi hướng khi đang chạy nhanh: trượt bụi
            if (Mathf.Abs(lastVx) > runSpeed * 0.6f && Mathf.Sign(lastVx) != Mathf.Sign(v.x) && speed > 0.5f)
                ZodiacParticles.Dust(Feet, new Vector3(Mathf.Sign(lastVx), 0f, 0f), 6);
        }
        else
        {
            // Trên không: kéo dài khi bay lên, bẹp nhẹ khi rơi, ngửa nhẹ khi lên
            float up = Mathf.Clamp(v.y / 14f, -1f, 1f);
            targetSquash = up > 0f ? new Vector2(-0.08f * up, 0.14f * up) : new Vector2(0.05f * -up, -0.03f * -up);
            targetLean = up * 6f - Mathf.Clamp01(speed / runSpeed) * 4f;
            if (player.IsGliding)
            {
                targetSquash = new Vector2(0.08f, -0.04f);
                wobble = Mathf.Sin(Time.time * 4f) * 6f;
                targetLean = -8f;
            }
        }
        lastVx = v.x;

        // Lò xo tỉ lệ (semi-implicit Euler)
        float damping = 2f * Mathf.Sqrt(springStiffness) * springDamping;
        squashVelocity += ((targetSquash - squash) * springStiffness - squashVelocity * damping) * dt;
        squash += squashVelocity * dt;
        squash = Vector2.ClampMagnitude(squash, 0.45f);

        kickVelocity += (-kickAngle * springStiffness * 0.8f - kickVelocity * damping) * dt;
        kickAngle += kickVelocity * dt;

        if (spinSpeed != 0f)
        {
            spin += spinSpeed * dt;
            if (Mathf.Abs(spin) >= 360f) { spin = 0f; spinSpeed = 0f; }
        }

        lean = Mathf.Lerp(lean, targetLean, 1f - Mathf.Exp(-leanSmoothing * dt));
        // Bị quật ngã: ngả ra sau nằm hẳn xuống đất, hết thời gian thì bật dậy
        down = Mathf.MoveTowards(down, player.IsKnockedDown ? 1f : 0f, dt * (player.IsKnockedDown ? 5f : 3.5f));

        // Nhún bước chân bằng co giãn thân (pivot ở bàn chân nên chân luôn bám đất), không nhấc cả người lên
        float stretch = bob / Mathf.Max(1f, c != null ? c.colliderSize.y : 1.6f);
        // Giữ thể tích: kéo dài trục này thì trục kia co lại một phần
        float sx = 1f + squash.x - squash.y * 0.5f - stretch * 0.5f;
        float sy = 1f + squash.y - squash.x * 0.5f + stretch;
        t.localScale = new Vector3(Mathf.Max(0.5f, sx), Mathf.Max(0.5f, sy), 1f);
        t.localRotation = Quaternion.Euler(0f, 0f, lean + wobble + kickAngle + spin + down * 82f);
        // Khi lộn vòng thì xoay quanh giữa thân, không quanh chân
        float halfHeight = c != null ? c.colliderSize.y * 0.5f : 1f;
        Vector3 spinOffset = spin != 0f ? (Vector3)(Vector2.up * halfHeight - (Vector2)(Quaternion.Euler(0, 0, spin) * (Vector2.up * halfHeight))) : Vector3.zero;
        t.localPosition = new Vector3(0f, -FootSink, 0f) + spinOffset;
    }

    /// <summary>Ấn bàn chân ngập nhẹ vào mặt đường — tránh khe sáng nhỏ giữa chân và đất trông như lơ lửng.</summary>
    const float FootSink = 0.03f;

    void Footstep(float run)
    {
        var r = ZodiacResources.Get;
        if (r != null)
        {
            var group = r.footstepGrass;
            if (ZodiacGameManager.Instance != null && ZodiacGameManager.Instance.stoneFootsteps) group = r.footstepStone;
            ZodiacAudio.Play(group, Mathf.Lerp(0.25f, 0.45f, run), 0.1f);
        }
        if (run > 0.5f) ZodiacParticles.Dust(Feet, new Vector3(-player.FacingDirection, 0f, 0f), 2);
    }
}
