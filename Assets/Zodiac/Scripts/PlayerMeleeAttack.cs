using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Đánh thường bằng action Player/Attack (J, Q, chuột trái, Enter, nút West gamepad) — kiểu beat 'em up:
///   • Combo 3 đòn trên đất: chém – chém ngược – đòn kết (gấp đôi sát thương, QUẬT NGÃ quái).
///     Bấm tiếp trong comboWindow sau khi đòn trước kết thúc thì nối đòn, quá thì quay về đòn 1.
///   • Đang nhảy: đá bay (quật ngã, nhân thêm sát thương đòn trên không của nhân vật); trúng thì nảy nhẹ lên để đá tiếp.
///   • Đang chạy (giữ Shift): đòn lao — trượt tới trước, quật ngã.
/// Mỗi đòn: startup chờ ra đòn → hitbox có hiệu lực trong active → hết; bấm lại được sau recovery.
/// Mỗi mục tiêu chỉ nhận sát thương một lần trong một đòn (dù hitbox còn chạm nhiều frame).
///
/// Hitbox là BoxCollider 3D (con của Facing, luôn TẮT): script chỉ dùng hình dạng của nó để quét Physics.OverlapBox.
/// Bề dày theo trục Z của hộp = độ lệch làn tối đa vẫn đánh trúng (đứng khác làn thì chém hụt — phải canh làn như Streets of Rage).
/// Sát thương lấy từ công của <see cref="CharacterStats"/> (có dao động, chí mạng), nhân thêm khi đánh lén sau tàng hình (Rắn)
/// và gây độc nếu có nội tại. Chém trúng đạn của quái thì đạn tan (đỡ đạn bằng đòn đánh).
/// </summary>
[RequireComponent(typeof(ZodiacPlayerController))]
public class PlayerMeleeAttack : MonoBehaviour
{
    [Serializable]
    public struct Swing
    {
        [Min(0f)] public float startup;
        [Min(0.02f)] public float active;
        [Tooltip("Từ lúc bấm tới khi bấm được đòn tiếp (giây).")]
        [Min(0.05f)] public float recovery;
        [Min(0.1f)] public float damageMultiplier;
        public float knockback;
        public bool knockdown;
        [Tooltip("Lướt tới trước khi ra đòn (unit/s).")]
        public float lunge;
    }

    public enum Kind { Combo, Air, Dash }

    [Header("Hitbox")]
    [Tooltip("BoxCollider 3D (để tắt), là con của Facing để tự lật theo hướng nhìn.")]
    public BoxCollider hitbox;
    [Tooltip("Layer có thể bị đánh trúng (Enemy).")]
    public LayerMask targetLayers;
    [Tooltip("Vệt chém hiện trong lúc hitbox có hiệu lực.")]
    public GameObject slashEffect;

    [Header("Đòn đánh")]
    [Min(1)] public int damage = ZodiacBalance.Unit;
    [Tooltip("Đòn 1, 2, 3 của combo trên đất.")]
    public Swing[] combo =
    {
        new Swing { startup = 0.04f, active = 0.1f, recovery = 0.24f, damageMultiplier = 1f, knockback = 0.6f, lunge = 1.5f },
        new Swing { startup = 0.04f, active = 0.1f, recovery = 0.24f, damageMultiplier = 1f, knockback = 0.6f, lunge = 1.5f },
        new Swing { startup = 0.09f, active = 0.14f, recovery = 0.42f, damageMultiplier = 2f, knockback = 1.6f, knockdown = true, lunge = 3f },
    };
    public Swing air = new Swing { startup = 0.05f, active = 0.22f, recovery = 0.35f, damageMultiplier = 1.5f, knockback = 1.4f, knockdown = true };
    public Swing dash = new Swing { startup = 0.05f, active = 0.18f, recovery = 0.45f, damageMultiplier = 1.8f, knockback = 1.8f, knockdown = true, lunge = 9f };
    [Tooltip("Thời gian được nối đòn sau khi đòn trước hết hiệu lực (giây).")]
    [Min(0f)] public float comboWindow = 0.4f;
    [Tooltip("Vận tốc nảy lên khi đá bay trúng quái.")]
    [Min(0f)] public float airBounce = 6f;

    enum Phase { Ready, Startup, Active }

    ZodiacPlayerController controller;
    PlayerSkill skill;
    PlayerEnergy energy;
    CharacterStats stats;
    PlayerDodge dodge;
    PlayerGrab grab;
    PlayerPassives passives;
    LayerMask groundLayers;
    InputAction attackAction;
    readonly Collider[] overlaps = new Collider[16];
    readonly HashSet<Health> hitThisSwing = new HashSet<Health>();
    Phase phase;
    float phaseEndTime, nextAttackTime, comboExpires;
    int comboStep;
    Swing current;
    bool bounced;
    Vector3 baseHitboxCenter, baseHitboxSize;

