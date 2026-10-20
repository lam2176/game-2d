using System;
using UnityEngine;

/// <summary>
/// Boss Hồ Tinh (cáo chín đuôi — truyền thuyết Lạc Long Quân diệt Hồ Tinh ở Hồ Tây).
/// Nhịp đánh kiểu Hollow Knight / Cuphead: mỗi đòn có báo hiệu rõ ràng rồi mới ra, giữa các đòn có khoảng nghỉ để phản công.
///   • Lao vồ: cúi thấp + đuôi rực sáng → lao ngang qua đấu trường tới khi chạm tường.
///   • Nhảy đập: bật cao về phía người chơi → đáp xuống tạo 2 sóng chấn chạy trên mặt đất (nhảy qua để né).
///   • Hồ hoả: đứng niệm chú → bắn chùm lửa hình quạt về phía người chơi.
///   • Giai đoạn 2 (≤ 50% máu): nhanh hơn, chùm lửa dày hơn, gọi thêm Ma Trơi.
/// Chỉ hoạt động sau khi <see cref="Activate"/> (người chơi bước vào đấu trường).
/// </summary>
public class BossFoxSpirit : EnemyBase
{
    enum State { Dormant, Idle, DashWindup, Dash, LeapWindup, Leap, CastWindup, Summon }

    public string bossName = "Hồ Tinh";

    [Header("Nhịp đánh")]
    public float idleTime = 0.9f;
    [Tooltip("Hệ số tốc độ ở giai đoạn 2 (nghỉ ngắn hơn, báo hiệu nhanh hơn).")]
    public float phase2Speedup = 1.35f;

    [Header("Lao vồ")]
    public float dashWindup = 0.6f;
    public float dashSpeed = 15f;
    public float dashMaxTime = 1.4f;

    [Header("Nhảy đập")]
    public float leapWindup = 0.45f;
    public float leapHeight = 6f;
    public float shockwaveSpeed = 9f;
    public float shockwaveRange = 12f;

    [Header("Hồ hoả")]
    public float castWindup = 0.7f;
    public int fireCount = 5;
    public int fireCountPhase2 = 7;
    public float fireSpread = 60f;
    public float fireSpeed = 7f;
    public Color fireColor = new Color(0.55f, 0.85f, 1f);

    [Header("Gọi Ma Trơi (giai đoạn 2)")]
    public GameObject minionPrefab;
    public int minionCount = 2;

    [Header("Hiển thị")]
    [Tooltip("Các đuôi — sáng lên khi báo hiệu đòn.")]
    public SpriteRenderer[] tails;
    public Color tailIdle = new Color(1f, 0.85f, 0.6f);
    public Color tailCharge = new Color(0.6f, 0.9f, 1f);

    State state = State.Dormant;
    float stateEnd;
    int lastAttack = -1;
    bool phase2, summoned;
    float leapLandAfter;

    public bool IsActive => state != State.Dormant;
    public bool IsPhase2 => phase2;
    public string CurrentState => state.ToString();
    public event Action PhaseChanged;

    float Speed => phase2 ? phase2Speedup : 1f;

    public void Activate()
    {
        if (state != State.Dormant || health.IsDead) return;
        EnterIdle(1.2f);
        Roar();
    }

    protected override void Think()
    {
        if (!phase2 && health.Current <= health.maxHealth / 2)
        {
            phase2 = true;
            ZodiacFx.Ring(body.bounds.center, 5f, tailCharge, 0.6f);
            Roar();
            PhaseChanged?.Invoke();
        }

        switch (state)
        {
            case State.Dormant:
                SetHorizontalVelocity(0f);
                FacePlayer();
                break;
            case State.Idle:
                SetHorizontalVelocity(0f);
                FacePlayer();
                if (Time.time >= stateEnd && IsGrounded()) ChooseAttack();
                break;
            case State.DashWindup:
                SetHorizontalVelocity(0f);
                if (Time.time >= stateEnd)
                {
                    state = State.Dash;
                    stateEnd = Time.time + dashMaxTime;
                }
                break;
            case State.Dash:
                SetHorizontalVelocity(direction * dashSpeed * Mathf.Sqrt(Speed));
                if (Time.time >= stateEnd || WallAhead()) EnterIdle(idleTime);
                break;
            case State.LeapWindup:
                SetHorizontalVelocity(0f);
                if (Time.time >= stateEnd) StartLeap();
                break;
            case State.Leap:
                if (Time.time >= leapLandAfter && IsGrounded() && rb.linearVelocity.y <= 0.01f) Land();
                break;
            case State.CastWindup:
                SetHorizontalVelocity(0f);
                FacePlayer();
                if (Time.time >= stateEnd)
                {
                    CastFire();
                    EnterIdle(idleTime * 0.8f);
                }
                break;
            case State.Summon:
                SetHorizontalVelocity(0f);
                if (Time.time >= stateEnd)
                {
                    SummonMinions();
                    EnterIdle(idleTime);
                }
                break;
        }
    }

    void ChooseAttack()
    {
        if (phase2 && !summoned && minionPrefab != null)
        {
            summoned = true;
            state = State.Summon;
            stateEnd = Time.time + 0.8f;
            return;
        }
        // Không lặp lại đòn vừa dùng — người chơi đoán được nhịp nhưng không quá đơn điệu.
        int attack;
        do attack = UnityEngine.Random.Range(0, 3); while (attack == lastAttack);
        lastAttack = attack;
        FacePlayer();
        switch (attack)
        {
            case 0: state = State.DashWindup; stateEnd = Time.time + dashWindup / Speed; break;
            case 1: state = State.LeapWindup; stateEnd = Time.time + leapWindup / Speed; break;
            default: state = State.CastWindup; stateEnd = Time.time + castWindup / Speed; break;
        }
    }

