using System.Collections;
using UnityEngine;

/// <summary>
/// Boss Ông Ba Bị (Chợ Phiên Ma) — ông ba bị "chín quai mười hai con mắt" vác bao tải đi bắt trẻ con, trong lời doạ của bà.
///   • Ném bao: quăng bao tải vòng cung, vòng đỏ báo chỗ rơi → trúng thì bị trùm bao ngã nhào. Né: chạy khỏi vòng.
///   • Vồ: khom người, tay vươn ra → lao dọc làn người chơi. Né: đổi làn / lăn.
///   • Đổ bếp than: hất ba bốn bếp than hồng ra quanh chợ, chỗ rơi thành vùng lửa cháy một lúc. Đi vòng tránh.
///   • Giai đoạn 2 (≤ 50% máu): độn thổ, hiện ra sau lưng người chơi rồi quật bao; bao rơi xổ ra Lính Đất Nung.
/// </summary>
public class BossBogeyman : BossBase
{
    enum State { Dormant, Idle, ThrowWindup, LungeWindup, Lunge, Scatter, Vanish, Appear }

    [Header("Nhịp")]
    public float idleTime = 1f;
    public float moveSpeed = 2.6f;
    public float phase2Speedup = 1.3f;

    [Header("Ném bao")]
    public float throwWindup = 0.6f;
    public float sackFlight = 0.9f;
    public float sackRadius = 1.3f;

    [Header("Vồ")]
    public float lungeWindup = 0.65f;
    public float lungeSpeed = 12f;
    public float lungeTime = 0.8f;

    [Header("Đổ bếp than")]
    public int embers = 3;
    public float emberLife = 5f;

    [Header("Lính trong bao (giai đoạn 2)")]
    public GameObject minionPrefab;

    [Header("Hiển thị")]
    public SpriteRenderer[] eyes;
    public Transform sack;

    static readonly Color SackColor = new Color(0.65f, 0.5f, 0.3f);
    static readonly Color Ember = new Color(1f, 0.45f, 0.15f);

    State state = State.Dormant;
    float stateEnd;
    int lastAttack = -1;
    bool phase2, lungeHit;

    public override bool IsActive => state != State.Dormant;
    float Speed => phase2 ? phase2Speedup : 1f;

    protected override void Awake()
    {
        base.Awake();
        knockbackTaken = 0.15f;
    }