    public bool IsHitboxActive => phase == Phase.Active;
    /// <summary>Đang trong một đòn (startup hoặc active) — controller vẫn chạy nhưng không đổi hướng nhìn.</summary>
    public bool IsSwinging => phase != Phase.Ready;
    public float CooldownRemaining => Mathf.Max(0f, nextAttackTime - Time.time);
    /// <summary>0 = đánh được, 1 = vừa ra đòn (cho ô Q trên HUD).</summary>
    public float CooldownNormalized => current.recovery > 0f ? Mathf.Clamp01(CooldownRemaining / current.recovery) : 0f;
    /// <summary>Tổng số đòn đã ra (để kiểm tra).</summary>
    public int SwingCount { get; private set; }
    /// <summary>Bước combo của đòn gần nhất (0..2); đòn trên không / đòn lao báo qua <see cref="LastKind"/>.</summary>
    public int LastComboStep { get; private set; }
    public Kind LastKind { get; private set; }
    /// <summary>Ra đòn: (bước combo, loại đòn).</summary>
    public event Action<int, Kind> Swung;

    void Awake()
    {
        controller = GetComponent<ZodiacPlayerController>();
        skill = GetComponent<PlayerSkill>();
        energy = GetComponent<PlayerEnergy>();
        dodge = GetComponent<PlayerDodge>();
        grab = GetComponent<PlayerGrab>();
        passives = GetComponent<PlayerPassives>();
        var skillComp = GetComponent<PlayerSkill>();
        groundLayers = skillComp != null ? skillComp.groundLayer : (LayerMask)0;
        attackAction = InputSystem.actions.FindAction("Player/Attack");
        if (attackAction == null || hitbox == null)
        {
            Debug.LogError("Thiếu action Player/Attack hoặc chưa gán Hitbox.", this);
            enabled = false;
            return;
        }
        hitbox.enabled = false;
        baseHitboxCenter = hitbox.center;
        baseHitboxSize = hitbox.size;
        if (slashEffect != null && slashEffect.TryGetComponent(out SpriteRenderer sr)) sr.sprite = ZodiacFx.Crescent;
        SetActiveVisual(false);
    }

    void OnEnable() => attackAction?.Enable();

    void Update()
    {
        if (!controller.ControlEnabled || controller.IsKnockedDown)
        {
            if (phase != Phase.Ready) EndSwing();
            return;
        }

        if (attackAction.WasPressedThisFrame() && Time.time >= nextAttackTime && (skill == null || !skill.IsBusy) &&
            (dodge == null || !dodge.IsDodging) && (grab == null || !grab.IsHolding) && !HoldingWeapon) StartSwing();

        if (phase == Phase.Startup && Time.time >= phaseEndTime)
        {
            phase = Phase.Active;
            phaseEndTime = Time.time + current.active;
            SetActiveVisual(true);
            if (current.lunge > 0f && controller.IsGrounded)
            {
                var v = controller.Body.linearVelocity;
                controller.ApplyKnockback(new Vector3(controller.FacingDirection * current.lunge, v.y, v.z * 0.3f), current.active * 0.8f);
            }
        }
        else if (phase == Phase.Active && Time.time >= phaseEndTime)
        {
            EndSwing();
        }
    }

    void FixedUpdate()
    {
        if (phase == Phase.Active) DetectHits();
    }

    /// <summary>Ra đòn theo trạng thái: trên không → đá bay, đang chạy → đòn lao, còn lại → combo.</summary>
    public void StartSwing() =>
        StartSwing(!controller.IsGrounded ? Kind.Air : controller.IsRunning ? Kind.Dash : Kind.Combo);

    public void StartSwing(Kind kind)
    {
        LastKind = kind;
        switch (kind)
        {
            case Kind.Air:
                current = air;
                LastComboStep = 0;
                break;
            case Kind.Dash:
                current = dash;
                LastComboStep = 2;
                comboStep = 0;
                break;
            default:
                if (Time.time > comboExpires) comboStep = 0;
                current = combo[Mathf.Clamp(comboStep, 0, combo.Length - 1)];
                LastComboStep = comboStep;
                comboStep = (comboStep + 1) % combo.Length;
                break;
        }

        if (stats == null) stats = GetComponent<CharacterStats>();
        if (stats != null) damage = stats.Attack;
        else if (controller.Character != null) damage = controller.Character.attackDamage;
        PlaceHitbox(kind);
        SwingCount++;
        bounced = false;
        hitThisSwing.Clear();
        nextAttackTime = Time.time + current.recovery;
        phase = Phase.Startup;
        phaseEndTime = Time.time + current.startup;

        var r = ZodiacResources.Get;
        if (r != null) ZodiacAudio.Play(current.knockdown ? r.swingHeavy : r.swing, 0.55f, 0.1f);
        Swung?.Invoke(LastComboStep, kind);
    }

