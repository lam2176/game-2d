using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Đánh thường bằng action Player/Attack (J, chuột trái, Enter, nút West gamepad).
/// Mỗi đòn: startupTime chờ ra đòn → hitbox có hiệu lực trong activeTime → hết. Hồi đòn tính từ lúc bấm.
/// Mỗi mục tiêu chỉ nhận sát thương một lần trong một đòn (dù hitbox còn chạm nhiều frame).
///
/// Hitbox là BoxCollider2D để chỉnh kích thước bằng Inspector/Edit Collider, nhưng luôn để TẮT:
/// script chỉ dùng hình dạng của nó để quét Physics2D.OverlapBox trong thời điểm tấn công hợp lệ,
/// nên hitbox không bao giờ tự kích hoạt trigger khác (đồng xu, đích, vực).
/// Nhân vật chưa có animation Attack → đòn đánh hiển thị bằng slashEffect trong lúc hitbox có hiệu lực.
/// </summary>
[RequireComponent(typeof(ZodiacPlayerController))]
public class PlayerMeleeAttack : MonoBehaviour
{
    [Header("Hitbox")]
    [Tooltip("BoxCollider2D (để tắt), là con của Facing để tự lật theo hướng nhìn.")]
    public BoxCollider2D hitbox;
    [Tooltip("Layer có thể bị đánh trúng (Enemy).")]
    public LayerMask targetLayers;
    [Tooltip("Hiện trong lúc hitbox có hiệu lực — thay cho animation Attack chưa có.")]
    public GameObject slashEffect;

    [Header("Đòn đánh")]
    [Min(1)] public int damage = 1;
    [Tooltip("Thời gian từ lúc bấm tới khi hitbox có hiệu lực (giây).")]
    [Min(0f)] public float startupTime = 0.05f;
    [Tooltip("Thời gian hitbox có hiệu lực (giây).")]
    [Min(0.02f)] public float activeTime = 0.12f;
    [Tooltip("Thời gian từ lúc bấm tới khi đánh được đòn tiếp theo (giây). Bấm trong lúc hồi đòn bị bỏ qua.")]
    [Min(0.05f)] public float cooldown = 0.4f;

    enum Phase { Ready, Startup, Active }

    ZodiacPlayerController controller;
    InputAction attackAction;
    ContactFilter2D filter;
    readonly List<Collider2D> overlaps = new List<Collider2D>();
    readonly HashSet<Health> hitThisSwing = new HashSet<Health>();
    Phase phase;
    float phaseEndTime, nextAttackTime;

    public bool IsHitboxActive => phase == Phase.Active;
    public float CooldownRemaining => Mathf.Max(0f, nextAttackTime - Time.time);
    /// <summary>Tổng số đòn đã ra (để kiểm tra).</summary>
    public int SwingCount { get; private set; }

    void Awake()
    {
        controller = GetComponent<ZodiacPlayerController>();
        attackAction = InputSystem.actions.FindAction("Player/Attack");
        if (attackAction == null || hitbox == null)
        {
            Debug.LogError("Thiếu action Player/Attack hoặc chưa gán Hitbox.", this);
            enabled = false;
            return;
        }
        hitbox.enabled = false;
        filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(targetLayers);
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

        if (attackAction.WasPressedThisFrame() && Time.time >= nextAttackTime) StartSwing();

        if (phase == Phase.Startup && Time.time >= phaseEndTime)
        {
            phase = Phase.Active;
            phaseEndTime = Time.time + activeTime;
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

    void StartSwing()
    {
        SwingCount++;
        hitThisSwing.Clear();
        nextAttackTime = Time.time + cooldown;
        phase = Phase.Startup;
        phaseEndTime = Time.time + startupTime;
    }

    void EndSwing()
    {
        phase = Phase.Ready;
        SetActiveVisual(false);
    }

    void DetectHits()
    {
        GetHitboxWorld(out Vector2 center, out Vector2 size, out float angle);
        Physics2D.OverlapBox(center, size, angle, filter, overlaps);
        foreach (var col in overlaps)
        {
            var target = col.GetComponentInParent<Health>();
            if (target == null || target.gameObject == gameObject) continue;
            if (!hitThisSwing.Add(target)) continue;   // đã trúng trong đòn này
            target.TakeDamage(damage, transform.position);
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
