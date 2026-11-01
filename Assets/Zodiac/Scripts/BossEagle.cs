using UnityEngine;

/// <summary>
/// Boss Đại Bàng Tinh (Đỉnh Tản Viên) — chim yêu khổng lồ trong truyện Thạch Sanh, từng quắp công chúa về hang.
/// Đánh từ trên cao xuống, mỗi đòn có báo hiệu rồi để lộ sơ hở:
///   • Bay vút — bổ nhào: vỗ cánh bay lên khỏi tầm đánh, bóng đen lượn theo người chơi; bóng đứng lại và vòng đỏ hiện →
///     bổ nhào xuống đúng chỗ đó (quật ngã). Né: chạy khỏi vòng / lăn. Bổ xong cắm mỏ xuống đất một lúc — lúc để đánh.
///   • Mưa lông vũ: xoè cánh, lông vàng rực sáng → bắn chùm lông thép hình quạt qua nhiều làn. Né: đổi làn vào khe.
///   • Gió cánh: lùi lại, vỗ cánh mạnh → các luồng gió sát đất chạy dọc từng làn. Né: nhảy qua.
///   • Giai đoạn 2 (≤ 50% máu): bổ nhào liền hai lần, lông dày hơn, gọi Ma Trơi núi.
/// </summary>
public class BossEagle : BossBase
{
    enum State { Dormant, Idle, Soar, Circle, Lock, Dive, Stuck, FeatherWindup, GustWindup }

    [Header("Nhịp")]
    public float idleTime = 1.1f;
    public float moveSpeed = 3.2f;
    public float phase2Speedup = 1.3f;

    [Header("Bay vút — bổ nhào")]
    public float soarTime = 0.6f;
    public float circleTime = 1.8f;
    public float circleSpeed = 6f;
    public float lockTime = 0.6f;
    public float diveTime = 0.25f;
    public float diveRadius = 1.8f;
    public float stuckTime = 1.7f;
    public float flightHeight = 7f;

    [Header("Mưa lông vũ")]
    public float featherWindup = 0.7f;
    public int featherCount = 5;
    public int featherCountPhase2 = 8;
    public float featherSpread = 60f;
    public float featherSpeed = 9f;

    [Header("Gió cánh")]
    public float gustWindup = 0.8f;
    public float gustSpeed = 7.5f;
    public float gustRange = 12f;

    [Header("Gọi lính (giai đoạn 2)")]
    public GameObject minionPrefab;
    public int minionCount = 2;

    [Header("Hiển thị")]
    [Tooltip("Lông cánh / mắt — sáng lên khi báo hiệu đòn.")]
    public SpriteRenderer[] glowParts;
    public Color glowIdle = new Color(1f, 0.75f, 0.3f);
    public Color glowCharge = new Color(1f, 0.35f, 0.15f);

    static readonly Color Wind = new Color(0.85f, 0.95f, 1f);
    static readonly Color Gold = new Color(1f, 0.8f, 0.3f);

    State state = State.Dormant;
    float stateEnd, height, nextShadowRing;
    int lastAttack = -1, divesLeft;
    bool phase2, summoned;
    Vector3 lockAt;

