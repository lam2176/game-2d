using System;
using System.Collections.Generic;
using UnityEngine;

public enum StatType
{
    MaxHealth,
    MaxEnergy,
    /// <summary>Khí tự hồi mỗi giây.</summary>
    EnergyRegen,
    Attack,
    /// <summary>Giáp — xem <see cref="ZodiacBalance.Mitigate"/>.</summary>
    Defense,
    /// <summary>Tỉ lệ chí mạng 0..1.</summary>
    CritChance,
    /// <summary>Hệ số nhân sát thương chí mạng (1.5 = +50%).</summary>
    CritDamage,
}

public enum ModifierKind
{
    /// <summary>Cộng thẳng vào chỉ số gốc.</summary>
    Flat,
    /// <summary>Cộng phần trăm (0.1 = +10%), các phần trăm cộng dồn rồi mới nhân.</summary>
    Percent,
}

[Serializable]
public struct StatModifier
{
    public StatType stat;
    public ModifierKind kind;
    public float value;

    public StatModifier(StatType stat, ModifierKind kind, float value)
    {
        this.stat = stat;
        this.kind = kind;
        this.value = value;
    }
}

/// <summary>
/// Chỉ số người chơi = gốc (từ <see cref="ZodiacCharacter"/>) + modifier từ trang bị, nâng cấp (lên cấp, Đan), buff.
/// Giá trị = (gốc + tổng Flat) × (1 + tổng Percent). Mỗi modifier gắn với một nguồn (món đồ, nâng cấp...) để gỡ
/// cả nhóm khi tháo đồ / hết buff. Đổi chỉ số thì tự cập nhật Health (máu tối đa, giáp) và PlayerEnergy.
/// </summary>
public class CharacterStats : MonoBehaviour
{
    static readonly int StatCount = Enum.GetValues(typeof(StatType)).Length;

    readonly float[] baseValues = new float[StatCount];
    readonly float[] values = new float[StatCount];
    readonly List<(StatModifier mod, object source)> modifiers = new List<(StatModifier, object)>();
    Health health;
    PlayerEnergy energy;

    /// <summary>Chỉ số thay đổi (thêm/gỡ modifier, đổi nhân vật).</summary>
    public event Action Changed;

    public int MaxHealth => Mathf.Max(1, Mathf.RoundToInt(Get(StatType.MaxHealth)));
    public int MaxEnergy => Mathf.Max(1, Mathf.RoundToInt(Get(StatType.MaxEnergy)));
    public int Attack => Mathf.Max(1, Mathf.RoundToInt(Get(StatType.Attack)));
    public int Defense => Mathf.Max(0, Mathf.RoundToInt(Get(StatType.Defense)));
    /// <summary>Công hiện tại / công gốc — nhân vào sát thương chiêu để trang bị tăng cả chiêu.</summary>
    public float AttackScale => baseValues[(int)StatType.Attack] > 0f ? Get(StatType.Attack) / baseValues[(int)StatType.Attack] : 1f;

    void Awake()
    {
        health = GetComponent<Health>();
        energy = GetComponent<PlayerEnergy>();
    }

    public float Get(StatType stat) => values[(int)stat];
    public float GetBase(StatType stat) => baseValues[(int)stat];

    /// <summary>Đặt chỉ số gốc theo nhân vật và hồi đầy máu, Khí.</summary>
    public void InitFrom(ZodiacCharacter character)
    {
        if (health == null) health = GetComponent<Health>();
        if (energy == null) energy = GetComponent<PlayerEnergy>();
        baseValues[(int)StatType.MaxHealth] = character.maxHealth;
        baseValues[(int)StatType.MaxEnergy] = character.maxEnergy;
        baseValues[(int)StatType.EnergyRegen] = character.energyRegen;
        baseValues[(int)StatType.Attack] = character.attackDamage;
        baseValues[(int)StatType.Defense] = character.defense;
        baseValues[(int)StatType.CritChance] = ZodiacBalance.BaseCritChance;
        baseValues[(int)StatType.CritDamage] = ZodiacBalance.BaseCritDamage;
        Recalculate(refill: true);
    }

    public void AddModifier(StatModifier modifier, object source)
    {
        modifiers.Add((modifier, source));
        Recalculate(refill: false);
    }

    /// <summary>Gỡ mọi modifier của một nguồn (tháo đồ, hết buff). Trả về số modifier đã gỡ.</summary>
    public int RemoveModifiersFrom(object source)
    {
        int removed = modifiers.RemoveAll(m => Equals(m.source, source));
        if (removed > 0) Recalculate(refill: false);
        return removed;
    }

    void Recalculate(bool refill)
    {
        var flat = new float[StatCount];
        var percent = new float[StatCount];
        foreach (var (mod, _) in modifiers)
        {
            if (mod.kind == ModifierKind.Flat) flat[(int)mod.stat] += mod.value;
            else percent[(int)mod.stat] += mod.value;
        }
        for (int i = 0; i < StatCount; i++)
            values[i] = Mathf.Max(0f, (baseValues[i] + flat[i]) * (1f + percent[i]));

        if (health != null)
        {
            health.defense = Defense;
            if (refill) health.Configure(MaxHealth);
            else health.SetMax(MaxHealth);
        }
        if (energy != null)
        {
            energy.regenPerSecond = Get(StatType.EnergyRegen);
            if (refill) energy.Configure(MaxEnergy);
            else energy.SetMax(MaxEnergy);
        }
        Changed?.Invoke();
    }

    /// <summary>Tung xúc xắc sát thương: dao động ±<see cref="ZodiacBalance.DamageSpread"/> và chí mạng.</summary>
    public int RollDamage(float baseDamage, out bool crit)
    {
        crit = UnityEngine.Random.value < Get(StatType.CritChance);
        float spread = 1f + UnityEngine.Random.Range(-ZodiacBalance.DamageSpread, ZodiacBalance.DamageSpread);
        float amount = baseDamage * spread * (crit ? Get(StatType.CritDamage) : 1f);
        return Mathf.Max(1, Mathf.RoundToInt(amount));
    }

    /// <summary>Tạo đòn đánh; attacker null (quái, bẫy) thì sát thương cố định, không chí mạng.</summary>
    public static Hit CreateHit(CharacterStats attacker, float baseDamage, Vector3 source, float knockback = 1f, float stun = 0f)
    {
        if (attacker == null) return new Hit(Mathf.RoundToInt(baseDamage), source, knockback, stun);
        int amount = attacker.RollDamage(baseDamage, out bool crit);
        return new Hit(amount, source, knockback, stun) { crit = crit };
    }
}
