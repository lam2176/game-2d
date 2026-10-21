using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Chiêu thức riêng của từng con giáp — action Player/Skill (K, chuột phải, RB tay cầm).
/// Kiểu chiêu và thông số đọc từ <see cref="ZodiacCharacter.skill"/> của nhân vật đang chơi:
/// Dash / Projectile / Burst / Heal / Stealth / GroundPound (xem <see cref="SkillType"/>).
/// Trong lúc chiêu chiếm quyền di chuyển (lướt, dậm đất, gồng hồi máu) controller ngừng điều khiển vận tốc
/// và đòn đánh thường bị khoá. Hồi chiêu tính từ lúc chiêu kết thúc.
/// Mỗi lần tung tốn <see cref="SkillConfig.energyCost"/> Khí (<see cref="PlayerEnergy"/>) — thiếu Khí thì không tung được.
/// Tuyệt kỹ (action Player/Ultimate — R, RT): chiêu riêng của từng con (PlayerSkill.Ultimates.cs), con nào chưa có thì
/// dùng bản cường hoá của chiêu (<see cref="SkillConfig.Empowered"/>); cần đầy thanh Khí và tốn hết;
/// có cảnh cắt chân dung + chậm thời gian (<see cref="UltimateCutIn"/>).
/// </summary>
[RequireComponent(typeof(ZodiacPlayerController))]
public partial class PlayerSkill : MonoBehaviour
{
    [Tooltip("Layer có thể bị chiêu đánh trúng (Enemy).")]
    public LayerMask targetLayers;
    public LayerMask groundLayer;

    ZodiacPlayerController controller;
    PlayerEnergy energy;
    CharacterStats stats;
    Health health;
    HitFlash flash;
    InputAction skillAction, ultimateAction;
    float readyTime;
    bool hidden;
    float backstab = 1f;
    readonly Collider[] overlaps = new Collider[24];
    readonly HashSet<Health> hitThisSkill = new HashSet<Health>();

    public SkillConfig Config => controller.Character != null ? controller.Character.skill : null;
    /// <summary>Chiêu đang chiếm quyền điều khiển.</summary>
    public bool IsBusy { get; private set; }
    public bool IsReady => Time.time >= readyTime && !IsBusy;
    /// <summary>Đủ Khí để tung chiêu.</summary>
    public bool HasEnergy => energy == null || Config == null || energy.CanSpend(Config.energyCost);
    public float CooldownRemaining => Mathf.Max(0f, readyTime - Time.time);
    /// <summary>0 = sẵn sàng, 1 = vừa dùng.</summary>
    public float CooldownNormalized => Config == null ? 0f : Mathf.Clamp01(CooldownRemaining / Config.cooldown);
    /// <summary>Tổng số lần đã tung chiêu (để kiểm tra).</summary>
    public int UseCount { get; private set; }
    /// <summary>Vừa tung chiêu (animation, âm thanh).</summary>
    public event Action<SkillType> Used;
    /// <summary>Vừa tung tuyệt kỹ.</summary>
    public event Action UltimateUsed;
    /// <summary>Đủ Khí (đầy thanh) và đang rảnh để tung tuyệt kỹ.</summary>
    public bool UltimateReady => Config != null && IsReady && (energy == null || energy.Current >= energy.max);
    /// <summary>Tổng số lần tung tuyệt kỹ (để kiểm tra).</summary>
    public int UltimateCount { get; private set; }

    void Awake()
    {
        controller = GetComponent<ZodiacPlayerController>();
        energy = GetComponent<PlayerEnergy>();
        health = GetComponent<Health>();
        flash = GetComponent<HitFlash>();
        skillAction = InputSystem.actions.FindAction("Player/Skill");
        ultimateAction = InputSystem.actions.FindAction("Player/Ultimate");
        if (skillAction == null)
        {
            Debug.LogError("Thiếu action Player/Skill trong InputSystem_Actions.", this);
            enabled = false;
        }
    }

    void OnEnable()
    {
        skillAction?.Enable();
        ultimateAction?.Enable();
    }

    void Update()
    {
        if (!controller.ControlEnabled)
        {
            if (hidden) EndStealth();
            return;
        }
        if (skillAction.WasPressedThisFrame()) TryUse();
        if (ultimateAction != null && ultimateAction.WasPressedThisFrame()) TryUseUltimate();
    }

    /// <summary>Tung chiêu nếu đã hồi. Trả về false nếu chưa hồi / đang bận / không đủ điều kiện.</summary>
    public bool TryUse()
    {
        var cfg = Config;
        if (cfg == null || !IsReady || !controller.ControlEnabled || controller.IsOverridden || !HasEnergy) return false;
        if (cfg.type == SkillType.Heal && (!controller.IsGrounded || health == null || health.Current >= health.maxHealth))
            return false;

        energy?.TrySpend(cfg.energyCost);
        UseCount++;
        Execute(cfg);
        return true;
    }

