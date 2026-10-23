using UnityEngine;

/// <summary>
/// Boss Chằn Tinh (Hang Đá Vôi) — yêu tinh khổng lồ trong truyện Thạch Sanh, vác chùy đá. Chậm nhưng đòn nào cũng nặng:
///   • Đập chùy: giơ chùy lên cao (rung, chùy sáng) → nện xuống trước mặt, sóng chấn chạy dọc ba làn. Né: lùi ra / nhảy qua sóng.
///   • Ném đá tảng: bẻ một tảng đá ném vòng cung, vòng báo chỗ rơi. Né: chạy khỏi vòng.
///   • Húc: cúi đầu cào chân → lao dọc làn tới khi đâm vách. Đâm vách thì choáng một lúc — cơ hội đánh. Né: đổi làn.
///   • Giai đoạn 2 (≤ 50% máu): gầm làm rụng nhũ đá khắp hang (nhiều vòng báo), sóng chấn dày hơn.
/// </summary>
public class BossOgre : BossBase
{
    enum State { Dormant, Idle, SlamWindup, Recover, ThrowWindup, ChargeWindup, Charge, Dazed, RoarRocks }

    [Header("Nhịp")]
    public float idleTime = 1.2f;
    public float moveSpeed = 2f;
    public float phase2Speedup = 1.25f;

    [Header("Đập chùy")]
    public float slamWindup = 0.9f;
    public float slamReach = 2.4f;
    public float waveSpeed = 7f;

    [Header("Ném đá")]
    public float throwWindup = 0.7f;
    public float rockFlight = 1f;
    public float rockRadius = 1.5f;

    [Header("Húc")]
    public float chargeWindup = 0.8f;
    public float chargeSpeed = 11f;
    public float chargeTime = 1.6f;
    public float dazedTime = 2f;

    [Header("Rụng nhũ đá (giai đoạn 2)")]
    public int fallingRocks = 5;

    [Header("Hiển thị")]
    public Transform club;
    public SpriteRenderer clubGlow;
    public SpriteRenderer[] eyes;

    static readonly Color Stone = new Color(0.75f, 0.68f, 0.55f);
    static readonly Color Rage = new Color(1f, 0.4f, 0.2f);

    State state = State.Dormant;
    float stateEnd;
    int lastAttack = -1;
    bool phase2, roared, chargeHit;

    public override bool IsActive => state != State.Dormant;
    float Speed => phase2 ? phase2Speedup : 1f;

    protected override void Awake()
    {
        base.Awake();
        knockbackTaken = 0f;
    }

    public override void Activate()
    {
        if (state != State.Dormant || health.IsDead) return;
        EnterIdle(1.4f);
        Roar(0.6f);
    }

