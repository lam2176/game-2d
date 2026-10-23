using UnityEngine;

/// <summary>
/// Boss Thuồng Luồng (Đầm Sen) — rắn nước khổng lồ trong truyện cổ, nấp dưới đầm. Mỗi đòn có báo hiệu rồi để lộ sơ hở:
///   • Lặn – trồi: chìm xuống (không đánh được), gợn sóng bám theo người chơi; gợn đứng lại và sáng lên → trồi vọt lên đớp.
///     Né: chạy khỏi vòng / lăn. Trồi xong nằm phơi mình một lúc — lúc để đánh.
///   • Quật đuôi: cuộn mình, đuôi rực sáng → quét sát đất cả một vùng rộng trước mặt. Né: nhảy qua.
///   • Phun nước: ngóc cổ nhắm làn người chơi → chuỗi tia nước bắn dọc làn đó. Né: đổi làn.
///   • Giai đoạn 2 (≤ 50% máu): lặn trồi liền hai lần, phun ba làn, gọi Cóc Tía lên bờ.
/// </summary>
public class BossSerpent : BossBase
{
    enum State { Dormant, Idle, Sink, Hunt, Lock, Rise, Exposed, TailWindup, Spit }

    [Header("Nhịp")]
    public float idleTime = 1.1f;
    public float moveSpeed = 2.4f;
    public float phase2Speedup = 1.3f;

    [Header("Lặn – trồi")]
    public float huntTime = 1.8f;
    public float huntSpeed = 5f;
    public float lockTime = 0.55f;
    public float riseRadius = 1.6f;
    public float exposedTime = 1.6f;

    [Header("Quật đuôi")]
    public float tailWindup = 0.8f;
    public float tailReach = 5f;

    [Header("Phun nước")]
    public float spitWindup = 0.6f;
    public int spitShots = 5;
    public float spitSpeed = 9f;

    [Header("Gọi lính (giai đoạn 2)")]
    public GameObject minionPrefab;
    public int minionCount = 2;

    [Header("Hiển thị")]
    public SpriteRenderer[] glowParts;
    public Color glowIdle = new Color(0.4f, 0.75f, 0.7f);
    public Color glowCharge = new Color(0.6f, 1f, 1f);

    static readonly Color Water = new Color(0.5f, 0.85f, 1f);

    State state = State.Dormant;
    float stateEnd, nextRipple;
    int lastAttack = -1, risesLeft;
    bool phase2, summoned, spitPhase2;
    Vector3 lockAt;

    public override bool IsActive => state != State.Dormant;
    float Speed => phase2 ? phase2Speedup : 1f;
    bool Submerged => state == State.Sink || state == State.Hunt || state == State.Lock;

    protected override void Awake()
    {
        base.Awake();
        knockbackTaken = 0f;
    }

    public override void Activate()
    {
        if (state != State.Dormant || health.IsDead) return;
        EnterIdle(1.2f);
        Roar();
        ZodiacFx.Ring(transform.position, 4f, Water, 0.6f, flat: true);
    }

    protected override void Think()
    {
        if (!phase2 && health.Current <= health.maxHealth / 2)
        {
            phase2 = true;
            Roar(0.5f);
            ZodiacFx.Ring(transform.position, 5f, glowCharge, 0.6f, flat: true);
        }

        switch (state)
        {
            case State.Dormant:
                SetPlanarVelocity(Vector3.zero);
                FacePlayer();
                break;

            case State.Idle:
                FacePlayer();
                if (player != null)
                {
                    float dist = Mathf.Abs(ToPlayer.x);
                    Vector3 v = dist < 3f ? Vector3.right * -direction * moveSpeed : dist > 6f ? Vector3.right * direction * moveSpeed : Vector3.zero;
                    SetPlanarVelocity(BlockedAhead(v) ? Vector3.zero : v);
                }
                if (Time.time >= stateEnd) ChooseAttack();
                break;

            case State.Sink:
                SetPlanarVelocity(Vector3.zero);
                health.SetInvulnerable(0.2f);
                if (Time.time >= stateEnd) { state = State.Hunt; stateEnd = Time.time + huntTime / Speed; }
                break;

            case State.Hunt:
            {
                health.SetInvulnerable(0.2f);
                Vector3 target = PlayerGround();
                Vector3 d = target - transform.position;
                d.y = 0f;
                SetPlanarVelocity(d.magnitude < 0.2f ? Vector3.zero : d.normalized * Mathf.Min(huntSpeed * Speed, d.magnitude * 6f));
                Ripple(0.35f, 1.2f);
                if (Time.time >= stateEnd)
                {
                    state = State.Lock;
                    stateEnd = Time.time + lockTime / Speed;
                    lockAt = transform.position;
                    ZodiacFx.Ring(lockAt + Vector3.up * 0.03f, riseRadius * 2f, new Color(0.6f, 1f, 1f, 0.85f), lockTime / Speed, flat: true);
                }
                break;
            }

            case State.Lock:
                health.SetInvulnerable(0.2f);
                SetPlanarVelocity(Vector3.zero);
                Ripple(0.12f, riseRadius * 1.6f);
                if (Time.time >= stateEnd) Rise();
                break;

            case State.Rise:
                SetPlanarVelocity(Vector3.zero);
                if (Time.time >= stateEnd)
                {
                    if (--risesLeft > 0) { Sink(0.35f); break; }
                    state = State.Exposed;
                    stateEnd = Time.time + exposedTime;
                }
                break;

            case State.Exposed:
                SetPlanarVelocity(Vector3.zero);
                if (Time.time >= stateEnd) EnterIdle(idleTime * 0.6f);
                break;

            case State.TailWindup:
                SetPlanarVelocity(Vector3.zero);
                if (Time.time >= stateEnd) TailSweep();
                break;

            case State.Spit:
                SetPlanarVelocity(Vector3.zero);
                FacePlayer();
                if (Time.time >= stateEnd) SpitWater();
                break;
        }
    }