    /// <summary>Tung tuyệt kỹ nếu đầy Khí. Trả về false nếu chưa đủ / đang bận.</summary>
    public bool TryUseUltimate()
    {
        var baseCfg = Config;
        if (baseCfg == null || !UltimateReady || !controller.ControlEnabled || controller.IsOverridden) return false;
        bool unique = HasUniqueUltimate;
        if (!unique && baseCfg.type == SkillType.Heal && (!controller.IsGrounded || health == null || health.Current >= health.maxHealth))
            return false;
        var cfg = baseCfg.Empowered();
        energy?.TrySpend(energy.max);
        UltimateCount++;
        var c = controller.Character;
        UltimateCutIn.Show(c, Theme);
        GameFeel.Shake(0.5f);
        if (unique) RunUniqueUltimate(baseCfg);
        else Execute(cfg);
        UltimateUsed?.Invoke();
        return true;
    }

    void Execute(SkillConfig cfg)
    {
        readyTime = float.PositiveInfinity;   // tính hồi chiêu khi chiêu kết thúc
        hitThisSkill.Clear();
        if (controller.Character != null) ZodiacAudio.Play(controller.Character.skillSound, 0.9f, 0.03f);
        Used?.Invoke(cfg.type);
        switch (cfg.type)
        {
            case SkillType.Dash: StartCoroutine(Dash(cfg)); break;
            case SkillType.Projectile: FireProjectiles(cfg); Finish(cfg); break;
            case SkillType.Burst: Burst(cfg, BodyCenter + Vector3.right * (controller.FacingDirection * cfg.forwardOffset)); Finish(cfg); break;
            case SkillType.Heal: StartCoroutine(Channel(cfg)); break;
            case SkillType.Stealth: StartCoroutine(Stealth(cfg)); break;
            case SkillType.GroundPound: StartCoroutine(GroundPound(cfg)); break;
        }
    }

    void Finish(SkillConfig cfg)
    {
        IsBusy = false;
        readyTime = Time.time + cfg.cooldown;
    }

    Vector3 BodyCenter => controller.BodyCenter;
    Color Theme => controller.Character != null ? controller.Character.themeColor : Color.white;

    // ───────── Lướt / vồ ─────────

    IEnumerator Dash(SkillConfig cfg)
    {
        IsBusy = true;
        var rb = controller.Body;
        // Hướng lướt trên mặt đất: theo phím (chéo đổi làn được), không bấm thì theo hướng nhìn; dashAngle > 0 = vồ chếch lên.
        Vector2 input = controller.MoveInput;
        Vector3 flat = input.sqrMagnitude > 0.09f ? new Vector3(input.x, 0f, input.y * 0.7f).normalized
                                                  : new Vector3(controller.FacingDirection, 0f, 0f);
        if (Mathf.Abs(flat.x) > 0.1f) controller.SetFacing(flat.x);
        float rad = cfg.dashAngle * Mathf.Deg2Rad;
        Vector3 velocity = (flat * Mathf.Cos(rad) + Vector3.up * Mathf.Sin(rad)) * cfg.dashSpeed;
        controller.SetOverride(true, 0f);
        if (cfg.dashInvulnerable) health?.SetInvulnerable(cfg.dashTime + 0.1f);
        ZodiacFx.Puff(transform.position + Vector3.up * 0.2f, Theme);

        bool bounced = false;
        float nextImage = 0f;
        for (float t = 0f; t < cfg.dashTime && controller.ControlEnabled; t += Time.fixedDeltaTime)
        {
            rb.linearVelocity = velocity;
            if (t >= nextImage)
            {
                ZodiacFx.Afterimage(controller.spriteRenderer, new Color(Theme.r, Theme.g, Theme.b, 0.5f));
                nextImage = t + 0.035f;
            }
            if (cfg.damage > 0 && HitAround(BodyCenter, controller.BodyCollider.bounds.size * 1.1f, cfg) && cfg.dashBounceOnHit)
            {
                bounced = true;
                break;
            }
            yield return new WaitForFixedUpdate();
        }

        controller.SetOverride(false);
        if (bounced) controller.Bounce(11f);
        else if (!rb.isKinematic) rb.linearVelocity = new Vector3(velocity.x * 0.35f, Mathf.Min(velocity.y * 0.4f, 4f), velocity.z * 0.35f);
        Finish(cfg);
    }

