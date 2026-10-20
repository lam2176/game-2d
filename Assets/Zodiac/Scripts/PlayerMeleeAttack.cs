using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Đánh thường bằng action Player/Attack (J, chuột trái, Enter, nút West gamepad).
///   • Combo 3 đòn ngang: chém – chém ngược – đòn kết (gấp đôi sát thương, đẩy văng, đứng hình lâu hơn).
///     Bấm tiếp trong comboWindow sau khi đòn trước kết thúc thì nối đòn, quá thì quay về đòn 1 (Dead Cells, Hollow Knight).
///   • Giữ ↑ + J: chém lên. Đang trên không giữ ↓ + J: chém xuống — trúng quái hoặc gai thì nảy lên (pogo của Hollow Knight).
/// Mỗi đòn: startup chờ ra đòn → hitbox có hiệu lực trong active → hết; bấm lại được sau recovery.
/// Mỗi mục tiêu chỉ nhận sát thương một lần trong một đòn (dù hitbox còn chạm nhiều frame).
///
/// Hitbox là BoxCollider2D (con của Facing, luôn TẮT): script chỉ dùng hình dạng của nó để quét
/// Physics2D.OverlapBox, rồi tự dời/đổi cỡ theo hướng chém, nên hitbox không bao giờ tự kích hoạt trigger khác.
/// Vệt chém (slashEffect) là hình lưỡi liềm quét theo hướng đòn trong lúc hitbox có hiệu lực.
///
/// Sát thương lấy từ nhân vật (Hổ đánh 2), nhân thêm khi đánh lén sau tàng hình (Rắn) và gây độc nếu có nội tại.
/// Chém trúng đạn của quái thì đạn tan (đỡ đạn bằng đòn đánh).
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
    }

    [Header("Hitbox")]
    [Tooltip("BoxCollider2D (để tắt), là con của Facing để tự lật theo hướng nhìn.")]
    public BoxCollider2D hitbox;
    [Tooltip("Layer có thể bị đánh trúng (Enemy).")]
    public LayerMask targetLayers;
    [Tooltip("Vệt chém hiện trong lúc hitbox có hiệu lực.")]
    public GameObject slashEffect;

    [Header("Đòn đánh")]
    [Min(1)] public int damage = 1;
    [Tooltip("Đòn 1, 2, 3 của combo ngang.")]
    public Swing[] combo =
    {
        new Swing { startup = 0.04f, active = 0.1f, recovery = 0.24f, damageMultiplier = 1f, knockback = 0.6f },
        new Swing { startup = 0.04f, active = 0.1f, recovery = 0.24f, damageMultiplier = 1f, knockback = 0.6f },
        new Swing { startup = 0.09f, active = 0.14f, recovery = 0.42f, damageMultiplier = 2f, knockback = 1.8f },
    };
    public Swing vertical = new Swing { startup = 0.04f, active = 0.12f, recovery = 0.3f, damageMultiplier = 1f, knockback = 0.6f };
    [Tooltip("Thời gian được nối đòn sau khi đòn trước hết hiệu lực (giây).")]
    [Min(0f)] public float comboWindow = 0.4f;
    [Tooltip("Vận tốc nảy lên khi chém xuống trúng quái/gai.")]
    [Min(0f)] public float pogoBounce = 13f;

    enum Phase { Ready, Startup, Active }

    ZodiacPlayerController controller;
    PlayerSkill skill;
    PlayerEnergy energy;
    PlayerDodge dodge;
    InputAction attackAction;
    ContactFilter2D filter, anyFilter;
    readonly List<Collider2D> overlaps = new List<Collider2D>();
    readonly HashSet<Health> hitThisSwing = new HashSet<Health>();
    Phase phase;
    float phaseEndTime, nextAttackTime, comboExpires;
    int comboStep;
    Swing current;
    Vector2 direction = Vector2.right;
    bool pogoed;
    Vector3 sideHitboxPosition;
    Vector2 sideHitboxSize;

    public bool IsHitboxActive => phase == Phase.Active;
    public float CooldownRemaining => Mathf.Max(0f, nextAttackTime - Time.time);
    /// <summary>0 = đánh được, 1 = vừa ra đòn (cho ô Q trên HUD).</summary>
    public float CooldownNormalized => current.recovery > 0f ? Mathf.Clamp01(CooldownRemaining / current.recovery) : 0f;
    /// <summary>Tổng số đòn đã ra (để kiểm tra).</summary>
    public int SwingCount { get; private set; }
    /// <summary>Chỉ số đòn trong combo của đòn gần nhất (0..2), -1 = đòn lên/xuống.</summary>
    public int LastComboStep { get; private set; }
    /// <summary>Ra đòn: (bước combo, hướng chém).</summary>
    public event Action<int, Vector2> Swung;

    void Awake()
    {
        controller = GetComponent<ZodiacPlayerController>();
        skill = GetComponent<PlayerSkill>();
        energy = GetComponent<PlayerEnergy>();
        dodge = GetComponent<PlayerDodge>();
        attackAction = InputSystem.actions.FindAction("Player/Attack");
        if (attackAction == null || hitbox == null)
        {
            Debug.LogError("Thiếu action Player/Attack hoặc chưa gán Hitbox.", this);
            enabled = false;
            return;
        }
        hitbox.enabled = false;
        sideHitboxPosition = hitbox.transform.localPosition;
        sideHitboxSize = hitbox.size;
        filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(targetLayers);
        anyFilter = new ContactFilter2D { useTriggers = true };
        if (slashEffect != null && slashEffect.TryGetComponent(out SpriteRenderer sr)) sr.sprite = ZodiacFx.Crescent;
        SetActiveVisual(false);
    }

    void OnEnable() => attackAction?.Enable();

    void Update()
    {
        if (!controller.ControlEnabled)
        {
            if (phase != Phase.Ready) EndSwing();
            return;
        }

        if (attackAction.WasPressedThisFrame() && Time.time >= nextAttackTime && (skill == null || !skill.IsBusy) &&
            (dodge == null || !dodge.IsDodging)) StartSwing();

        if (phase == Phase.Startup && Time.time >= phaseEndTime)
        {
            phase = Phase.Active;
            phaseEndTime = Time.time + current.active;
            SetActiveVisual(true);
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

    /// <summary>Ra đòn theo phím di chuyển: ↑ chém lên, ↓ (trên không) chém xuống, còn lại chém ngang theo combo.</summary>
    public void StartSwing()
    {
        float y = controller.InputY;
        StartSwing(y > 0.5f ? Vector2.up : y < -0.5f && !controller.IsGrounded ? Vector2.down : Vector2.right);
    }

    /// <summary>Ra đòn theo hướng cho trước (Vector2.right = ngang theo hướng nhìn, up, down).</summary>
    public void StartSwing(Vector2 swingDirection)
    {
        direction = swingDirection;

        if (direction == Vector2.right)
        {
            if (Time.time > comboExpires) comboStep = 0;
            current = combo[Mathf.Clamp(comboStep, 0, combo.Length - 1)];
            LastComboStep = comboStep;
            comboStep = (comboStep + 1) % combo.Length;
        }
        else
        {
            current = vertical;
            LastComboStep = -1;
        }

        if (controller.Character != null) damage = controller.Character.attackDamage;
        PlaceHitbox();
        SwingCount++;
        pogoed = false;
        hitThisSwing.Clear();
        nextAttackTime = Time.time + current.recovery;
        phase = Phase.Startup;
        phaseEndTime = Time.time + current.startup;

        var r = ZodiacResources.Get;
        if (r != null) ZodiacAudio.Play(LastComboStep == 2 ? r.swingHeavy : r.swing, 0.55f, 0.1f);
        Swung?.Invoke(Mathf.Max(0, LastComboStep), direction);
    }

    /// <summary>Hitbox ngang giữ nguyên chỉnh sửa trong Inspector; lên/xuống đặt theo chiều cao nhân vật.</summary>
    void PlaceHitbox()
    {
        float height = controller.BodyCollider.size.y;
        var ht = hitbox.transform;
        if (direction == Vector2.up)
        {
            ht.localPosition = new Vector3(0.1f, height + 0.45f, 0f);
            hitbox.size = new Vector2(1.5f, 1.2f);
        }
        else if (direction == Vector2.down)
        {
            ht.localPosition = new Vector3(0.1f, -0.45f, 0f);
            hitbox.size = new Vector2(1.3f, 1.2f);
        }
        else
        {
            ht.localPosition = sideHitboxPosition;
            hitbox.size = LastComboStep == 2 ? sideHitboxSize * new Vector2(1.25f, 1.15f) : sideHitboxSize;
        }
        if (slashEffect != null)
        {
            slashEffect.transform.localScale = new Vector3(hitbox.size.x * 1.15f, hitbox.size.y * 1.15f, 1f);
            slashEffect.transform.localRotation = Quaternion.Euler(0f, 0f, direction == Vector2.up ? 90f : direction == Vector2.down ? -90f : 0f);
            if (slashEffect.TryGetComponent(out SlashEffect fx)) fx.Configure(current.active, LastComboStep == 1, LastComboStep == 2);
        }
    }

    void EndSwing()
    {
        if (phase == Phase.Active || phase == Phase.Startup) comboExpires = Time.time + comboWindow;
        phase = Phase.Ready;
        SetActiveVisual(false);
    }

    void DetectHits()
    {
        GetHitboxWorld(out Vector2 center, out Vector2 size, out float angle);
        Physics2D.OverlapBox(center, size, angle, filter, overlaps);
        bool heavy = LastComboStep == 2;
        foreach (var col in overlaps)
        {
            var target = col.GetComponentInParent<Health>();
            if (target == null || target.gameObject == gameObject) continue;
            if (!hitThisSwing.Add(target)) continue;   // đã trúng trong đòn này
            float multiplier = (skill != null ? skill.ConsumeBackstab() : 1f) * current.damageMultiplier;
            int amount = Mathf.RoundToInt(damage * multiplier);
            // Nguồn sát thương đặt phía sau người chơi theo hướng chém → quái bị đẩy theo hướng chém
            Vector2 source = (Vector2)transform.position - WorldDirection * 2f;
            if (target.TakeDamage(new Hit(amount, source, current.knockback)))
            {
                if (controller.Character != null) Poison.Apply(target, controller.Character.poisonDamage);
                energy?.Gain(energy.gainPerHit + (heavy ? 1 : 0));
                Color spark = controller.Character != null ? controller.Character.themeColor : Color.white;
                GameFeel.Impact(col.ClosestPoint(center), spark, heavy || multiplier >= 2.5f);
            }
            Pogo();
        }
        if (direction == Vector2.down && !pogoed)
        {
            // Chém xuống trúng gai cũng nảy lên được
            Physics2D.OverlapBox(center, size, angle, anyFilter, overlaps);
            foreach (var col in overlaps)
                if (col.GetComponent<Hazard>() != null) { Pogo(); ZodiacParticles.Sparks(col.ClosestPoint(center), Color.white, 6); break; }
        }
        PopEnemyShots(center, size);
    }

    Vector2 WorldDirection => direction == Vector2.right ? new Vector2(controller.FacingDirection, 0f) : direction;

    void Pogo()
    {
        if (direction != Vector2.down || pogoed) return;
        pogoed = true;
        controller.Bounce(pogoBounce);
    }

    void PopEnemyShots(Vector2 center, Vector2 size)
    {
        var area = new Rect(center - size / 2f, size);
        for (int i = Projectile.Active.Count - 1; i >= 0; i--)
        {
            var shot = Projectile.Active[i];
            if (shot.hostile && area.Contains(shot.transform.position)) shot.Pop();
        }
    }

    void GetHitboxWorld(out Vector2 center, out Vector2 size, out float angle)
    {
        Transform t = hitbox.transform;
        center = t.TransformPoint(hitbox.offset);
        Vector3 s = t.lossyScale;
        size = new Vector2(hitbox.size.x * Mathf.Abs(s.x), hitbox.size.y * Mathf.Abs(s.y));
        angle = t.eulerAngles.z;
    }

    void SetActiveVisual(bool value)
    {
        if (slashEffect != null) slashEffect.SetActive(value);
    }

    void OnDrawGizmos()
    {
        if (hitbox == null || !(Application.isPlaying && IsHitboxActive)) return;
        GetHitboxWorld(out Vector2 center, out Vector2 size, out _);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(center, size);
    }

    void OnDrawGizmosSelected()
    {
        if (hitbox == null) return;
        GetHitboxWorld(out Vector2 center, out Vector2 size, out _);
        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}