    public override void Activate()
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
            Roar(0.5f);
            ZodiacFx.Ring(transform.position, 5f, new Color(0.8f, 0.3f, 1f), 0.6f, flat: true);
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
                    // Lừ đừ tiến lại gần, lệch dần sang làn người chơi
                    float dist = Mathf.Abs(ToPlayer.x);
                    float vx = dist > 4f ? direction * moveSpeed : dist < 2f ? -direction * moveSpeed * 0.5f : 0f;
                    float vz = Mathf.Clamp(ToPlayer.z * 1.5f, -moveSpeed, moveSpeed);
                    Vector3 v = new Vector3(vx, 0f, vz);
                    SetPlanarVelocity(BlockedAhead(v) ? new Vector3(0f, 0f, vz) : v);
                }
                if (Time.time >= stateEnd) ChooseAttack();
                break;

            case State.ThrowWindup:
                SetPlanarVelocity(Vector3.zero);
                FacePlayer();
                if (Time.time >= stateEnd) ThrowSack(PlayerGround(), phase2);
                break;

            case State.LungeWindup:
            {
                float vz = player != null && stateEnd - Time.time > 0.25f ? Mathf.Clamp(ToPlayer.z * 6f, -5f, 5f) : 0f;
                SetPlanarVelocity(new Vector3(0f, 0f, vz));
                if (Time.time >= stateEnd)
                {
                    state = State.Lunge;
                    stateEnd = Time.time + lungeTime;
                    lungeHit = false;
                }
                break;
            }

            case State.Lunge:
            {
                Vector3 forward = Vector3.right * direction;
                SetPlanarVelocity(forward * lungeSpeed * Mathf.Sqrt(Speed));
                if (!lungeHit)
                {
                    Bounds b = body.bounds;
                    lungeHit = StrikeBox(b.center + forward * 0.5f, b.extents, new Hit(UnitDamage(1.5f), transform.position - forward, 1.4f) { knockdown = true });
                }
                if (Time.time >= stateEnd || BlockedAhead(forward, 0.3f)) EnterIdle(idleTime);
                break;
            }

            case State.Scatter:
                SetPlanarVelocity(Vector3.zero);
                if (Time.time >= stateEnd) ScatterEmbers();
                break;

            case State.Vanish:
                SetPlanarVelocity(Vector3.zero);
                health.SetInvulnerable(0.2f);
                if (Time.time >= stateEnd) AppearBehind();
                break;

            case State.Appear:
                SetPlanarVelocity(Vector3.zero);
                FacePlayer();
                if (Time.time >= stateEnd)
                {
                    // Quật bao ngay tại chỗ, sát lưng người chơi
                    Impact(transform.position + Vector3.right * direction * 1.4f, 1.5f, UnitDamage(1.5f), SackColor, knockdown: true);
                    EnterIdle(idleTime);
                }
                break;
        }
    }

    void ChooseAttack()
    {
        int count = phase2 ? 4 : 3;
        int attack;
        do attack = Random.Range(0, count); while (attack == lastAttack);
        lastAttack = attack;
        FacePlayer();
        switch (attack)
        {
            case 0: state = State.ThrowWindup; stateEnd = Time.time + throwWindup / Speed; break;
            case 1: state = State.LungeWindup; stateEnd = Time.time + lungeWindup / Speed; break;
            case 2: state = State.Scatter; stateEnd = Time.time + 0.55f / Speed; break;
            default:
                state = State.Vanish;
                stateEnd = Time.time + 0.7f;
                ZodiacFx.Puff(transform.position + Vector3.up * 1f, new Color(0.3f, 0.2f, 0.35f, 0.9f), 2.4f);
                break;
        }
    }

    void EnterIdle(float seconds)
    {
        state = State.Idle;
        stateEnd = Time.time + seconds / Speed;
    }

    void ThrowSack(Vector3 target, bool withMinion)
    {
        Vector3 from = body.bounds.center + Vector3.up * 0.8f;
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.swingHeavy, 1f, 0.05f, from);
        Lob(from, target, sackFlight / Speed, 0.9f, SackColor, sackRadius, at =>
        {
            Impact(at, sackRadius, UnitDamage(1.5f), SackColor, knockdown: true);
            if (withMinion && minionPrefab != null && AliveMinions() < 3) SpawnFromSack(at);
        });
        EnterIdle(idleTime);
    }

    int AliveMinions()
    {
        int n = 0;
        foreach (var e in FindObjectsByType<EnemyBase>())
            if (e != this && !e.Health.IsDead) n++;
        return n;
    }

    void SpawnFromSack(Vector3 at)
    {
        var go = Instantiate(minionPrefab, at + Vector3.up * 0.1f, Quaternion.identity);
        if (go.TryGetComponent(out EnemyBase e))
        {
            e.alwaysAggro = true;
            e.ApplyScaling(ZodiacDifficulty.HealthScale * 0.6f, ZodiacDifficulty.DamageScale, 0.3f);
            ZodiacGameManager.Instance?.Room?.Track(e.Health);
        }
        ZodiacFx.Puff(at + Vector3.up * 0.6f, SackColor, 1.4f);
    }

    void ScatterEmbers()
    {
        int n = phase2 ? embers + 1 : embers;
        Vector3 from = body.bounds.center + Vector3.up * 0.5f;
        for (int i = 0; i < n; i++)
        {
            Vector3 to = i == 0 ? PlayerGround()
                                : new Vector3(ClampRoom(transform.position.x + Random.Range(-6f, 6f)), transform.position.y, ClampLane(Random.Range(-3f, 3f)));
            Lob(from, to, 0.8f + i * 0.12f, 0.45f, Ember, 1f, at => StartCoroutine(Fire(at)));
        }
        EnterIdle(idleTime);
    }

    /// <summary>Vùng than hồng cháy một lúc: đứng vào là bỏng (như bẫy trên đường).</summary>
    IEnumerator Fire(Vector3 at)
    {
        var go = new GameObject("BurningCoals");
        go.transform.position = new Vector3(at.x, 0f, at.z);
        var box = go.AddComponent<BoxCollider>();
        box.size = new Vector3(1.6f, 0.6f, 1.4f);
        box.center = new Vector3(0f, 0.3f, 0f);
        go.AddComponent<Hazard>().damage = Mathf.Max(1, ZodiacBalance.Unit / 2);
        var glow = ZodiacFx.Shape("Coals", true, go.transform.position + Vector3.up * 0.04f, new Vector2(1.6f, 1.4f), new Color(1f, 0.4f, 0.1f, 0.85f), 13, go.transform);
        glow.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        ZodiacLights.AddPoint(go, Ember, 3.5f, 2f);
        float end = Time.time + emberLife;
        while (Time.time < end && !health.IsDead)
        {
            if (Random.value < 0.15f) ZodiacParticles.Sparks(go.transform.position + Vector3.up * 0.2f, Ember, 2);
            glow.color = new Color(1f, 0.4f, 0.1f, 0.6f + 0.25f * Mathf.Sin(Time.time * 8f));
            yield return null;
        }
        Destroy(go);
    }

    void AppearBehind()
    {
        Vector3 p = PlayerGround();
        // Hiện ra ở phía bên kia người chơi (sau lưng nếu người chơi đang quay mặt về phía boss)
        float side = player != null && Mathf.Abs(ToPlayer.x) > 0.01f ? Mathf.Sign(ToPlayer.x) : 1f;
        Vector3 at = new Vector3(ClampRoom(p.x + side * 1.8f), transform.position.y, p.z);
        rb.position = at;
        transform.position = at;
        ZodiacFx.Puff(at + Vector3.up * 1f, new Color(0.3f, 0.2f, 0.35f, 0.9f), 2.4f);
        state = State.Appear;
        stateEnd = Time.time + 0.55f / Speed;   // khựng lại một nhịp để người chơi kịp lăn
        FacePlayer();
    }

    protected override float MinPlayerDistance => state == State.Lunge || state == State.Appear ? 0f : 1.4f;

    protected override void Interrupt()
    {
        if (state == State.ThrowWindup || state == State.LungeWindup || state == State.Scatter) EnterIdle(idleTime * 0.5f);
    }

    protected override void UpdateVisual()
    {
        if (visual == null) return;
        Vector3 s = state switch
        {
            State.ThrowWindup => new Vector3(0.9f, 1.15f, 1f),
            State.LungeWindup => new Vector3(1.2f, 0.82f, 1f),
            State.Lunge => new Vector3(1.3f, 0.85f, 1f),
            State.Scatter => new Vector3(1.1f, 0.9f, 1f),
            State.Vanish => new Vector3(0.3f, 1.4f, 1f),
            _ => new Vector3(1f, 1f + 0.03f * Mathf.Sin(Time.time * 4f), 1f),
        };
        SetVisualScale(s);
        if (sack != null) sack.gameObject.SetActive(state != State.ThrowWindup || Time.time < stateEnd - 0.1f);
        if (eyes == null) return;
        bool winding = state == State.ThrowWindup || state == State.LungeWindup || state == State.Scatter || state == State.Appear;
        foreach (var e in eyes)
            if (e != null) e.color = winding ? Color.Lerp(new Color(1f, 0.85f, 0.2f), Color.red, 0.5f + 0.5f * Mathf.Sin(Time.time * 25f))
                                             : phase2 ? new Color(1f, 0.4f, 0.3f) : new Color(1f, 0.85f, 0.2f);
    }
}
