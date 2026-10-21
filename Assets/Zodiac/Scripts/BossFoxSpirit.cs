using System;
using UnityEngine;

/// <summary>
/// Boss Hồ Tinh (cáo chín đuôi — truyền thuyết Lạc Long Quân diệt Hồ Tinh ở Hồ Tây), đánh trên mặt đường có chiều sâu.
/// Mỗi đòn có báo hiệu rõ ràng rồi mới ra, giữa các đòn có khoảng nghỉ để phản công:
///   • Lao vồ: dạt sang đúng làn người chơi, cúi thấp + đuôi rực sáng → lao dọc đường tới khi chạm tường. Né: đổi làn / lăn.
///   • Nhảy đập: bật cao, đáp đúng chỗ người chơi đứng → vùng chấn tròn + sóng toả ra mọi hướng sát đất. Né: chạy ra khỏi
///     vùng, nhảy qua sóng.
///   • Hồ hoả: đứng niệm chú → chùm lửa hình quạt quét qua nhiều làn. Né: đổi làn vào khe giữa các viên / chém tan.
///   • Giai đoạn 2 (≤ 50% máu): nhanh hơn, sóng và lửa dày hơn, gọi thêm Ma Trơi.
/// Chỉ hoạt động sau khi <see cref="Activate"/> (người chơi bước vào đấu trường).
/// </summary>
public class BossFoxSpirit : EnemyBase
{
    enum State { Dormant, Idle, DashWindup, Dash, LeapWindup, Leap, CastWindup, Summon }

    public string bossName = "Hồ Tinh";

    [Header("Nhịp đánh")]
    public float idleTime = 1f;
    [Tooltip("Hệ số tốc độ ở giai đoạn 2 (nghỉ ngắn hơn, báo hiệu nhanh hơn).")]
    public float phase2Speedup = 1.35f;
    public float moveSpeed = 3f;

    [Header("Lao vồ")]
    public float dashWindup = 0.7f;
    public float dashSpeed = 14f;
    public float dashMaxTime = 1.4f;
    [Min(0)] public int dashDamage = 2 * ZodiacBalance.Unit;

    [Header("Nhảy đập")]
    public float leapWindup = 0.5f;
    public float leapAirTime = 0.9f;
    public float slamRadius = 2.4f;
    [Min(0)] public int slamDamage = 2 * ZodiacBalance.Unit;
    public int waveCount = 6;
    public int waveCountPhase2 = 10;
    public float shockwaveSpeed = 7f;
    public float shockwaveRange = 9f;

    [Header("Hồ hoả")]
    public float castWindup = 0.7f;
    public int fireCount = 5;
    public int fireCountPhase2 = 7;
    public float fireSpread = 70f;
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
    public SpriteRenderer landingMarker;

    State state = State.Dormant;
    float stateEnd;
    int lastAttack = -1;
    bool phase2, summoned, dashLanded;
    float leapLandAfter;
    Vector3 leapTarget;

    public bool IsActive => state != State.Dormant;
    public bool IsPhase2 => phase2;
    public string CurrentState => state.ToString();
    public event Action PhaseChanged;

    float Speed => phase2 ? phase2Speedup : 1f;