    /// <summary>Hitbox ngang giữ nguyên chỉnh sửa trong Inspector; đá bay thấp và rộng hơn, đòn kết/đòn lao to hơn.</summary>
    void PlaceHitbox(Kind kind)
    {
        Vector3 center = baseHitboxCenter, size = baseHitboxSize;
        if (kind == Kind.Air)
        {
            center = new Vector3(baseHitboxCenter.x, baseHitboxCenter.y * 0.55f, 0f);
            size = new Vector3(baseHitboxSize.x * 1.1f, baseHitboxSize.y * 1.3f, baseHitboxSize.z);
        }
        else if (current.knockdown)
        {
            size = Vector3.Scale(baseHitboxSize, new Vector3(1.25f, 1.15f, 1.1f));
        }
        hitbox.center = center;
        hitbox.size = size;
        if (slashEffect != null)
        {
            var t = slashEffect.transform;
            t.localPosition = hitbox.transform.localPosition + center;
            t.localScale = new Vector3(size.x * 1.15f, size.y * 1.15f, 1f);
            t.localRotation = Quaternion.Euler(0f, 0f, kind == Kind.Air ? -35f : 0f);
            if (slashEffect.TryGetComponent(out SlashEffect fx)) fx.Configure(current.active, LastComboStep == 1, current.knockdown);
        }
    }

    PlayerWeapon weapon;
    /// <summary>Đang cầm vũ khí nhặt được — phím Đánh do <see cref="PlayerWeapon"/> xử lý.</summary>
    bool HoldingWeapon => (weapon != null || TryGetComponent(out weapon)) && weapon.IsHolding;

    void EndSwing()
    {
        if (phase == Phase.Active || phase == Phase.Startup) comboExpires = Time.time + comboWindow;
        phase = Phase.Ready;
        SetActiveVisual(false);
    }

    void DetectHits()
    {
        GetHitboxWorld(out Vector3 center, out Vector3 halfExtents);
        int count = Physics.OverlapBoxNonAlloc(center, halfExtents, overlaps, Quaternion.identity, targetLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            var col = overlaps[i];
            var target = col.GetComponentInParent<Health>();
            if (target == null || target.gameObject == gameObject || target.IsDead) continue;
            if (!hitThisSwing.Add(target)) continue;   // đã trúng trong đòn này
            float multiplier = (skill != null ? skill.ConsumeBackstab() : 1f) * current.damageMultiplier;
            if (passives != null) multiplier *= passives.DamageMultiplier(LastKind);
            // Nguồn sát thương đặt phía sau người chơi theo hướng chém → quái bị đẩy theo hướng chém
            Vector3 source = transform.position - Vector3.right * (controller.FacingDirection * 2f);
            Hit hit = CharacterStats.CreateHit(stats, damage * multiplier, source, current.knockback);
            hit.knockdown = current.knockdown;
            bool counter = dodge != null && dodge.CounterReady;
            if (counter)
            {
                // Phản đòn sau né hoàn hảo: chí mạng, gấp đôi, quật ngã
                hit.amount *= 2;
                hit.crit = true;
                hit.knockdown = true;
            }
            if (target.TakeDamage(hit))
            {
                if (counter && dodge.ConsumeCounter())
                {
                    ZodiacFx.Ring(col.ClosestPoint(center), 2f, new Color(1f, 0.95f, 0.6f), 0.3f);
                    GameFeel.Shake(0.4f);
                }
                passives?.OnMeleeHit(target, LastKind, LastComboStep, targetLayers, groundLayers);
                energy?.Gain(current.knockdown ? energy.hitGain * 2 : energy.hitGain);
                Color spark = controller.Character != null ? controller.Character.themeColor : Color.white;
                GameFeel.Impact(col.ClosestPoint(center), spark, current.knockdown || multiplier >= 2.5f || hit.crit);
                if (LastKind == Kind.Air && !bounced)
                {
                    bounced = true;
                    controller.Bounce(airBounce);
                }
            }
        }
        PopEnemyShots(center, halfExtents);
    }

    void PopEnemyShots(Vector3 center, Vector3 halfExtents)
    {
        var area = new Bounds(center, halfExtents * 2f);
        for (int i = Projectile.Active.Count - 1; i >= 0; i--)
        {
            var shot = Projectile.Active[i];
            if (shot.hostile && area.Contains(shot.transform.position)) shot.Pop();
        }
    }

    void GetHitboxWorld(out Vector3 center, out Vector3 halfExtents)
    {
        Transform t = hitbox.transform;
        center = t.TransformPoint(hitbox.center);
        Vector3 s = t.lossyScale;
        halfExtents = new Vector3(hitbox.size.x * Mathf.Abs(s.x), hitbox.size.y * Mathf.Abs(s.y), hitbox.size.z * Mathf.Abs(s.z)) * 0.5f;
    }

    void SetActiveVisual(bool value)
    {
        if (slashEffect != null) slashEffect.SetActive(value);
    }

    void OnDrawGizmos()
    {
        if (hitbox == null || !(Application.isPlaying && IsHitboxActive)) return;
        GetHitboxWorld(out Vector3 center, out Vector3 half);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, half * 2f);
    }

    void OnDrawGizmosSelected()
    {
        if (hitbox == null) return;
        GetHitboxWorld(out Vector3 center, out Vector3 half);
        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawWireCube(center, half * 2f);
    }
}