    /// <summary>Gây sát thương các quái chồng lên hộp quanh người (mỗi con một lần mỗi chiêu).</summary>
    bool HitAround(Vector3 center, Vector3 size, SkillConfig cfg)
    {
        int count = Physics.OverlapBoxNonAlloc(center, size * 0.5f, overlaps, Quaternion.identity, targetLayers, QueryTriggerInteraction.Ignore);
        bool any = false;
        for (int i = 0; i < count; i++)
        {
            var col = overlaps[i];
            var target = col.GetComponentInParent<Health>();
            if (target == null || target.IsDead || !hitThisSkill.Add(target)) continue;
            Hit hit = CreateHit(cfg, transform.position);
            if (target.TakeDamage(hit))
                GameFeel.Impact(col.ClosestPoint(BodyCenter), Theme, cfg.damage >= 3 * ZodiacBalance.Unit || hit.crit);
            any = true;
        }
        return any;
    }

    /// <summary>CharacterStats do controller thêm lúc chọn nhân vật — lấy lười vì có thể chưa có khi Awake.</summary>
    CharacterStats Stats => stats != null ? stats : stats = GetComponent<CharacterStats>();
    /// <summary>Trang bị / nâng cấp tăng công thì chiêu mạnh lên cùng tỉ lệ.</summary>
    float AttackScale => Stats != null ? Stats.AttackScale : 1f;

    Hit CreateHit(SkillConfig cfg, Vector3 source) =>
        CharacterStats.CreateHit(Stats, cfg.damage * AttackScale, source, cfg.knockback, cfg.stunTime);

    // ───────── Bắn đạn ─────────

    void FireProjectiles(SkillConfig cfg)
    {
        float facing = controller.FacingDirection;
        Vector3 origin = cfg.groundWave
            ? transform.position + Vector3.up * 0.45f
            : BodyCenter + new Vector3(facing * 0.5f, 0.2f, 0f);
        float[] dirs = cfg.bothDirections ? new[] { -1f, 1f } : new[] { facing };
        foreach (float d in dirs)
        {
            for (int i = 0; i < cfg.projectileCount; i++)
            {
                float a = cfg.projectileCount == 1 ? 0f
                    : Mathf.Lerp(-cfg.spreadAngle / 2f, cfg.spreadAngle / 2f, i / (float)(cfg.projectileCount - 1));
                // Chùm đạn toả theo chiều sâu (quét nhiều làn), không toả lên trời
                Vector3 dir = Quaternion.Euler(0f, a * d, 0f) * new Vector3(d, 0f, 0f);
                var shot = Projectile.Spawn(origin + new Vector3(d * 0.3f, 0f, 0f), dir * cfg.projectileSpeed, cfg.projectileSize,
                                            Theme, targetLayers, groundLayer, Mathf.RoundToInt(cfg.damage * AttackScale), false);
                shot.attacker = Stats;
                shot.range = cfg.projectileRange;
                shot.pierce = cfg.pierce;
                shot.boomerang = cfg.boomerang;
                shot.groundWave = cfg.groundWave;
                shot.owner = transform;
                shot.knockback = cfg.knockback;
                shot.stun = cfg.stunTime;
                if (cfg.boomerang) shot.spin = -720f * d;
                if (cfg.groundWave) shot.transform.localScale = new Vector3(cfg.projectileSize * 0.6f, cfg.projectileSize * 1.4f, 1f);
            }
        }
        ZodiacFx.Ring(origin, 0.9f, Theme, 0.2f);
    }

    // ───────── Sóng âm ─────────

    void Burst(SkillConfig cfg, Vector3 center)
    {
        GameFeel.Shake(0.2f + cfg.radius * 0.04f);
        ZodiacParticles.Sparkle(center, Theme, 18, cfg.radius * 0.6f);
        // Vòng nằm trên mặt đất cho thấy đúng vùng ảnh hưởng theo cả chiều sâu, vòng đứng nhỏ cho cảm giác nổ
        ZodiacFx.Ring(new Vector3(center.x, transform.position.y, center.z), cfg.radius, new Color(Theme.r, Theme.g, Theme.b, 0.6f), 0.4f, flat: true);
        ZodiacFx.Ring(center, cfg.radius * 0.6f, new Color(1f, 1f, 1f, 0.5f), 0.25f);
        int count = Physics.OverlapSphereNonAlloc(center, cfg.radius, overlaps, targetLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var col = overlaps[i];
            var target = col.GetComponentInParent<Health>();
            if (target == null || target.IsDead || !hitThisSkill.Add(target)) continue;
            if (cfg.damage > 0) target.TakeDamage(CreateHit(cfg, center));
            else if (target.TryGetComponent(out EnemyBase enemy)) enemy.Stun(cfg.stunTime);
        }
        if (!cfg.clearsProjectiles) return;
        for (int i = Projectile.Active.Count - 1; i >= 0; i--)
        {
            var shot = Projectile.Active[i];
            if (shot.hostile && Vector3.Distance(shot.transform.position, center) <= cfg.radius) shot.Pop();
        }
    }

    // ───────── Hồi máu ─────────

