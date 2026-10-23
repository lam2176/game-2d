using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Túm & ném quái (đặc trưng beat 'em up): đứng sát một quái đang choáng, bấm phím Tương tác (mặc định F, tay cầm: Y / North) để túm.
/// Đang túm: đứng yên, quái bị giữ trước mặt; Q thúc gối (tối đa <see cref="maxKnees"/> lần), Q lần nữa hoặc F thì ném
/// theo hướng đang nhìn (bấm ← / → để quay). Quái bị ném văng đi, ai nó đâm phải (quái khác, hũ, rương) cũng mất máu
/// và bị quật ngã. Giữ quá <see cref="holdTime"/> giây hoặc bị đánh trúng thì quái tuột ra.
/// Không túm được boss, quái bay, quái không quật ngã được (Thạch Sư).
/// </summary>
[RequireComponent(typeof(ZodiacPlayerController))]
public class PlayerGrab : MonoBehaviour
{
    [Tooltip("Khoảng cách ngang tối đa tới quái để túm.")]
    [Min(0.3f)] public float reach = 1.3f;
    [Min(0.5f)] public float holdTime = 2.5f;
    [Min(0)] public int maxKnees = 3;
    [Tooltip("Sát thương mỗi cú thúc gối (× Unit).")]
    public float kneeDamage = 0.6f;
    [Tooltip("Sát thương cú ném cho quái bị ném và quái bị đâm (× Unit).")]
    public float throwDamage = 2f;
    public float throwSpeed = 11f;

    ZodiacPlayerController controller;
    Health health;
    InputAction interact, attack;
    EnemyBase held;
    float releaseAt, nextKnee;
    int knees, grabFrame;

    /// <summary>Đang túm một quái.</summary>
    public bool IsHolding => held != null;
    /// <summary>Số lần ném (để kiểm tra).</summary>
    public int ThrowCount { get; private set; }

    void Awake()
    {
        controller = GetComponent<ZodiacPlayerController>();
        health = GetComponent<Health>();
        interact = InputSystem.actions.FindAction("Player/Interact");
        attack = InputSystem.actions.FindAction("Player/Attack");
        if (health != null) health.Damaged += _ => Release();
    }

    void OnEnable()
    {
        interact?.Enable();
        attack?.Enable();
    }

    void OnDisable() => Release();

    CharacterStats Stats => GetComponent<CharacterStats>();
    float AttackScale => Stats != null ? Stats.AttackScale : 1f;

    void Update()
    {
        bool interactPressed = interact != null && interact.WasPressedThisFrame() &&
                               !(TryGetComponent(out PlayerWeapon w) && w.IsHolding) && Time.frameCount != PlayerWeapon.LastDropFrame;
        if (held == null)
        {
            if (interactPressed && controller.ControlEnabled && !controller.IsOverridden && controller.IsGrounded) TryGrab();
            return;
        }

        if (!controller.ControlEnabled || held.Health.IsDead || !held.isActiveAndEnabled)
        {
            Release();
            return;
        }
        var rb = controller.Body;
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        float mx = controller.MoveInput.x;
        if (Mathf.Abs(mx) > 0.5f) controller.SetFacing(mx);
        held.HoldAt(HoldPoint);

        if (Time.time >= releaseAt)
        {
            Release();
            return;
        }
        if (interactPressed && Time.frameCount != grabFrame)
        {
            Throw();
            return;
        }
        if (attack != null && attack.WasPressedThisFrame() && Time.time >= nextKnee)
        {
            if (knees >= maxKnees) Throw();
            else Knee();
        }
    }

    Vector3 HoldPoint => transform.position + new Vector3(controller.FacingDirection * 0.8f, 0.35f, 0f);

    /// <summary>Túm quái gần nhất trước mặt / sau lưng đang choáng. Trả về true nếu túm được.</summary>
    public bool TryGrab()
    {
        EnemyBase best = null;
        float bestDist = float.MaxValue;
        foreach (var e in FindObjectsByType<EnemyBase>())
        {
            if (!e.isActiveAndEnabled || !e.CanBeGrabbed) continue;
            Vector3 d = e.transform.position - transform.position;
            if (Mathf.Abs(d.x) > reach || Mathf.Abs(d.z) > 0.6f) continue;
            if (d.sqrMagnitude < bestDist) { bestDist = d.sqrMagnitude; best = e; }
        }
        if (best == null) return false;
        held = best;
        controller.SetFacing(best.transform.position.x - transform.position.x);
        held.Grab();
        controller.SetOverride(true, controller.gravityScale);
        releaseAt = Time.time + holdTime;
        nextKnee = Time.time + 0.15f;
        knees = 0;
        grabFrame = Time.frameCount;
        ZodiacFx.Puff(HoldPoint, new Color(1f, 1f, 1f, 0.5f), 0.7f);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.block, 0.6f, 0.1f);
        return true;
    }

    void Knee()
    {
        knees++;
        nextKnee = Time.time + 0.28f;
        var hit = CharacterStats.CreateHit(Stats, ZodiacBalance.Unit * kneeDamage * AttackScale, transform.position, 0f);
        if (held.Health.TakeDamage(hit))
        {
            Color spark = controller.Character != null ? controller.Character.themeColor : Color.white;
            GameFeel.Impact(held.transform.position + Vector3.up * 0.6f, spark, hit.crit);
        }
        releaseAt = Mathf.Max(releaseAt, Time.time + 0.8f);
        if (held != null && held.Health.IsDead) Release();
    }

    /// <summary>Ném quái đang túm theo hướng nhìn.</summary>
    public void Throw()
    {
        if (held == null) return;
        var e = held;
        held = null;
        controller.SetOverride(false);
        int damage = Mathf.RoundToInt(ZodiacBalance.Unit * throwDamage * AttackScale);
        e.Throw(new Vector3(controller.FacingDirection * throwSpeed, 4.5f, 0f), damage, Stats);
        ThrowCount++;
        GameFeel.Shake(0.15f);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.swingHeavy, 0.9f, 0.05f);
    }

    /// <summary>Thả quái ra (hết giờ, bị đánh): quái bật nhẹ ra, không gây sát thương.</summary>
    public void Release()
    {
        if (held == null) return;
        var e = held;
        held = null;
        if (controller != null) controller.SetOverride(false);
        if (e != null && !e.Health.IsDead) e.Throw(new Vector3(controller.FacingDirection * 2f, 3f, 0f), 0, null);
    }
}
