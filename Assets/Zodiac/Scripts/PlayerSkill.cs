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
/// </summary>
[RequireComponent(typeof(ZodiacPlayerController))]
public class PlayerSkill : MonoBehaviour
{
    [Tooltip("Layer có thể bị chiêu đánh trúng (Enemy).")]
    public LayerMask targetLayers;
    public LayerMask groundLayer;

    ZodiacPlayerController controller;
    Health health;
    HitFlash flash;
    InputAction skillAction;
    float readyTime;
    bool hidden;
    readonly List<Collider2D> overlaps = new List<Collider2D>();
    readonly HashSet<Health> hitThisSkill = new HashSet<Health>();

    public SkillConfig Config => controller.Character != null ? controller.Character.skill : null;
    /// <summary>Chiêu đang chiếm quyền điều khiển.</summary>
    public bool IsBusy { get; private set; }
    public bool IsReady => Time.time >= readyTime && !IsBusy;
    public float CooldownRemaining => Mathf.Max(0f, readyTime - Time.time);
    /// <summary>0 = sẵn sàng, 1 = vừa dùng.</summary>
    public float CooldownNormalized => Config == null ? 0f : Mathf.Clamp01(CooldownRemaining / Config.cooldown);
    /// <summary>Tổng số lần đã tung chiêu (để kiểm tra).</summary>
    public int UseCount { get; private set; }
    /// <summary>Vừa tung chiêu (animation, âm thanh).</summary>
    public event Action<SkillType> Used;

    void Awake()
    {
        controller = GetComponent<ZodiacPlayerController>();
        health = GetComponent<Health>();
        flash = GetComponent<HitFlash>();
        skillAction = InputSystem.actions.FindAction("Player/Skill");
        if (skillAction == null)
        {
            Debug.LogError("Thiếu action Player/Skill trong InputSystem_Actions.", this);
            enabled = false;
        }
    }

    void OnEnable() => skillAction?.Enable();

    void Update()
    {
        if (!controller.ControlEnabled)
        {
            if (hidden) EndStealth();
            return;
        }
        if (skillAction.WasPressedThisFrame()) TryUse();
    }

    /// <summary>Tung chiêu nếu đã hồi. Trả về false nếu chưa hồi / đang bận / không đủ điều kiện.</summary>
    public bool TryUse()
    {
        var cfg = Config;
        if (cfg == null || !IsReady || !controller.ControlEnabled) return false;
        if (cfg.type == SkillType.Heal && (!controller.IsGrounded || health == null || health.Current >= health.maxHealth))
            return false;

        UseCount++;
        readyTime = float.PositiveInfinity;   // tính hồi chiêu khi chiêu kết thúc
        hitThisSkill.Clear();
        if (controller.Character != null) ZodiacAudio.Play(controller.Character.skillSound, 0.9f, 0.03f);
        Used?.Invoke(cfg.type);
        switch (cfg.type)
        {
            case SkillType.Dash: StartCoroutine(Dash(cfg)); break;
            case SkillType.Projectile: FireProjectiles(cfg); Finish(cfg); break;
            case SkillType.Burst: Burst(cfg, BodyCenter + Vector2.right * (controller.FacingDirection * cfg.forwardOffset)); Finish(cfg); break;
            case SkillType.Heal: StartCoroutine(Channel(cfg)); break;
            case SkillType.Stealth: StartCoroutine(Stealth(cfg)); break;
            case SkillType.GroundPound: StartCoroutine(GroundPound(cfg)); break;
        }
        return true;
    }

    void Finish(SkillConfig cfg)
    {
        IsBusy = false;
        readyTime = Time.time + cfg.cooldown;
    }

    Vector2 BodyCenter => controller.BodyCollider.bounds.center;
    Color Theme => controller.Character != null ? controller.Character.themeColor : Color.white;

    // ───────── Lướt / vồ ─────────

    IEnumerator Dash(SkillConfig cfg)
    {
        IsBusy = true;
        var rb = controller.Body;
        float dir = controller.FacingDirection;
        float rad = cfg.dashAngle * Mathf.Deg2Rad;
        Vector2 velocity = new Vector2(Mathf.Cos(rad) * dir, Mathf.Sin(rad)) * cfg.dashSpeed;
        controller.SetOverride(true, 0f);
        if (cfg.dashInvulnerable) health?.SetInvulnerable(cfg.dashTime + 0.1f);
        ZodiacFx.Puff((Vector2)transform.position + Vector2.up * 0.2f, Theme);

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
        else rb.linearVelocity = new Vector2(velocity.x * 0.35f, Mathf.Min(velocity.y * 0.4f, 4f));
        Finish(cfg);
    }