    void EnterIdle(float seconds)
    {
        state = State.Idle;
        stateEnd = Time.time + seconds / Speed;
    }

    void FacePlayer()
    {
        if (player != null) direction = Mathf.Sign(player.transform.position.x - transform.position.x);
    }

    bool WallAhead()
    {
        Bounds b = body.bounds;
        float frontX = direction > 0f ? b.max.x : b.min.x;
        return Physics2D.Raycast(new Vector2(frontX, b.center.y), Vector2.right * direction, 0.2f, groundLayer);
    }

    void StartLeap()
    {
        float g = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
        float vy = Mathf.Sqrt(2f * g * leapHeight);
        float airTime = 2f * vy / g;
        float targetX = player != null ? player.transform.position.x : transform.position.x;
        float vx = Mathf.Clamp((targetX - transform.position.x) / airTime, -12f, 12f);
        rb.linearVelocity = new Vector2(vx, vy);
        state = State.Leap;
        leapLandAfter = Time.time + 0.2f;
    }

    void Land()
    {
        SetHorizontalVelocity(0f);
        Vector2 feet = new Vector2(transform.position.x, transform.position.y + 0.4f);
        ZodiacFx.Ring(feet, 3f, tailCharge, 0.35f);
        ZodiacParticles.Dust(feet, Vector2.left, 12);
        ZodiacParticles.Dust(feet, Vector2.right, 12);
        ZodiacParticles.Debris(feet, new Color(0.45f, 0.4f, 0.5f), 10, 1f);
        GameFeel.Shake(0.55f);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.bossSlam, 1f, 0.05f, feet);
        int count = phase2 ? 2 : 1;
        for (int i = 0; i < count; i++)
        {
            // Giai đoạn 2: thêm cặp sóng thứ hai chậm hơn — phải nhảy hai nhịp.
            float speed = shockwaveSpeed * (i == 0 ? 1f : 0.6f);
            foreach (float dir in new[] { -1f, 1f })
            {
                var wave = Projectile.Spawn(feet + new Vector2(dir * 0.8f, 0f), new Vector2(dir * speed, 0f), 0.9f, tailCharge,
                                            playerLayer, groundLayer, 1, true);
                wave.range = shockwaveRange;
                wave.transform.localScale = new Vector3(0.6f, 1.1f, 1f);
            }
        }
        EnterIdle(idleTime);
    }

    void CastFire()
    {
        if (player == null) return;
        Vector2 origin = (Vector2)body.bounds.center + Vector2.up * 0.6f;
        Vector2 aim = (PlayerCenter - origin).normalized;
        int count = phase2 ? fireCountPhase2 : fireCount;
        for (int i = 0; i < count; i++)
        {
            float a = count == 1 ? 0f : Mathf.Lerp(-fireSpread / 2f, fireSpread / 2f, i / (float)(count - 1));
            Vector2 dir = Quaternion.Euler(0f, 0f, a) * aim;
            var shot = Projectile.Spawn(origin, dir * fireSpeed, 0.5f, fireColor, playerLayer, groundLayer, 1, true);
            shot.range = 16f;
        }
        ZodiacFx.Ring(origin, 1.2f, fireColor, 0.25f);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.fireball, 0.9f, 0.1f, origin);
    }

    void Roar()
    {
        GameFeel.Shake(0.4f);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.bossRoar, 1f, 0f, transform.position);
    }

    void SummonMinions()
    {
        for (int i = 0; i < minionCount; i++)
        {
            Vector2 at = (Vector2)transform.position + new Vector2((i % 2 == 0 ? -1f : 1f) * (2.5f + i), 4f);
            var minion = Instantiate(minionPrefab, at, Quaternion.identity);
            minion.name = "Ma Trơi (gọi ra)";
            ZodiacFx.Ring(at, 1.2f, fireColor, 0.3f);
        }
    }

    protected override void Interrupt()
    {
        // Boss không bị ngắt đòn bởi đòn đánh thường; choáng do chiêu chỉ huỷ phần báo hiệu.
        if (state == State.DashWindup || state == State.LeapWindup || state == State.CastWindup) EnterIdle(idleTime * 0.5f);
    }

    protected override void UpdateVisual()
    {
        if (visual == null) return;
        bool winding = state == State.DashWindup || state == State.LeapWindup || state == State.CastWindup || state == State.Summon;
        Vector3 s = state switch
        {
            State.DashWindup => new Vector3(1.2f, 0.8f, 1f),
            State.LeapWindup => new Vector3(1.15f, 0.75f, 1f),
            State.Dash => new Vector3(1.25f, 0.85f, 1f),
            State.Leap => new Vector3(0.9f, 1.15f, 1f),
            _ => Vector3.one,
        };
        SetVisualScale(new Vector3(s.x * direction, s.y, 1f));
        if (tails == null) return;
        float flicker = winding ? 0.5f + 0.5f * Mathf.Sin(Time.time * 25f) : 0f;
        for (int i = 0; i < tails.Length; i++)
        {
            if (tails[i] == null) continue;
            tails[i].color = Color.Lerp(tailIdle, tailCharge, winding ? 0.5f + flicker * 0.5f : (phase2 ? 0.35f : 0f));
            float sway = Mathf.Sin(Time.time * 3f + i * 0.7f) * 6f;
            tails[i].transform.localRotation = Quaternion.Euler(0f, 0f, (i - (tails.Length - 1) / 2f) * 14f + sway);
        }
    }
}
