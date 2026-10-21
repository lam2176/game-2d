using UnityEngine;

/// <summary>
/// Nội tại của con giáp đang chơi, gom về một chỗ: các script khác chỉ báo sự kiện (đánh trúng, nhặt xu) hoặc hỏi hệ số,
/// còn đọc số liệu nội tại từ <see cref="ZodiacCharacter"/> thì chỉ ở đây.
///   • Chỉ số: cộng chí mạng (<see cref="ZodiacCharacter.critBonus"/>), lăn hồi nhanh (<see cref="ZodiacCharacter.dodgeCooldownScale"/>).
///   • Đánh trúng: tẩm độc, đá bay nhân sát thương / hồi Khí / choáng thêm, đòn kết combo phun cầu lửa.
///   • Xu: tự hút xu trong bán kính, đủ số xu thì hồi máu (cả xu rải sẵn lẫn xu quái rơi).
/// Di chuyển (nhảy thêm, lượn, chống đẩy lùi) và "Chín mạng" vẫn nằm ở <see cref="ZodiacPlayerController"/> / <see cref="Health"/>.
/// </summary>
[RequireComponent(typeof(ZodiacPlayerController))]
public class PlayerPassives : MonoBehaviour
{
    public static PlayerPassives Current { get; private set; }

    ZodiacPlayerController controller;
    Health health;
    PlayerEnergy energy;
    PlayerDodge dodge;
    float baseDodgeCooldown = -1f;
    int coinsTowardHeal;

    ZodiacCharacter Character => controller != null ? controller.Character : null;

    /// <summary>Bán kính tự hút xu của con đang chơi (0 = không).</summary>
    public static float CoinMagnet => Current != null && Current.Character != null ? Current.Character.coinMagnetRadius : 0f;

    void Awake()
    {
        Current = this;
        controller = GetComponent<ZodiacPlayerController>();
        health = GetComponent<Health>();
        energy = GetComponent<PlayerEnergy>();
        dodge = GetComponent<PlayerDodge>();
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    void Start() => Apply();

    /// <summary>Áp nội tại dạng chỉ số cho con đang chơi (gọi lại khi đổi nhân vật).</summary>
    public void Apply()
    {
        var c = Character;
        if (c == null) return;
        var stats = GetComponent<CharacterStats>();
        if (stats != null)
        {
            stats.RemoveModifiersFrom(this);
            if (c.critBonus > 0f) stats.AddModifier(new StatModifier(StatType.CritChance, ModifierKind.Flat, c.critBonus), this);
        }
        if (dodge != null)
        {
            if (baseDodgeCooldown < 0f) baseDodgeCooldown = dodge.cooldown;
            dodge.cooldown = baseDodgeCooldown * Mathf.Max(0.1f, c.dodgeCooldownScale);
        }
        coinsTowardHeal = 0;
    }

    /// <summary>Hệ số sát thương đòn đánh thường theo loại đòn (đá bay của Heo ×2).</summary>
    public float DamageMultiplier(PlayerMeleeAttack.Kind kind)
    {
        var c = Character;
        return c != null && kind == PlayerMeleeAttack.Kind.Air ? c.stompDamageMultiplier : 1f;
    }

    /// <summary>Đòn đánh thường vừa trúng (comboStep 2 = đòn kết combo trên đất).</summary>
    public void OnMeleeHit(Health target, PlayerMeleeAttack.Kind kind, int comboStep, LayerMask targets, LayerMask walls)
    {
        var c = Character;
        if (c == null || target == null) return;
        if (c.poisonDamage > 0) Poison.Apply(target, c.poisonDamage);
        if (kind == PlayerMeleeAttack.Kind.Air)
        {
            if (c.airHitEnergy > 0) energy?.Gain(c.airHitEnergy);
            if (c.airHitStun > 0f && target.TryGetComponent(out EnemyBase enemy)) enemy.Stun(c.airHitStun);
        }
        if (c.finisherFireball && kind == PlayerMeleeAttack.Kind.Combo && comboStep == 2) Fireball(targets, walls);
    }

    void Fireball(LayerMask targets, LayerMask walls)
    {
        var stats = GetComponent<CharacterStats>();
        float d = controller.FacingDirection;
        Vector3 origin = controller.BodyCenter + new Vector3(d * 0.7f, 0.1f, 0f);
        var shot = Projectile.Spawn(origin, new Vector3(d * 12f, 0f, 0f), 0.5f, new Color(1f, 0.5f, 0.15f), targets, walls,
                                    Mathf.RoundToInt(ZodiacBalance.Unit * 0.8f * (stats != null ? stats.AttackScale : 1f)), false);
        shot.attacker = stats;
        shot.range = 7f;
        shot.owner = transform;
        shot.knockback = 0.6f;
    }

    /// <summary>Vừa nhặt n đồng xu: đủ số thì hồi máu (nội tại "Hiền hoà" của Dê).</summary>
    public void OnCoins(int n)
    {
        var c = Character;
        if (c == null || c.coinsPerHeal <= 0 || health == null || n <= 0) return;
        coinsTowardHeal += n;
        while (coinsTowardHeal >= c.coinsPerHeal)
        {
            coinsTowardHeal -= c.coinsPerHeal;
            if (health.Heal(ZodiacBalance.Unit) > 0) ZodiacFx.Ring(controller.BodyCenter, 1.5f, new Color(0.5f, 1f, 0.6f, 0.8f));
        }
    }
}
