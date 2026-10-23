using UnityEngine;

/// <summary>
/// Vũ khí đang cầm (nhặt từ <see cref="WeaponPickup"/>), thay đòn đánh thường bằng phím Đánh:
///   • Đòn gánh — quét dài trước mặt, trúng là quật ngã cả đám (6 lần).
///   • Đèn lồng — đập ngắn, lửa làm quái bỏng mất máu dần (5 lần).
///   • Chum vại — ném thẳng, vỡ tung quật ngã quái trên đường bay (1 lần).
/// Hết lượt thì vỡ. Bấm Tương tác lần nữa để đặt xuống; bị quật ngã thì đánh rơi.
/// Gắn vào người chơi lúc nhặt món đầu tiên (không cần dựng lại scene).
/// </summary>
[RequireComponent(typeof(ZodiacPlayerController))]
public class PlayerWeapon : MonoBehaviour
{
    ZodiacPlayerController controller;
    PlayerMeleeAttack melee;
    PlayerDodge dodge;
    CharacterStats stats;
    WeaponPickup source;
    Transform held;
    int uses;
    float nextSwing, swingStart = -10f, takenFrame;
    readonly Collider[] overlaps = new Collider[16];

    public bool IsHolding => source != null;
    /// <summary>Khung hình vừa đặt vũ khí xuống — để phím Tương tác vừa bấm không nhặt lại ngay.</summary>
    public static int LastDropFrame { get; private set; } = -10;

    void Awake()
    {
        controller = GetComponent<ZodiacPlayerController>();
        melee = GetComponent<PlayerMeleeAttack>();
        dodge = GetComponent<PlayerDodge>();
        stats = GetComponent<CharacterStats>();
    }