    public override bool IsActive => state != State.Dormant;
    float Speed => phase2 ? phase2Speedup : 1f;
    bool Airborne => state == State.Soar || state == State.Circle || state == State.Lock;

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
        ZodiacFx.Ring(transform.position, 4f, Gold, 0.6f, flat: true);
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
                    Vector3 v = dist < 2.5f ? Vector3.right * -direction * moveSpeed : dist > 6f ? Vector3.right * direction * moveSpeed : Vector3.zero;
                    SetPlanarVelocity(BlockedAhead(v) ? Vector3.zero : v);
                }
                if (Time.time >= stateEnd) ChooseAttack();
                break;

            case State.Soar:
                health.SetInvulnerable(0.2f);
                SetPlanarVelocity(Vector3.zero);
                if (Time.time >= stateEnd) { state = State.Circle; stateEnd = Time.time + circleTime / Speed; }
                break;

            case State.Circle:
            {
                health.SetInvulnerable(0.2f);
                Vector3 d = PlayerGround() - transform.position;
                d.y = 0f;
                SetPlanarVelocity(d.magnitude < 0.2f ? Vector3.zero : d.normalized * Mathf.Min(circleSpeed * Speed, d.magnitude * 5f));
                ShadowPulse(0.4f, 1.4f, new Color(0.15f, 0.1f, 0.1f, 0.5f));
                if (Time.time >= stateEnd)
                {
                    state = State.Lock;
                    stateEnd = Time.time + lockTime / Speed;
                    lockAt = transform.position;
                    ZodiacFx.Ring(lockAt + Vector3.up * 0.03f, diveRadius * 2f, new Color(1f, 0.35f, 0.2f, 0.85f), lockTime / Speed, flat: true);
                }
                break;
            }

            case State.Lock:
                health.SetInvulnerable(0.2f);
                SetPlanarVelocity(Vector3.zero);
                if (Time.time >= stateEnd) { state = State.Dive; stateEnd = Time.time + diveTime; }
                break;

            case State.Dive:
                SetPlanarVelocity(Vector3.zero);
                if (Time.time >= stateEnd) Land();
                break;

            case State.Stuck:
                SetPlanarVelocity(Vector3.zero);
                if (Time.time >= stateEnd) EnterIdle(idleTime * 0.6f);
                break;

            case State.FeatherWindup:
                SetPlanarVelocity(Vector3.zero);
                FacePlayer();
                if (Time.time >= stateEnd) Feathers();
                break;

            case State.GustWindup:
            {
                // Lùi lại lấy đà rồi vỗ cánh
                Vector3 back = Vector3.right * -direction * moveSpeed * 0.6f;
                SetPlanarVelocity(BlockedAhead(back) ? Vector3.zero : back);
                if (Time.time >= stateEnd) Gust();
                break;
            }
        }
    }

    void ChooseAttack()
    {
        if (phase2 && !summoned && minionPrefab != null)
        {
            summoned = true;
            Summon(minionPrefab, minionCount, Gold);
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
                divesLeft = phase2 ? 2 : 1;
                TakeOff(soarTime);
                break;
            case 1:
                state = State.FeatherWindup;
                stateEnd = Time.time + featherWindup / Speed;
                break;
            default:
                state = State.GustWindup;
                stateEnd = Time.time + gustWindup / Speed;
                break;
        }
    }

    void EnterIdle(float seconds)
    {
        state = State.Idle;
        stateEnd = Time.time + seconds / Speed;
    }

    void TakeOff(float seconds)
    {
        state = State.Soar;
        stateEnd = Time.time + seconds;
        ZodiacFx.Ring(transform.position, 3f, Wind, 0.4f, flat: true);
        ZodiacParticles.Dust(transform.position + Vector3.up * 0.2f, Vector3.left, 8, Wind);
        ZodiacParticles.Dust(transform.position + Vector3.up * 0.2f, Vector3.right, 8, Wind);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.swingHeavy, 0.9f, 0.1f, transform.position);
    }

    void ShadowPulse(float every, float radius, Color color)
    {
        if (Time.time < nextShadowRing) return;
        nextShadowRing = Time.time + every;
        ZodiacFx.Ring(transform.position + Vector3.up * 0.02f, radius, color, 0.5f, flat: true);
    }

    void Land()
    {
        Impact(lockAt, diveRadius, UnitDamage(2f), Gold, knockdown: true);
        Roar(0.35f);
        if (--divesLeft > 0) { TakeOff(0.3f); return; }
        state = State.Stuck;
        stateEnd = Time.time + stuckTime;
    }

    void Feathers()
    {
        if (player == null) { EnterIdle(idleTime); return; }
        Vector3 origin = body.bounds.center + Vector3.up * 0.3f;
        int count = phase2 ? featherCountPhase2 : featherCount;
        Vector3 aim = PlayerCenter - origin;
        aim.y = 0f;
        if (aim.sqrMagnitude < 0.01f) aim = Vector3.right * direction;
        for (int i = 0; i < count; i++)
        {
            float angle = count == 1 ? 0f : Mathf.Lerp(-featherSpread / 2f, featherSpread / 2f, i / (count - 1f));
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * aim.normalized;
            var shot = Projectile.Spawn(origin, dir * featherSpeed, 0.35f, Gold, playerLayer, groundLayer, ShotDamage(1f), true);
            shot.range = 14f;
            shot.passWalls = true;
        }
        ZodiacFx.Ring(origin, 1.4f, Gold, 0.25f);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.fireball, 0.8f, 0.15f, origin);
        EnterIdle(idleTime);
    }

    void Gust()
    {
        Vector3 from = transform.position + Vector3.right * direction * 0.8f;
        float[] lanes = phase2 ? new[] { -1.6f, -0.5f, 0.6f, 1.7f } : new[] { -1.2f, 0f, 1.2f };
        foreach (float offset in lanes)
            GroundWave(new Vector3(from.x, from.y, ClampLane(transform.position.z + offset)), Vector3.right * direction, gustSpeed, gustRange, Wind, 1.2f);
        ZodiacParticles.Dust(from + Vector3.up * 0.4f, Vector3.right * direction, 16, Wind);
        GameFeel.Shake(0.25f);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.swingHeavy, 1f, 0.05f, from);
        EnterIdle(idleTime);
    }

    protected override float MinPlayerDistance => Airborne ? 0f : 1.6f;

    protected override void Interrupt()
    {
        if (state == State.FeatherWindup || state == State.GustWindup) EnterIdle(idleTime * 0.5f);
    }

    protected override void UpdateVisual()
    {
        if (visual == null) return;
        // Độ cao của hình (thân vẫn ở mặt đất để bóng và vòng báo đúng chỗ)
        float target = state switch
        {
            State.Soar or State.Circle or State.Lock => flightHeight,
            State.Dive => 0f,
            _ => 0f,
        };
        float rate = state == State.Dive ? flightHeight / Mathf.Max(0.05f, diveTime) : state == State.Soar ? flightHeight / Mathf.Max(0.1f, soarTime) : 12f;
        height = Mathf.MoveTowards(height, target, rate * Time.fixedDeltaTime);
        var lp = visual.localPosition;
        visual.localPosition = new Vector3(lp.x, height, lp.z);

        float flap = Mathf.Sin(Time.time * (Airborne ? 14f : 5f));
        Vector3 s = state switch
        {
            State.Dive => new Vector3(0.75f, 1.3f, 1f),
            State.Stuck => new Vector3(1.2f, 0.75f, 1f),
            State.FeatherWindup => new Vector3(1.2f + 0.05f * flap, 1.05f, 1f),
            State.GustWindup => new Vector3(1.1f + 0.12f * flap, 0.95f, 1f),
            _ => new Vector3(1f, 1f + 0.05f * flap, 1f),
        };
        SetVisualScale(s);
        if (glowParts == null) return;
        bool winding = state == State.FeatherWindup || state == State.GustWindup || state == State.Lock;
        float flicker = winding ? 0.5f + 0.5f * Mathf.Sin(Time.time * 25f) : 0f;
        foreach (var g in glowParts)
            if (g != null) g.color = WithGlowAlpha(Color.Lerp(glowIdle, glowCharge, winding ? 0.5f + flicker * 0.5f : phase2 ? 0.35f : 0f));
    }
}