    protected override void Think()
    {
        if (!phase2 && health.Current <= health.maxHealth / 2)
        {
            phase2 = true;
            Roar(0.6f);
            ZodiacFx.Ring(transform.position, 5f, Rage, 0.6f, flat: true);
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
                    float vx = dist > 3f ? direction * moveSpeed : 0f;
                    float vz = Mathf.Clamp(ToPlayer.z * 1.2f, -moveSpeed, moveSpeed);
                    Vector3 v = new Vector3(vx, 0f, vz);
                    SetPlanarVelocity(BlockedAhead(v) ? new Vector3(0f, 0f, vz) : v);
                }
                if (Time.time >= stateEnd) ChooseAttack();
                break;

            case State.SlamWindup:
                SetPlanarVelocity(Vector3.zero);
                if (Time.time >= stateEnd) Slam();
                break;

            case State.Recover:
                SetPlanarVelocity(Vector3.zero);
                if (Time.time >= stateEnd) EnterIdle(idleTime * 0.6f);
                break;

            case State.ThrowWindup:
                SetPlanarVelocity(Vector3.zero);
                FacePlayer();
                if (Time.time >= stateEnd) ThrowRock();
                break;

            case State.ChargeWindup:
            {
                float vz = player != null && stateEnd - Time.time > 0.3f ? Mathf.Clamp(ToPlayer.z * 5f, -4f, 4f) : 0f;
                SetPlanarVelocity(new Vector3(0f, 0f, vz));
                if (Random.value < 0.3f) ZodiacParticles.Dust(transform.position + Vector3.up * 0.1f, Vector3.left * direction, 2);
                if (Time.time >= stateEnd)
                {
                    state = State.Charge;
                    stateEnd = Time.time + chargeTime;
                    chargeHit = false;
                }
                break;
            }

            case State.Charge:
            {
                Vector3 forward = Vector3.right * direction;
                SetPlanarVelocity(forward * chargeSpeed * Mathf.Sqrt(Speed));
                if (!chargeHit)
                {
                    Bounds b = body.bounds;
                    chargeHit = StrikeBox(b.center + forward * 0.4f, b.extents, new Hit(UnitDamage(2f), transform.position - forward, 1.8f) { knockdown = true });
                }
                if (BlockedAhead(forward, 0.35f) || transform.position.x <= ZodiacGameManager.RoomMinX + 1.2f && direction < 0f ||
                    transform.position.x >= ZodiacGameManager.RoomMaxX - 1.2f && direction > 0f)
                {
                    // Đâm sầm vào vách: đá rơi lả tả, choáng váng — lúc phản công
                    Impact(transform.position + forward * 1.2f, 1.2f, UnitDamage(1f), Stone);
                    state = State.Dazed;
                    stateEnd = Time.time + dazedTime;
                    ZodiacFx.Ring(transform.position + Vector3.up * 2.6f, 0.8f, Color.yellow, 0.5f);
                }
                else if (Time.time >= stateEnd) EnterIdle(idleTime);
                break;
            }

            case State.Dazed:
                SetPlanarVelocity(Vector3.zero);
                if (Time.time >= stateEnd) EnterIdle(idleTime * 0.5f);
                break;

            case State.RoarRocks:
                SetPlanarVelocity(Vector3.zero);
                if (Time.time >= stateEnd)
                {
                    RainRocks();
                    EnterIdle(idleTime);
                }
                break;
        }
    }

    void ChooseAttack()
    {
        if (phase2 && !roared)
        {
            roared = true;
            state = State.RoarRocks;
            stateEnd = Time.time + 0.8f;
            Roar(0.7f);
            return;
        }
        int attack;
        do attack = Random.Range(0, 3); while (attack == lastAttack);
        lastAttack = attack;
        FacePlayer();
        // Đứng xa thì ít đập chùy (không với tới) — ném đá hoặc húc
        if (attack == 0 && player != null && Mathf.Abs(ToPlayer.x) > 5f) attack = Random.value < 0.5f ? 1 : 2;
        switch (attack)
        {
            case 0:
                state = State.SlamWindup;
                stateEnd = Time.time + slamWindup / Speed;
                ZodiacFx.Ring(SlamPoint + Vector3.up * 0.03f, slamReach * 2f, new Color(1f, 0.4f, 0.2f, 0.75f), slamWindup / Speed, flat: true);
                break;
            case 1: state = State.ThrowWindup; stateEnd = Time.time + throwWindup / Speed; break;
            default: state = State.ChargeWindup; stateEnd = Time.time + chargeWindup / Speed; break;
        }
        // Mỗi lần gầm ở giai đoạn 2 lại có thể rụng đá thêm
        if (phase2 && Random.value < 0.25f) roared = false;
    }

    Vector3 SlamPoint => transform.position + Vector3.right * direction * slamReach;

    void EnterIdle(float seconds)
    {
        state = State.Idle;
        stateEnd = Time.time + seconds / Speed;
    }

    void Slam()
    {
        Vector3 at = SlamPoint;
        Impact(at, slamReach, UnitDamage(2f), Rage, knockdown: true);
        GameFeel.Shake(0.6f);
        // Sóng chấn chạy tiếp về phía trước, trên làn của boss và hai làn cạnh
        float[] offsets = phase2 ? new[] { -1.8f, -0.9f, 0f, 0.9f, 1.8f } : new[] { -1.4f, 0f, 1.4f };
        foreach (float dz in offsets)
        {
            Vector3 from = new Vector3(at.x, at.y, ClampLane(at.z + dz));
            GroundWave(from, Vector3.right * direction, waveSpeed, 10f, Rage);
        }
        state = State.Recover;
        stateEnd = Time.time + 0.9f / Speed;   // rút chùy lên chậm — lúc đánh vào
    }

    void ThrowRock()
    {
        Vector3 from = body.bounds.center + Vector3.up * 1.2f;
        Vector3 target = PlayerGround();
        Lob(from, target, rockFlight / Speed, 1.1f, Stone, rockRadius, at => Impact(at, rockRadius, UnitDamage(2f), Stone, knockdown: true));
        if (phase2)   // giai đoạn 2: thêm một tảng đón đầu theo hướng người chơi chạy
        {
            Vector3 lead = target + (player != null ? new Vector3(Mathf.Sign(ToPlayer.x) * 2.5f, 0f, 0f) : Vector3.zero);
            lead.x = ClampRoom(lead.x);
            Lob(from, lead, rockFlight * 1.25f / Speed, 1f, Stone, rockRadius, at => Impact(at, rockRadius, UnitDamage(2f), Stone, knockdown: true));
        }
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.swingHeavy, 1f, 0.05f, from);
        EnterIdle(idleTime);
    }

    void RainRocks()
    {
        for (int i = 0; i < fallingRocks; i++)
        {
            Vector3 at = i == 0 ? PlayerGround()
                                : new Vector3(ClampRoom(Random.Range(ZodiacGameManager.RoomMinX + 1f, ZodiacGameManager.RoomMaxX - 1f)),
                                              transform.position.y, ClampLane(Random.Range(-3f, 3f)));
            Blast(at, 1f, 1.1f + i * 0.15f, UnitDamage(1.5f), Stone, onBlast: () => ZodiacParticles.Debris(at + Vector3.up * 0.5f, Stone, 10, 0.6f));
        }
    }

    protected override float MinPlayerDistance => state == State.Charge ? 0f : 1.8f;

    protected override void Interrupt()
    {
        if (state == State.ThrowWindup || state == State.ChargeWindup) EnterIdle(idleTime * 0.5f);
    }

    protected override void UpdateVisual()
    {
        if (visual == null) return;
        Vector3 s = state switch
        {
            State.SlamWindup => new Vector3(0.92f, 1.12f, 1f),
            State.Recover => new Vector3(1.15f, 0.85f, 1f),
            State.ChargeWindup => new Vector3(1.15f, 0.88f, 1f),
            State.Charge => new Vector3(1.25f, 0.85f, 1f),
            State.Dazed => new Vector3(1f, 0.92f + 0.05f * Mathf.Sin(Time.time * 10f), 1f),
            State.RoarRocks => new Vector3(1.1f, 1.1f, 1f),
            _ => new Vector3(1f, 1f + 0.025f * Mathf.Sin(Time.time * 2.5f), 1f),
        };
        SetVisualScale(s);
        if (club != null)
        {
            float angle = state switch
            {
                State.SlamWindup => 110f,
                State.Recover => -40f,
                State.ThrowWindup => 70f,
                State.Charge => -20f,
                _ => 20f + 4f * Mathf.Sin(Time.time * 2f),
            };
            club.localRotation = Quaternion.Lerp(club.localRotation, Quaternion.Euler(0f, 0f, angle), Time.deltaTime * 12f);
        }
        bool winding = state == State.SlamWindup || state == State.ThrowWindup || state == State.ChargeWindup || state == State.RoarRocks;
        float flicker = winding ? 0.5f + 0.5f * Mathf.Sin(Time.time * 22f) : 0f;
        if (clubGlow != null) clubGlow.color = new Color(1f, 0.5f, 0.2f, winding ? 0.4f + 0.5f * flicker : 0f);
        if (eyes != null)
            foreach (var e in eyes)
                if (e != null) e.color = Color.Lerp(new Color(1f, 0.85f, 0.3f), Rage, winding ? flicker : phase2 ? 0.6f : 0f);
    }
}