    public void Take(WeaponPickup pickup)
    {
        if (IsHolding || pickup == null) return;
        source = pickup;
        uses = WeaponPickup.UsesOf(pickup.kind);
        held = pickup.Detach();
        if (held != null)
        {
            held.SetParent(controller.facing, false);
            held.localPosition = HandPosition;
            held.localRotation = Quaternion.Euler(0f, 0f, RestAngle);
        }
        takenFrame = Time.frameCount;
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.jump, 0.6f, 0.1f);
        PickupToast.Show(ZodiacInput.Format($"Cầm {WeaponPickup.NameOf(pickup.kind)} — {{Attack}}: {(pickup.kind == WeaponPickup.Kind.Jar ? "ném" : "đánh")} · {{Interact}}: đặt xuống"),
                         new Color(1f, 0.85f, 0.55f));
    }

    Vector3 HandPosition => source != null && source.kind == WeaponPickup.Kind.Jar ? new Vector3(0.1f, 1.7f, -0.05f) : new Vector3(0.45f, 1f, -0.05f);
    float RestAngle => source != null && source.kind == WeaponPickup.Kind.Pole ? -60f : 0f;

    void Update()
    {
        if (!IsHolding) return;
        if (controller.IsKnockedDown || !controller.gameObject.activeInHierarchy)
        {
            Drop();
            return;
        }
        AnimateSwing();
        if (!controller.ControlEnabled || controller.IsOverridden || (dodge != null && dodge.IsDodging)) return;
        if (InventoryWindow.IsOpen || PauseMenu.IsOpen) return;
        var interact = ZodiacInput.Find("Interact");
        if (interact != null && interact.WasPressedThisFrame() && Time.frameCount > takenFrame)
        {
            Drop();
            return;
        }
        var attack = ZodiacInput.Find("Attack");
        if (attack == null || !attack.WasPressedThisFrame() || Time.time < nextSwing) return;
        switch (source.kind)
        {
            case WeaponPickup.Kind.Jar: Throw(); break;
            default: Swing(); break;
        }
    }

    int BaseDamage => melee != null ? melee.damage : ZodiacBalance.Unit;

    void Swing()
    {
        bool pole = source.kind == WeaponPickup.Kind.Pole;
        nextSwing = Time.time + (pole ? 0.55f : 0.4f);
        swingStart = Time.time;
        float dir = controller.FacingDirection;
        Vector3 center = transform.position + new Vector3(dir * (pole ? 1.5f : 1f), 1f, 0f);
        Vector3 half = pole ? new Vector3(1.5f, 0.8f, 0.9f) : new Vector3(0.9f, 0.7f, 0.7f);
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(pole ? res.swingHeavy : res.swing, 0.9f, 0.1f);
        int count = Physics.OverlapBoxNonAlloc(center, half, overlaps, Quaternion.identity, melee != null ? melee.targetLayers : ~0, QueryTriggerInteraction.Ignore);
        bool any = false;
        for (int i = 0; i < count; i++)
        {
            var target = overlaps[i].GetComponentInParent<Health>();
            if (target == null || target.gameObject == gameObject || target.IsDead) continue;
            Vector3 src = transform.position - Vector3.right * dir * 2f;
            var hit = CharacterStats.CreateHit(stats, BaseDamage * (pole ? 1.4f : 1.1f), src, pole ? 1.8f : 1.1f);
            hit.knockdown = pole;
            if (dodge != null && dodge.ConsumeCounter()) { hit.amount *= 2; hit.crit = true; hit.knockdown = true; }
            if (!target.TakeDamage(hit)) continue;
            any = true;
            if (!pole) Poison.Apply(target, 4, Mathf.Max(2, BaseDamage / 4));
            GameFeel.Impact(overlaps[i].ClosestPoint(center), pole ? new Color(1f, 0.85f, 0.5f) : new Color(1f, 0.5f, 0.2f), pole);
        }
        if (!pole) ZodiacParticles.Sparks(center, new Color(1f, 0.55f, 0.2f), 8);
        if (any || pole) UseOnce();
    }

    void Throw()
    {
        nextSwing = Time.time + 0.4f;
        float dir = controller.FacingDirection;
        Vector3 from = transform.position + new Vector3(dir * 0.6f, 1.4f, 0f);
        var shot = Projectile.Spawn(from, Vector3.right * dir * 13f, 0.8f, new Color(0.6f, 0.35f, 0.2f),
                                    melee != null ? melee.targetLayers : ~0, 0, BaseDamage * 3, false);
        shot.attacker = stats;
        shot.knockdown = true;
        shot.knockback = 2f;
        shot.range = 12f;
        shot.pierce = 3;
        shot.spin = 720f;
        var res = ZodiacResources.Get;
        if (res != null) ZodiacAudio.Play(res.swingHeavy, 1f, 0.05f);
        Break(false);
    }

    void UseOnce()
    {
        if (--uses <= 0) Break(true);
    }

    /// <summary>Vỡ: mảnh văng, hình biến mất.</summary>
    void Break(bool shatter)
    {
        if (held != null)
        {
            if (shatter)
            {
                ZodiacParticles.Debris(held.position, source.kind == WeaponPickup.Kind.Lantern ? new Color(0.9f, 0.2f, 0.1f) : new Color(0.55f, 0.4f, 0.25f), 12, 0.5f);
                var res = ZodiacResources.Get;
                if (res != null) ZodiacAudio.Play(res.enemyDeath, 0.7f, 0.15f, held.position);
            }
            Destroy(held.gameObject);
        }
        if (source != null) Destroy(source.gameObject);
        held = null;
        source = null;
    }

    void Drop()
    {
        if (source == null) return;
        var s = source;
        source = null;
        LastDropFrame = Time.frameCount;
        s.Restore(held, transform.position + Vector3.right * controller.FacingDirection * 0.8f);
        held = null;
    }

    /// <summary>Vung vũ khí: xoay nhanh một vòng cung rồi về tư thế cầm.</summary>
    void AnimateSwing()
    {
        if (held == null) return;
        float t = (Time.time - swingStart) / 0.25f;
        float angle = RestAngle;
        if (t >= 0f && t < 1f) angle = Mathf.Lerp(RestAngle + 110f, RestAngle - 70f, Mathf.SmoothStep(0f, 1f, t));
        held.localRotation = Quaternion.Euler(0f, 0f, angle);
        held.localPosition = HandPosition;
    }

    void OnDisable()
    {
        if (gameObject.scene.isLoaded) Drop();   // đang đổi scene thì thôi
    }
}