    /// <summary>Gây sát thương các quái chồng lên hộp quanh người (mỗi con một lần mỗi chiêu).</summary>
    bool HitAround(Vector2 center, Vector2 size, SkillConfig cfg)
    {
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(targetLayers);
        Physics2D.OverlapBox(center, size, 0f, filter, overlaps);
        bool any = false;
        foreach (var col in overlaps)
        {
            var target = col.GetComponentInParent<Health>();
            if (target == null || target.IsDead || !hitThisSkill.Add(target)) continue;
            if (target.TakeDamage(new Hit(cfg.damage, transform.position, cfg.knockback, cfg.stunTime)))
                GameFeel.Impact(col.ClosestPoint(BodyCenter), Theme, cfg.damage >= 3);
            any = true;
        }
        return any;
    }

    // ───────── Bắn đạn ─────────

    void FireProjectiles(SkillConfig cfg)
    {
        float facing = controller.FacingDirection;
        Vector2 origin = cfg.groundWave
            ? new Vector2(transform.position.x, transform.position.y + 0.5f)
            : BodyCenter + new Vector2(facing * 0.5f, 0.2f);
        float[] dirs = cfg.bothDirections ? new[] { -1f, 1f } : new[] { facing };
        foreach (float d in dirs)
        {
            for (int i = 0; i < cfg.projectileCount; i++)
            {
                float a = cfg.projectileCount == 1 ? 0f
                    : Mathf.Lerp(-cfg.spreadAngle / 2f, cfg.spreadAngle / 2f, i / (float)(cfg.projectileCount - 1));
                Vector2 dir = Quaternion.Euler(0f, 0f, a * d) * new Vector2(d, 0f);
                var shot = Projectile.Spawn(origin + new Vector2(d * 0.3f, 0f), dir * cfg.projectileSpeed, cfg.projectileSize,
                                            Theme, targetLayers, groundLayer, cfg.damage, false);
                shot.range = cfg.projectileRange;
                shot.pierce = cfg.pierce;
                shot.boomerang = cfg.boomerang;
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

    void Burst(SkillConfig cfg, Vector2 center)
    {
        GameFeel.Shake(0.2f + cfg.radius * 0.04f);
        ZodiacParticles.Sparkle(center, Theme, 18, cfg.radius * 0.6f);
        ZodiacFx.Ring(center, cfg.radius, new Color(Theme.r, Theme.g, Theme.b, 0.6f), 0.4f);
        ZodiacFx.Ring(center, cfg.radius * 0.6f, new Color(1f, 1f, 1f, 0.5f), 0.25f);
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(targetLayers);
        Physics2D.OverlapCircle(center, cfg.radius, filter, overlaps);
        foreach (var col in overlaps)
        {
            var target = col.GetComponentInParent<Health>();
            if (target == null || target.IsDead || !hitThisSkill.Add(target)) continue;
            if (cfg.damage > 0) target.TakeDamage(new Hit(cfg.damage, center, cfg.knockback, cfg.stunTime));
            else if (target.TryGetComponent(out EnemyBase enemy)) enemy.Stun(cfg.stunTime);
        }
        if (!cfg.clearsProjectiles) return;
        for (int i = Projectile.Active.Count - 1; i >= 0; i--)
        {
            var shot = Projectile.Active[i];
            if (shot.hostile && Vector2.Distance(shot.transform.position, center) <= cfg.radius) shot.Pop();
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
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
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
        if (!hidden || Config == null) return 1f;
        EndStealth();
        return Config.backstabMultiplier;
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
                rb.linearVelocity = new Vector2(0f, 10f);
                yield return new WaitForFixedUpdate();
            }
        }
        // Khựng lại trên không một nhịp — báo hiệu và cho cảm giác nặng.
        for (float t = 0f; t < 0.12f; t += Time.fixedDeltaTime)
        {
            rb.linearVelocity = Vector2.zero;
            yield return new WaitForFixedUpdate();
        }
        for (float t = 0f; t < 1.5f && controller.ControlEnabled; t += Time.fixedDeltaTime)
        {
            health?.SetInvulnerable(0.1f);   // bất tử trong lúc lao xuống
            rb.linearVelocity = new Vector2(0f, -cfg.diveSpeed);
            HitAround(BodyCenter, controller.BodyCollider.bounds.size, cfg);
            yield return new WaitForFixedUpdate();
            if (controller.IsGrounded) break;
        }
        controller.SetOverride(false);
        rb.linearVelocity = Vector2.zero;
        health?.SetInvulnerable(0.25f);
        Vector2 feet = new Vector2(transform.position.x, transform.position.y + 0.3f);
        ZodiacFx.Puff(feet, new Color(0.7f, 0.55f, 0.4f, 0.8f), 2.5f);
        ZodiacParticles.Dust(feet, Vector2.left, 10);
        ZodiacParticles.Dust(feet, Vector2.right, 10);
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