    protected override void Awake()
    {
        base.Awake();
        usesAttackToken = false;
        canBeKnockedDown = false;
        // Sprite vòng tròn tạo lúc chạy (không phải asset nên không lưu sẵn trong prefab được)
        if (landingMarker != null && landingMarker.sprite == null) landingMarker.sprite = ZodiacFx.RingSprite;
    }

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
            ZodiacFx.Ring(transform.position, 5f, tailCharge, 0.6f, flat: true);
            Roar();
            PhaseChanged?.Invoke();
        }

        switch (state)
        {
            case State.Dormant:
                SetPlanarVelocity(Vector3.zero);
                FacePlayer();
                break;
            case State.Idle:
                FacePlayer();
                // Lững thững giữ khoảng cách vừa phải với người chơi
                if (player != null)
                {
                    float dist = Mathf.Abs(ToPlayer.x);
                    Vector3 v = dist < 3f ? Vector3.right * -direction * moveSpeed * 0.6f
                              : dist > 7f ? Vector3.right * direction * moveSpeed : Vector3.zero;
                    SetPlanarVelocity(BlockedAhead(v) ? Vector3.zero : v);
                }
                if (Time.time >= stateEnd && IsGrounded()) ChooseAttack();
                break;
            case State.DashWindup:
            {
                // Dạt về làn người chơi trong lúc gồng (khoá làn ở 0.25s cuối để người chơi kịp né)
                float vz = 0f;
                if (player != null && stateEnd - Time.time > 0.25f)
                {
                    float dz = player.transform.position.z - transform.position.z;
                    vz = Mathf.Clamp(dz * 6f, -moveSpeed * 2f, moveSpeed * 2f);
                }
                SetPlanarVelocity(new Vector3(0f, 0f, vz));
                if (Time.time >= stateEnd)
                {
                    state = State.Dash;
                    stateEnd = Time.time + dashMaxTime;
                    dashLanded = false;
                }
                break;
            }
            case State.Dash:
            {
                Vector3 forward = Vector3.right * direction;
                SetPlanarVelocity(forward * dashSpeed * Mathf.Sqrt(Speed));
                if (!dashLanded)
                {
                    Bounds b = body.bounds;
                    var hit = new Hit(dashDamage, transform.position - forward, 1.5f) { knockdown = true };
                    dashLanded = StrikeBox(b.center + forward * 0.3f, b.extents, hit);
                }
                if (Time.time >= stateEnd || BlockedAhead(forward, 0.3f)) EnterIdle(idleTime);
                break;
            }
            case State.LeapWindup:
                SetPlanarVelocity(Vector3.zero);
                if (Time.time >= stateEnd) StartLeap();
                break;
            case State.Leap:
                if (Time.time >= leapLandAfter && IsGrounded() && rb.linearVelocity.y <= 0.01f) Land();
                break;
            case State.CastWindup:
                SetPlanarVelocity(Vector3.zero);
                FacePlayer();
                if (Time.time >= stateEnd)
                {
                    CastFire();
                    EnterIdle(idleTime * 0.8f);
                }
                break;
            case State.Summon:
                SetPlanarVelocity(Vector3.zero);
                if (Time.time >= stateEnd)
                {
                    SummonMinions();
                    EnterIdle(idleTime);
                }
                break;
        }
        UpdateMarker();
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
            case 1:
                state = State.LeapWindup;
                stateEnd = Time.time + leapWindup / Speed;
                leapTarget = player != null ? player.transform.position : transform.position;
                leapTarget.y = transform.position.y;
                leapTarget.z = ClampLane(leapTarget.z);
                leapTarget.x = ClampRoom(leapTarget.x);
                break;
            default: state = State.CastWindup; stateEnd = Time.time + castWindup / Speed; break;
        }
    }

    void EnterIdle(float seconds)
    {
        state = State.Idle;
        stateEnd = Time.time + seconds / Speed;
    }

    void StartLeap()
    {
        // Cập nhật đích theo chỗ người chơi lúc bật nhảy (vòng báo hiện ngay trên mặt đất)
        if (player != null)
        {
            leapTarget = player.transform.position;
            leapTarget.y = transform.position.y;
            leapTarget.z = ClampLane(leapTarget.z);
            leapTarget.x = ClampRoom(leapTarget.x);
        }
        float g = -Physics.gravity.y * Mathf.Max(1f, gravityMultiplier);
        Vector3 d = Vector3.ClampMagnitude(leapTarget - transform.position, 12f);
        leapTarget = transform.position + d;
        rb.linearVelocity = new Vector3(d.x / leapAirTime, g * leapAirTime * 0.5f, d.z / leapAirTime);
        state = State.Leap;
        leapLandAfter = Time.time + 0.2f;
    }

    void Land()
    {
        SetPlanarVelocity(Vector3.zero);
        Vector3 feet = transform.position + Vector3.up * 0.3f;
        ZodiacFx.Ring(transform.position, slamRadius, tailCharge, 0.35f, flat: true);
        ZodiacParticles.Dust(feet, Vector3.left, 12);
        ZodiacParticles.Dust(feet, Vector3.right, 12);
        ZodiacParticles.Debris(feet, new Color(0.45f, 0.4f, 0.5f), 10, 1f);
        GameFeel.Shake(0.55f);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.bossSlam, 1f, 0.05f, feet);

        if (PlayerAvailable && player.IsGrounded && ToPlayer.magnitude <= slamRadius)
            HitPlayer(new Hit(slamDamage, transform.position, 1.5f) { knockdown = true });

        // Sóng sát đất toả tròn — giai đoạn 2 dày hơn, phải nhảy đúng nhịp hoặc đứng vào khe
        int count = phase2 ? waveCountPhase2 : waveCount;
        float offset = UnityEngine.Random.Range(0f, 360f / count);
        for (int i = 0; i < count; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, offset + i * 360f / count, 0f) * Vector3.right;
            var wave = Projectile.Spawn(transform.position + dir * 0.9f + Vector3.up * 0.35f, dir * shockwaveSpeed, 0.7f,
                                        tailCharge, playerLayer, groundLayer, Mathf.RoundToInt(ZodiacBalance.Unit * damageScale), true);
            wave.range = shockwaveRange;
            wave.groundWave = true;
            wave.passWalls = true;
            wave.transform.localScale = new Vector3(0.6f, 0.9f, 1f);
        }
        EnterIdle(idleTime);
    }

    void CastFire()
    {
        if (player == null) return;
        Vector3 origin = body.bounds.center + Vector3.up * 0.3f + Vector3.right * direction * 0.6f;
        Vector3 aim = PlayerCenter - origin;
        aim.y = 0f;
        if (aim.sqrMagnitude < 0.01f) aim = Vector3.right * direction;
        aim.Normalize();
        int count = phase2 ? fireCountPhase2 : fireCount;
        for (int i = 0; i < count; i++)
        {
            float a = count == 1 ? 0f : Mathf.Lerp(-fireSpread / 2f, fireSpread / 2f, i / (float)(count - 1));
            Vector3 dir = Quaternion.Euler(0f, a, 0f) * aim;
            var shot = Projectile.Spawn(origin, dir * fireSpeed, 0.5f, fireColor, playerLayer, groundLayer, Mathf.RoundToInt(ZodiacBalance.Unit * damageScale), true);
            shot.range = 18f;
            shot.passWalls = true;
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
            Vector3 at = transform.position + new Vector3((i % 2 == 0 ? -1f : 1f) * (2.5f + i), 3f, (i % 2 == 0 ? 1f : -1f));
            at.z = ClampLane(at.z);
            at.x = ClampRoom(at.x);
            var minion = Instantiate(minionPrefab, at, Quaternion.identity);
            minion.name = "Ma Trơi (gọi ra)";
            ZodiacFx.Ring(at, 1.2f, fireColor, 0.3f);
        }
    }

    void UpdateMarker()
    {
        if (landingMarker == null) return;
        bool show = state == State.LeapWindup || state == State.Leap;
        landingMarker.gameObject.SetActive(show);
        if (!show) return;
        landingMarker.transform.SetPositionAndRotation(new Vector3(leapTarget.x, 0.05f, leapTarget.z), Quaternion.Euler(90f, 0f, 0f));
        float k = 0.85f + Mathf.Sin(Time.time * 16f) * 0.15f;
        landingMarker.transform.localScale = Vector3.one * slamRadius * 2f * k;
    }

    /// <summary>Thân to — đứng xa hơn; đang lao / nhảy thì đi xuyên qua.</summary>
    protected override float MinPlayerDistance => state == State.Dash || state == State.Leap ? 0f : 1.5f;

    protected override void Interrupt()
    {
        // Boss không bị ngắt đòn bởi đòn đánh thường; choáng do chiêu chỉ huỷ phần báo hiệu.
        if (state == State.DashWindup || state == State.LeapWindup || state == State.CastWindup) EnterIdle(idleTime * 0.5f);
    }

    protected override void OnDied()
    {
        if (landingMarker != null) landingMarker.gameObject.SetActive(false);
        base.OnDied();
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
        SetVisualScale(s);
        if (tails == null) return;
        float flicker = winding ? 0.5f + 0.5f * Mathf.Sin(Time.time * 25f) : 0f;
        for (int i = 0; i < tails.Length; i++)
        {
            if (tails[i] == null) continue;
            tails[i].color = Color.Lerp(tailIdle, tailCharge, winding ? 0.5f + flicker * 0.5f : (phase2 ? 0.35f : 0f));
            float sway = Mathf.Sin(Time.time * 3f + i * 0.7f) * 6f;
            tails[i].transform.parent.localRotation = Quaternion.Euler(0f, 0f, (i - (tails.Length - 1) / 2f) * 14f + 30f + sway);
        }
    }
}