    IEnumerator Channel(SkillConfig cfg)
    {
        IsBusy = true;
        var rb = controller.Body;
        controller.SetOverride(true, controller.gravityScale);
        bool interrupted = false;
        void OnHit(Hit _) => interrupted = true;
        health.Damaged += OnHit;

        float nextRing = 0f;
        for (float t = 0f; t < cfg.channelTime && !interrupted && controller.ControlEnabled; t += Time.deltaTime)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            if (t >= nextRing)
            {
                ZodiacFx.Ring(BodyCenter, 1.6f, new Color(0.5f, 1f, 0.6f, 0.5f), 0.3f);
                nextRing = t + 0.2f;
            }
            yield return null;
        }

        health.Damaged -= OnHit;
        controller.SetOverride(false);
        if (!interrupted && controller.ControlEnabled)
        {
            health.Heal(cfg.healAmount);
            ZodiacParticles.Sparkle(BodyCenter, new Color(0.5f, 1f, 0.6f), 20, 0.8f);
            ZodiacFx.Ring(BodyCenter, 2.5f, new Color(0.4f, 1f, 0.5f, 0.8f), 0.45f);
            Finish(cfg);
        }
        else
        {
            IsBusy = false;
            readyTime = Time.time + 1f;   // bị đánh ngắt: hồi ngắn rồi thử lại được
        }
    }

    // ───────── Tàng hình ─────────

    IEnumerator Stealth(SkillConfig cfg)
    {
        hidden = true;
        backstab = cfg.backstabMultiplier;
        controller.IsHidden = true;
        health?.SetInvulnerable(cfg.stealthDuration);
        if (flash != null) flash.tint = new Color(0.6f, 1f, 0.8f, 0.3f);
        ZodiacFx.Ring(BodyCenter, 1.8f, new Color(0.2f, 0.4f, 0.3f, 0.6f), 0.3f);
        float end = Time.time + cfg.stealthDuration;
        while (hidden && Time.time < end) yield return null;
        if (hidden) EndStealth();
        Finish(cfg);
    }

    void EndStealth(bool effect = true)
    {
        hidden = false;
        controller.IsHidden = false;
        if (flash != null) flash.tint = Color.white;
        if (effect) ZodiacFx.Ring(BodyCenter, 1.2f, new Color(0.2f, 0.4f, 0.3f, 0.5f), 0.2f);
    }

    /// <summary>
    /// Gọi khi đòn đánh thường trúng: đang tàng hình thì trả về hệ số đánh lén và hiện hình, không thì 1.
    /// </summary>
    public float ConsumeBackstab()
    {
        if (!hidden) return 1f;
        EndStealth();
        return backstab;
    }

    // ───────── Dậm đất ─────────

    IEnumerator GroundPound(SkillConfig cfg)
    {
        IsBusy = true;
        var rb = controller.Body;
        controller.SetOverride(true, 0f);
        if (controller.IsGrounded)
        {
            // Đang trên đất: bật lên trước rồi mới dậm (như Mario).
            for (float t = 0f; t < 0.22f; t += Time.fixedDeltaTime)
            {
                rb.linearVelocity = new Vector3(0f, 10f, 0f);
                yield return new WaitForFixedUpdate();
            }
        }
        // Khựng lại trên không một nhịp — báo hiệu và cho cảm giác nặng.
        for (float t = 0f; t < 0.12f; t += Time.fixedDeltaTime)
        {
            rb.linearVelocity = Vector3.zero;
            yield return new WaitForFixedUpdate();
        }
        for (float t = 0f; t < 1.5f && controller.ControlEnabled; t += Time.fixedDeltaTime)
        {
            health?.SetInvulnerable(0.1f);   // bất tử trong lúc lao xuống
            rb.linearVelocity = new Vector3(0f, -cfg.diveSpeed, 0f);
            HitAround(BodyCenter, controller.BodyCollider.bounds.size, cfg);
            yield return new WaitForFixedUpdate();
            if (controller.IsGrounded) break;
        }
        controller.SetOverride(false);
        if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
        health?.SetInvulnerable(0.25f);
        Vector3 feet = transform.position + Vector3.up * 0.3f;
        ZodiacFx.Puff(feet, new Color(0.7f, 0.55f, 0.4f, 0.8f), 2.5f);
        ZodiacParticles.Dust(feet, Vector3.left, 10);
        ZodiacParticles.Dust(feet, Vector3.right, 10);
        ZodiacParticles.Debris(feet, new Color(0.55f, 0.42f, 0.3f), 10, 0.8f);
        GameFeel.HitStop(0.06f);
        GameFeel.Shake(0.45f);
        Burst(cfg, feet);
        Finish(cfg);
    }

    void OnDisable()
    {
        if (hidden) EndStealth(effect: false);   // có thể đang huỷ scene — không tạo object mới
        if (IsBusy && controller != null) controller.SetOverride(false);
        IsBusy = false;
    }
}