    void ChooseAttack()
    {
        if (phase2 && !summoned && minionPrefab != null)
        {
            summoned = true;
            Summon(minionPrefab, minionCount, Water);
            EnterIdle(idleTime);
            return;
        }
        int attack;
        do attack = Random.Range(0, 3); while (attack == lastAttack);
        lastAttack = attack;
        FacePlayer();
        switch (attack)
        {
            case 0:
                risesLeft = phase2 ? 2 : 1;
                Sink(0.5f);
                break;
            case 1:
                state = State.TailWindup;
                stateEnd = Time.time + tailWindup / Speed;
                ZodiacFx.Ring(transform.position + Vector3.right * direction * tailReach * 0.5f, tailReach, new Color(1f, 0.5f, 0.3f, 0.7f), tailWindup / Speed, flat: true);
                break;
            default:
                state = State.Spit;
                stateEnd = Time.time + spitWindup / Speed;
                break;
        }
    }

    void EnterIdle(float seconds)
    {
        state = State.Idle;
        stateEnd = Time.time + seconds / Speed;
    }

    void Sink(float seconds)
    {
        state = State.Sink;
        stateEnd = Time.time + seconds;
        ZodiacFx.Ring(transform.position, 2.4f, Water, 0.4f, flat: true);
        ZodiacParticles.Dust(transform.position + Vector3.up * 0.2f, Vector3.up, 10, Water);
    }

    void Ripple(float every, float radius)
    {
        if (Time.time < nextRipple) return;
        nextRipple = Time.time + every;
        ZodiacFx.Ring(transform.position, radius, new Color(0.6f, 0.9f, 1f, 0.6f), 0.5f, flat: true);
    }

    void Rise()
    {
        state = State.Rise;
        stateEnd = Time.time + 0.45f;
        Impact(transform.position, riseRadius, UnitDamage(2f), Water, knockdown: true, groundOnly: false);
        ZodiacParticles.Dust(transform.position + Vector3.up * 0.3f, Vector3.up, 18, Water);
        Roar(0.3f);
    }

    void TailSweep()
    {
        Vector3 center = transform.position + Vector3.right * direction * tailReach * 0.5f;
        // Quét sát đất: nhảy lên thì tránh được
        if (PlayerAvailable && player.IsGrounded)
        {
            Vector3 p = player.transform.position;
            float along = (p.x - transform.position.x) * direction;
            if (along > -0.8f && along < tailReach && Mathf.Abs(p.z - transform.position.z) < 3f)
                HitPlayer(new Hit(UnitDamage(1.5f), transform.position, 1.6f) { knockdown = true });
        }
        ZodiacFx.Ring(center, tailReach, new Color(1f, 0.6f, 0.3f, 0.8f), 0.3f, flat: true);
        ZodiacParticles.Dust(center + Vector3.up * 0.2f, Vector3.right * direction, 14, Water);
        GameFeel.Shake(0.3f);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.swingHeavy, 1f, 0.05f, center);
        EnterIdle(idleTime);
    }

    void SpitWater()
    {
        if (player == null) { EnterIdle(idleTime); return; }
        Vector3 origin = body.bounds.center + Vector3.up * 0.4f + Vector3.right * direction * 0.8f;
        float[] lanes = phase2 ? new[] { -1.4f, 0f, 1.4f } : new[] { 0f };
        float pz = player.transform.position.z;
        foreach (float offset in lanes)
        {
            float z = ClampLane(pz + offset);
            for (int i = 0; i < spitShots; i++)
            {
                var shot = Projectile.Spawn(new Vector3(origin.x - direction * i * 0.6f, origin.y, z), Vector3.right * direction * spitSpeed,
                                            0.45f, Water, playerLayer, groundLayer, ShotDamage(1f), true);
                shot.range = 16f + i * 0.6f;
                shot.passWalls = true;
            }
        }
        ZodiacFx.Ring(origin, 1.2f, Water, 0.25f);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.fireball, 0.8f, 0.15f, origin);
        EnterIdle(idleTime);
    }

    protected override float MinPlayerDistance => Submerged ? 0f : 1.6f;

    protected override void Interrupt()
    {
        if (state == State.TailWindup || state == State.Spit) EnterIdle(idleTime * 0.5f);
    }

    protected override void UpdateVisual()
    {
        if (visual == null) return;
        Vector3 s = state switch
        {
            State.Sink or State.Hunt or State.Lock => new Vector3(1.3f, 0.05f, 1f),   // chìm hẳn, chỉ còn gợn sóng
            State.Rise => new Vector3(0.85f, 1.3f, 1f),
            State.Exposed => new Vector3(1.15f, 0.8f, 1f),
            State.TailWindup => new Vector3(1.2f, 0.85f, 1f),
            State.Spit => new Vector3(0.95f, 1.12f, 1f),
            _ => new Vector3(1f, 1f + 0.04f * Mathf.Sin(Time.time * 3f), 1f),
        };
        SetVisualScale(s);
        if (glowParts == null) return;
        bool winding = state == State.TailWindup || state == State.Spit || state == State.Lock;
        float flicker = winding ? 0.5f + 0.5f * Mathf.Sin(Time.time * 25f) : 0f;
        foreach (var g in glowParts)
            if (g != null) g.color = Color.Lerp(glowIdle, glowCharge, winding ? 0.5f + flicker * 0.5f : phase2 ? 0.35f : 0f);
    }
}
